using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class CustomerSpawner : MonoBehaviour
{
    const string CustomersFolder = "Assets/Prefabs/Characters/Customers";
    const string CharacterMaterialsFolder = "Assets/Materials/Characters";
    const string MazeThemesFolder = "Assets/Prefabs/Maze_Themes";

    [SerializeField] GameObject[] customerPrefabs;
    [SerializeField] GameObject customerPrefab;
    [SerializeField] Transform spawnPoint1;
    [SerializeField] Transform spawnPoint2;
    [SerializeField] Transform cashier;
    [SerializeField] Transform[] path1Checkpoints;
    [SerializeField] Transform[] path2Checkpoints;
    [SerializeField] bool autoFindSceneMarkers = true;
    [SerializeField] float spawnYOffset = -2f;
    [SerializeField] Material[] characterMaterials;
    [SerializeField] string elevatorDestinationSceneName = SceneNames.GatheringDestination;
    [SerializeField] Transform elevatorMiddleOverride;
    [SerializeField] float returnCustomerSwitchDelay = 1.5f;
    [SerializeField] MazeThemeData[] mazeThemes;
    [SerializeField] MazeThemeData pantryDefaultTheme;

    bool returnHandoffStarted;
    bool returnCutsceneStarted;
    Coroutine returnHandoffCoroutine;

    void Start()
    {
        GameBootstrap.EnsureCoreServices();

        if (autoFindSceneMarkers)
            FindSceneMarkers();

        EnsureDoorEntryZones();

        EnsureElevatorSetup();
        LoadCustomerPrefabsIfNeeded();
        LoadCharacterMaterialsIfNeeded();
        LoadMazeThemesIfNeeded();

        var orderService = CustomerOrderService.Instance;
        if (orderService != null && orderService.ReturningFromGathering)
        {
            var setup = GetComponent<BakeryElevatorSetup>();
            setup.SetupElevatorArea();

            var player = SceneTransformFinder.FindPlayer();
            var backDoor = setup.BackDoorInteractable;
            if (!returnCutsceneStarted && player != null && backDoor != null)
            {
                returnCutsceneStarted = true;
                StartCoroutine(backDoor.ReturnFromElevatorSequence(player, this));
                return;
            }

            HandlePostElevatorReturn();
            return;
        }

        SpawnCustomer();
    }

    public void ScheduleReturnCustomerHandoff()
    {
        if (returnHandoffStarted || returnHandoffCoroutine != null)
            return;

        returnHandoffCoroutine = StartCoroutine(ReturnCustomerHandoffAfterDelay());
    }

    IEnumerator ReturnCustomerHandoffAfterDelay()
    {
        var orderService = CustomerOrderService.Instance;
        var returningCustomer = orderService?.RestorePreservedCustomerAtCounter(
            path1Checkpoints,
            path2Checkpoints);

        if (returningCustomer != null)
        {
            var movement = returningCustomer.GetComponent<CustomerPathMovement>()
                ?? returningCustomer.GetComponentInChildren<CustomerPathMovement>();

            if (movement != null)
                yield return movement.PlayReturnTalkSequence(cashier);
            else
            {
                var animationDriver = returningCustomer.GetComponent<CharacterAnimatorDriver>()
                    ?? returningCustomer.GetComponentInChildren<CharacterAnimatorDriver>();

                if (animationDriver != null)
                {
                    animationDriver.SetTalking();
                    yield return animationDriver.WaitForStateFinish(CharacterAnimatorParams.Talking);
                    animationDriver.SetIdle();
                }
            }
        }

        if (returnCustomerSwitchDelay > 0f)
            yield return new WaitForSeconds(returnCustomerSwitchDelay);

        returnHandoffCoroutine = null;
        BeginReturnCustomerHandoff();
    }

    public void BeginReturnCustomerHandoff()
    {
        if (returnHandoffStarted)
            return;

        returnHandoffStarted = true;

        var orderService = CustomerOrderService.Instance;
        orderService?.StartPreservedCustomerExit(
            path1Checkpoints,
            path2Checkpoints,
            spawnPoint1,
            spawnPoint2,
            SpawnCustomer);
    }

    public void HandlePostElevatorReturn()
    {
        if (!returnHandoffStarted && returnHandoffCoroutine == null)
            ScheduleReturnCustomerHandoff();

        CustomerOrderService.Instance?.ClearReturningFromGathering();
        returnHandoffStarted = false;

        BakeryCashierDialogue.Instance?.ShowReturnGreetingIfNeeded();
    }

    void SpawnCustomer()
    {
        LoadMazeThemesIfNeeded();
        MazeThemeManager.EnsureInstance(mazeThemes, pantryDefaultTheme);
        MazeThemeManager.PrepareForNewOrder();
        ConfigureOrderService();

        GameObject prefabToSpawn = PickRandomCustomerPrefab();
        if (prefabToSpawn == null)
        {
            Debug.LogError(
                "CustomerSpawner: No customer prefabs found. Add prefabs to Assets/Prefabs/Characters/Customers.");
            return;
        }

        if (spawnPoint1 == null || spawnPoint2 == null)
        {
            Debug.LogError("CustomerSpawner: Missing spawn points.");
            return;
        }

        bool usePath1 = Random.Range(0, 2) == 0;
        Transform spawnPoint = usePath1 ? spawnPoint1 : spawnPoint2;
        Transform[] checkpoints = usePath1 ? path1Checkpoints : path2Checkpoints;
        int pathNumber = usePath1 ? 1 : 2;

        Vector3 spawnPosition = spawnPoint.position + new Vector3(0f, spawnYOffset, 0f);

        GameObject customer = Instantiate(
            prefabToSpawn,
            spawnPosition,
            spawnPoint.rotation
        );

        ApplyRandomCharacterMaterial(customer);
        EnsureDoorTriggerCollider(customer);

        CustomerPathMovement movement = customer.GetComponentInChildren<CustomerPathMovement>();
        if (movement == null)
        {
            movement = customer.AddComponent<CustomerPathMovement>();
            Debug.LogWarning(
                "CustomerSpawner: Added CustomerPathMovement at runtime. Add it to the prefab to keep settings.");
        }

        movement.Initialize(checkpoints, cashier, pathNumber);
        CustomerOrderService.Instance?.RegisterCustomerPath(customer.transform, pathNumber);
    }

    GameObject PickRandomCustomerPrefab()
    {
        if (customerPrefabs != null && customerPrefabs.Length > 0)
            return customerPrefabs[Random.Range(0, customerPrefabs.Length)];

        return customerPrefab;
    }

    void ConfigureOrderService()
    {
        var orderService = CustomerOrderService.EnsurePersistentInstance();
        var theme = MazeThemeManager.GetCurrentOrDefault(pantryDefaultTheme);

        if (theme != null && theme.customerOrderItems != null && theme.customerOrderItems.Length > 0)
        {
            orderService.Configure(theme.customerOrderItems);
            return;
        }

        Debug.LogError("CustomerSpawner: No customer order items found. Assign Customer Order Items on the current theme or Pantry Default Theme.");
        orderService.Configure(null);
    }

    void LoadCustomerPrefabsIfNeeded()
    {
        if (customerPrefabs != null && customerPrefabs.Length > 0)
            return;

#if UNITY_EDITOR
        customerPrefabs = LoadPrefabsFromFolder(CustomersFolder);
#endif
    }

    void LoadCharacterMaterialsIfNeeded()
    {
        if (characterMaterials != null && characterMaterials.Length > 0)
            return;

#if UNITY_EDITOR
        characterMaterials = LoadMaterialsFromFolder(CharacterMaterialsFolder);
#endif
    }

    void LoadMazeThemesIfNeeded()
    {
        if (mazeThemes != null && mazeThemes.Length > 0)
            return;

#if UNITY_EDITOR
        mazeThemes = LoadThemesFromFolder(MazeThemesFolder);
#endif
    }

    void EnsureElevatorSetup()
    {
        var setup = GetComponent<BakeryElevatorSetup>();
        if (setup == null)
            setup = gameObject.AddComponent<BakeryElevatorSetup>();

        setup.ApplySettings(elevatorDestinationSceneName, elevatorMiddleOverride);
    }

    void EnsureDoorEntryZones()
    {
        if (path1Checkpoints != null && path1Checkpoints.Length > 2)
        {
            CustomerDoorEntryZone.EnsureBetweenCheckpoints(
                path1Checkpoints[1],
                path1Checkpoints[2]);
        }

        if (path2Checkpoints != null && path2Checkpoints.Length > 2)
        {
            CustomerDoorEntryZone.EnsureBetweenCheckpoints(
                path2Checkpoints[1],
                path2Checkpoints[2]);
        }
    }

    void FindSceneMarkers()
    {
        if (spawnPoint1 == null)
            spawnPoint1 = FindTransform("Spawnpoint 1");

        if (spawnPoint2 == null)
            spawnPoint2 = FindTransform("spawnpoint 2");

        if (cashier == null)
            cashier = FindTransform("Cashier");

        if (path1Checkpoints == null || path1Checkpoints.Length == 0)
        {
            path1Checkpoints = new[]
            {
                FindTransform("Checkpoint 1.1"),
                FindTransform("Checkpoint 1.2"),
                FindTransform("Checkpoint 1.3"),
                FindTransform("Checkpoint 1.4")
            };
        }

        if (path2Checkpoints == null || path2Checkpoints.Length == 0)
        {
            path2Checkpoints = new[]
            {
                FindTransform("Checkpoint 2.1"),
                FindTransform("Checkpoint 2.2"),
                FindTransform("Checkpoint 2.3"),
                FindTransform("Checkpoint 2.4")
            };
        }
    }

    static Transform FindTransform(string objectName)
    {
        var found = SceneTransformFinder.FindRecursive(objectName);
        if (found != null)
            return found;

        var foundIgnoreCase = SceneTransformFinder.FindRecursiveIgnoreCase(objectName);
        if (foundIgnoreCase != null)
            return foundIgnoreCase;

        var legacy = GameObject.Find(objectName);
        return legacy != null ? legacy.transform : null;
    }

    void ApplyRandomCharacterMaterial(GameObject customer)
    {
        if (characterMaterials == null || characterMaterials.Length == 0)
        {
            Debug.LogWarning("CustomerSpawner: No character materials found in Materials/Characters.");
            return;
        }

        Material material = characterMaterials[Random.Range(0, characterMaterials.Length)];
        if (material == null)
            return;

        Renderer[] renderers = customer.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] slots = renderer.materials;
            if (slots.Length == 0)
                continue;

            slots[0] = material;
            renderer.materials = slots;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        customerPrefabs = LoadPrefabsFromFolder(CustomersFolder);
        characterMaterials = LoadMaterialsFromFolder(CharacterMaterialsFolder);
        mazeThemes = LoadThemesFromFolder(MazeThemesFolder);
    }

    static GameObject[] LoadPrefabsFromFolder(string folder)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        if (guids.Length == 0)
            return null;

        var loaded = new GameObject[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            loaded[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return loaded;
    }

    static Material[] LoadMaterialsFromFolder(string folder)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { folder });
        if (guids.Length == 0)
            return null;

        var loaded = new Material[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            loaded[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        return loaded;
    }

    static MazeThemeData[] LoadThemesFromFolder(string folder)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MazeThemeData", new[] { folder });
        if (guids.Length == 0)
            return null;

        var loaded = new MazeThemeData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            loaded[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<MazeThemeData>(path);
        }

        return loaded;
    }
#endif

    static void EnsureDoorTriggerCollider(GameObject customer)
    {
        CapsuleCollider capsule = customer.GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = customer.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.4f;
            capsule.center = new Vector3(0f, 1f, 0f);
        }

        Rigidbody rb = customer.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = customer.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}
