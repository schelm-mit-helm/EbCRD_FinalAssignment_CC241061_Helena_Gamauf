using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomerOrderService : MonoBehaviour
{
    public static CustomerOrderService Instance { get; private set; }

    public const int DefaultOrderItemCount = 2;
    public const int FirstCollapsedOrderCustomer = 5;

    const string ItemDataFolder = "Assets/Prefabs/Item data";

    [SerializeField] Item[] availableItems;
    [SerializeField] int orderItemCount = DefaultOrderItemCount;

    int completedCustomerCount;

    readonly List<Item> currentOrder = new();
    readonly List<bool> gatheredItems = new();
    Transform activeCustomer;
    CashierInteractable cashier;
    GameObject preservedCustomer;
    int preservedCustomerPath = 1;
    bool returningFromGathering;
    bool hasReturnPlayerPose;
    Vector3 returnPlayerPosition;
    Quaternion returnPlayerRotation;
    Vector3 returnCameraLocalPosition;
    Quaternion returnCameraLocalRotation;
    float returnCameraPitch;
    Vector3 returnElevatorSpawnPosition;
    float returnCameraWorldY;
    float returnCameraWorldZ;

    public IReadOnlyList<Item> CurrentOrder => currentOrder;
    public Transform ActiveCustomer => activeCustomer;
    public bool HasOrder => currentOrder.Count > 0;
    public bool CashierReady { get; private set; }
    public bool PlayerKnowsTask { get; private set; }
    public bool PlayerHasTaskList => PlayerKnowsTask && HasOrder;
    public bool ReturningFromGathering => returningFromGathering;
    public bool UseCollapsedOrderDisplay => completedCustomerCount >= FirstCollapsedOrderCustomer - 1;

    public readonly struct OrderDisplayEntry
    {
        public readonly Item Item;
        public readonly int Quantity;
        public readonly int GatheredCount;

        public OrderDisplayEntry(Item item, int quantity, int gatheredCount)
        {
            Item = item;
            Quantity = quantity;
            GatheredCount = gatheredCount;
        }

        public bool IsComplete => GatheredCount >= Quantity;
    }

    public bool AllItemsGathered
    {
        get
        {
            if (!PlayerHasTaskList)
                return false;

            for (var i = 0; i < gatheredItems.Count; i++)
            {
                if (!gatheredItems[i])
                    return false;
            }
            AkUnitySoundEngine.PostEvent("Play_missionAccomplished", gameObject);
            return true;
        }
    }

    public static CustomerOrderService EnsurePersistentInstance()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<CustomerOrderService>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.MakePersistent();
            return Instance;
        }

        var serviceObject = new GameObject(nameof(CustomerOrderService));
        DontDestroyOnLoad(serviceObject);
        return serviceObject.AddComponent<CustomerOrderService>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        MakePersistent();
        LoadItemsIfNeeded();
        EnsureCashier();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cashier = null;
        EnsureCashier();
    }

    void MakePersistent()
    {
        if (gameObject.scene.name == "DontDestroyOnLoad")
            return;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Configure(Item[] items)
    {
        if (items != null && items.Length > 0)
            availableItems = items;
        else
            LoadItemsIfNeeded();
    }

    public void PlaceOrder(Transform customer)
    {
        if (customer == null)
            return;

        var items = GetAvailableOrderItems();
        if (items == null || items.Length == 0)
            return;

        activeCustomer = customer;
        currentOrder.Clear();
        gatheredItems.Clear();

        for (var i = 0; i < orderItemCount; i++)
            currentOrder.Add(items[Random.Range(0, items.Length)]);

        for (var i = 0; i < currentOrder.Count; i++)
            gatheredItems.Add(false);

        CashierReady = true;
        EnsureCashier();
        if (cashier != null)
            cashier.OnCustomerAtCounter();
    }

    public void NotifyPlayerReceivedTask()
    {
        PlayerKnowsTask = true;
    }

    public bool IsItemGathered(int orderIndex) =>
        orderIndex >= 0 && orderIndex < gatheredItems.Count && gatheredItems[orderIndex];

    public bool TryGatherItem(Item item, out int orderIndex)
    {
        orderIndex = -1;

        if (!PlayerHasTaskList || item == null)
            return false;

        for (var i = 0; i < currentOrder.Count; i++)
        {
            if (gatheredItems[i] || currentOrder[i] != item)
                continue;

            gatheredItems[i] = true;
            orderIndex = i;
            return true;
        }

        return false;
    }

    public bool IsItemInOrder(Item item)
    {
        if (!HasOrder || item == null)
            return false;

        foreach (var orderItem in currentOrder)
        {
            if (orderItem == item)
                return true;
        }

        return false;
    }

    public bool IsItemFullyDelivered(Item item)
    {
        if (!HasOrder || item == null)
            return false;

        for (var i = 0; i < currentOrder.Count; i++)
        {
            if (currentOrder[i] == item && !gatheredItems[i])
                return false;
        }

        return IsItemInOrder(item);
    }

    public IReadOnlyList<OrderDisplayEntry> GetOrderDisplayEntries()
    {
        var entries = new List<OrderDisplayEntry>();
        var entryIndexByItem = new Dictionary<Item, int>();

        for (var i = 0; i < currentOrder.Count; i++)
        {
            var item = currentOrder[i];
            if (item == null)
                continue;

            if (entryIndexByItem.TryGetValue(item, out var entryIndex))
            {
                var existing = entries[entryIndex];
                entries[entryIndex] = new OrderDisplayEntry(
                    item,
                    existing.Quantity + 1,
                    existing.GatheredCount + (gatheredItems[i] ? 1 : 0));
            }
            else
            {
                entryIndexByItem[item] = entries.Count;
                entries.Add(new OrderDisplayEntry(item, 1, gatheredItems[i] ? 1 : 0));
            }
        }

        return entries;
    }

    public void StartCustomerLoopingTalk()
    {
        if (activeCustomer == null)
            return;

        var movement = activeCustomer.GetComponent<CustomerPathMovement>()
            ?? activeCustomer.GetComponentInChildren<CustomerPathMovement>();

        movement?.StartLoopingTalk();
    }

    public void RegisterCustomerPath(Transform customer, int pathNumber)
    {
        if (customer == null)
            return;

        preservedCustomerPath = pathNumber;

        var movement = customer.GetComponent<CustomerPathMovement>()
            ?? customer.GetComponentInChildren<CustomerPathMovement>();

        if (movement != null)
            preservedCustomerPath = movement.PathNumber;
    }

    public void PreserveActiveCustomerForGathering()
    {
        if (activeCustomer == null)
            return;

        preservedCustomer = activeCustomer.gameObject;

        var movement = preservedCustomer.GetComponent<CustomerPathMovement>()
            ?? preservedCustomer.GetComponentInChildren<CustomerPathMovement>();

        if (movement != null)
            preservedCustomerPath = movement.PathNumber;

        preservedCustomer.transform.SetParent(null);
        DontDestroyOnLoad(preservedCustomer);
        preservedCustomer.SetActive(false);
        activeCustomer = null;
    }

    public void MarkReturningFromGathering()
    {
        returningFromGathering = true;
        PlayerKnowsTask = false;
        currentOrder.Clear();
        gatheredItems.Clear();
        CashierReady = false;
        completedCustomerCount++;
        orderItemCount++;
        CustomerOrderUI.Instance?.Hide();
        BakeryCashierDialogue.MarkPendingReturnGreeting();
    }

    public void ResetToDefaults()
    {
        currentOrder.Clear();
        gatheredItems.Clear();
        activeCustomer = null;
        CashierReady = false;
        PlayerKnowsTask = false;
        returningFromGathering = false;
        hasReturnPlayerPose = false;
        completedCustomerCount = 0;
        orderItemCount = DefaultOrderItemCount;
        preservedCustomerPath = 1;

        if (preservedCustomer != null)
        {
            Destroy(preservedCustomer);
            preservedCustomer = null;
        }
    }

    public void SavePlayerReturnPose(
        Vector3 playerPosition,
        Quaternion playerRotation,
        Vector3 cameraLocalPosition,
        Quaternion cameraLocalRotation,
        float cameraPitch,
        float cameraWorldY,
        float cameraWorldZ,
        Vector3 elevatorSpawnPosition)
    {
        returnPlayerPosition = playerPosition;
        returnPlayerRotation = playerRotation;
        returnCameraLocalPosition = cameraLocalPosition;
        returnCameraLocalRotation = cameraLocalRotation;
        returnCameraPitch = cameraPitch;
        returnCameraWorldY = cameraWorldY;
        returnCameraWorldZ = cameraWorldZ;
        returnElevatorSpawnPosition = elevatorSpawnPosition;
        hasReturnPlayerPose = true;
    }

    public bool TryGetReturnElevatorSpawn(out Vector3 elevatorSpawnPosition)
    {
        elevatorSpawnPosition = returnElevatorSpawnPosition;
        return hasReturnPlayerPose;
    }

    public bool TryGetReturnCameraStart(out float cameraWorldY, out float cameraWorldZ)
    {
        cameraWorldY = returnCameraWorldY;
        cameraWorldZ = returnCameraWorldZ;
        return hasReturnPlayerPose;
    }

    public bool TryGetReturnPlayerPose(
        out Vector3 playerPosition,
        out Quaternion playerRotation,
        out Vector3 cameraLocalPosition,
        out Quaternion cameraLocalRotation,
        out float cameraPitch)
    {
        playerPosition = returnPlayerPosition;
        playerRotation = returnPlayerRotation;
        cameraLocalPosition = returnCameraLocalPosition;
        cameraLocalRotation = returnCameraLocalRotation;
        cameraPitch = returnCameraPitch;
        return hasReturnPlayerPose;
    }

    public void ClearReturningFromGathering()
    {
        returningFromGathering = false;
    }

    public GameObject RestorePreservedCustomerAtCounter(
        Transform[] path1Checkpoints,
        Transform[] path2Checkpoints)
    {
        if (preservedCustomer == null)
            return null;

        var returningCustomer = preservedCustomer;
        var activeScene = SceneManager.GetActiveScene();
        returningCustomer.SetActive(true);
        SceneManager.MoveGameObjectToScene(returningCustomer, activeScene);

        var counterCheckpoint = GetCounterCheckpoint(
            preservedCustomerPath,
            path1Checkpoints,
            path2Checkpoints);

        if (counterCheckpoint != null)
            returningCustomer.transform.position = counterCheckpoint.position;

        var movement = returningCustomer.GetComponent<CustomerPathMovement>()
            ?? returningCustomer.GetComponentInChildren<CustomerPathMovement>();

        movement?.PrepareForReturnTalk();
        return returningCustomer;
    }

    public void StartPreservedCustomerExit(
        Transform[] path1Checkpoints,
        Transform[] path2Checkpoints,
        Transform spawnPoint1,
        Transform spawnPoint2,
        System.Action onReachedSpawnPoint = null)
    {
        if (preservedCustomer == null)
            return;

        var exitPath = BuildExitPath(
            preservedCustomerPath,
            path1Checkpoints,
            path2Checkpoints,
            spawnPoint1,
            spawnPoint2);

        var movement = preservedCustomer.GetComponent<CustomerPathMovement>()
            ?? preservedCustomer.GetComponentInChildren<CustomerPathMovement>();

        movement?.StartExitPath(exitPath, spawnPoint1, spawnPoint2, onReachedSpawnPoint);
        preservedCustomer = null;
    }

    public GameObject ReleasePreservedCustomerOnReturn(
        Transform[] path1Checkpoints,
        Transform[] path2Checkpoints,
        Transform spawnPoint1,
        Transform spawnPoint2)
    {
        var returningCustomer = RestorePreservedCustomerAtCounter(path1Checkpoints, path2Checkpoints);
        if (returningCustomer == null)
            return null;

        StartPreservedCustomerExit(
            path1Checkpoints,
            path2Checkpoints,
            spawnPoint1,
            spawnPoint2);

        return returningCustomer;
    }

    static Transform GetCounterCheckpoint(
        int pathNumber,
        Transform[] path1Checkpoints,
        Transform[] path2Checkpoints)
    {
        var sourcePath = pathNumber == 2 ? path2Checkpoints : path1Checkpoints;
        if (sourcePath == null || sourcePath.Length == 0)
            return null;

        for (var i = sourcePath.Length - 1; i >= 0; i--)
        {
            if (sourcePath[i] != null)
                return sourcePath[i];
        }

        return null;
    }

    static Transform[] BuildExitPath(
        int pathNumber,
        Transform[] path1Checkpoints,
        Transform[] path2Checkpoints,
        Transform spawnPoint1,
        Transform spawnPoint2)
    {
        var sourcePath = pathNumber == 2 ? path2Checkpoints : path1Checkpoints;
        var spawnPoint = pathNumber == 2 ? spawnPoint2 : spawnPoint1;

        if (sourcePath == null || sourcePath.Length == 0)
            return spawnPoint != null ? new[] { spawnPoint } : System.Array.Empty<Transform>();

        var exitPath = new List<Transform>();
        for (var i = sourcePath.Length - 1; i >= 0; i--)
        {
            if (sourcePath[i] != null)
                exitPath.Add(sourcePath[i]);
        }

        if (spawnPoint != null)
            exitPath.Add(spawnPoint);

        return exitPath.ToArray();
    }

    void EnsureCashier()
    {
        if (cashier != null)
            return;

        cashier = FindFirstObjectByType<CashierInteractable>(FindObjectsInactive.Include);
        if (cashier != null)
        {
            WireCashierAnimator(cashier.transform);
            return;
        }

        var cashierTransform = GameObject.Find("Cashier")?.transform;
        if (cashierTransform == null)
            return;

        cashier = cashierTransform.gameObject.AddComponent<CashierInteractable>();
        WireCashierAnimator(cashierTransform);
    }

    static void WireCashierAnimator(Transform cashierTransform)
    {
        var driver = cashierTransform.GetComponent<CharacterAnimatorDriver>();
        var childAnimator = cashierTransform.GetComponentInChildren<Animator>();
        if (driver != null && childAnimator != null)
            driver.BindAnimator(childAnimator);
    }

    Item[] GetAvailableOrderItems()
    {
        if (availableItems != null && availableItems.Length > 0)
            return availableItems;

        LoadItemsIfNeeded();
        return availableItems;
    }

    void LoadItemsIfNeeded()
    {
        if (availableItems != null && availableItems.Length > 0)
            return;

#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Item", new[] { ItemDataFolder });
        if (guids.Length == 0)
            return;

        availableItems = new Item[guids.Length];
        for (var i = 0; i < guids.Length; i++)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            availableItems[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>(path);
        }
#else
        availableItems = Resources.LoadAll<Item>("Item data");
#endif
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (availableItems != null && availableItems.Length > 0)
            return;

        var guids = UnityEditor.AssetDatabase.FindAssets("t:Item", new[] { ItemDataFolder });
        if (guids.Length == 0)
            return;

        availableItems = new Item[guids.Length];
        for (var i = 0; i < guids.Length; i++)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            availableItems[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>(path);
        }
    }
#endif
}
