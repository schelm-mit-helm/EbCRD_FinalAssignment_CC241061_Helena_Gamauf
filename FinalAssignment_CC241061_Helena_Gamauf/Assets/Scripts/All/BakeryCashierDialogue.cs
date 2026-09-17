using UnityEngine;
using UnityEngine.SceneManagement;

public class BakeryCashierDialogue : MonoBehaviour
{
    public static BakeryCashierDialogue Instance { get; private set; }

    const string BakerySceneName = SceneNames.Bakery;

    [SerializeField] float idleNudgeDelay = 40f;

    float bakeryIdleTime;
    bool pendingReturnGreeting;
    CashierInteractable dialogueCashier;

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        var existing = FindFirstObjectByType<BakeryCashierDialogue>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        var dialogueObject = new GameObject(nameof(BakeryCashierDialogue));
        DontDestroyOnLoad(dialogueObject);
        dialogueObject.AddComponent<BakeryCashierDialogue>();
    }

    public static void MarkPendingReturnGreeting()
    {
        EnsureInstance();
        Instance.pendingReturnGreeting = true;
    }

    public static void OnPlayerTalkedToCashier()
    {
        if (Instance == null)
            return;

        Instance.bakeryIdleTime = 0f;
    }

    public void OnCashierWaitingForPlayer()
    {
        bakeryIdleTime = 0f;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bakeryIdleTime = 0f;

        if (scene.name == BakerySceneName)
            dialogueCashier = CashierInteractable.FindInScene();
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != BakerySceneName)
            return;

        var orderService = CustomerOrderService.Instance;
        if (orderService == null || orderService.PlayerKnowsTask || !orderService.CashierReady)
        {
            bakeryIdleTime = 0f;
            return;
        }

        bakeryIdleTime += Time.deltaTime;
        if (bakeryIdleTime < idleNudgeDelay)
            return;

        bakeryIdleTime = 0f;
        GetCashier()?.ShowIdleNudgeDialogue();
    }

    public void ShowTaskRequest()
    {
        GetCashier()?.ShowTaskRequestDialogue();
    }

    public void ShowReturnGreetingIfNeeded()
    {
        if (!pendingReturnGreeting)
            return;

        pendingReturnGreeting = false;
        GetCashier()?.ShowReturnGreetingDialogue();
    }

    public void ResetForNewRun()
    {
        bakeryIdleTime = 0f;
        pendingReturnGreeting = false;
        dialogueCashier = null;
    }

    CashierInteractable GetCashier()
    {
        if (dialogueCashier != null)
            return dialogueCashier;

        dialogueCashier = CashierInteractable.FindInScene();
        return dialogueCashier;
    }
}
