using UnityEngine;
using UnityEngine.SceneManagement;

public class GatheringSceneSetup : MonoBehaviour
{
    const string DropZoneObjectName = "Item Drop";
    static readonly string[] ReturnButtonObjectNames = { "Elevator_Button", "button" };

    [SerializeField] bool autoSetupOnStart = true;
    [SerializeField] string returnSceneName = SceneNames.Bakery;
    [SerializeField] bool ensureDropZoneTrigger = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SubscribeToSceneLoads()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryEnsureForScene(SceneManager.GetActiveScene().name);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryEnsureForScene(scene.name);

    static void TryEnsureForScene(string sceneName)
    {
        if (!SceneNames.IsGatheringScene(sceneName))
            return;

        if (FindFirstObjectByType<GatheringSceneSetup>(FindObjectsInactive.Include) != null)
            return;

        var setupObject = new GameObject(nameof(GatheringSceneSetup));
        setupObject.AddComponent<GatheringSceneSetup>();
    }

    void Start()
    {
        if (autoSetupOnStart)
            SetupScene();
    }

    public void SetupScene()
    {
        RecordPlayerStartPose();
        GameBootstrap.EnsureCoreServices();
        MazeThemeEnvironmentEffects.ApplyForCurrentTheme();
        PantryTimerUI.EnsureAndStart();
        EnsureElevatorDoors();
        EnsureDropZone();
        EnsureReturnButton();
    }

    static void RecordPlayerStartPose()
    {
        var player = SceneTransformFinder.FindPlayer();
        if (player != null)
            GatheringPlayerStartPose.Record(player);
    }

    void EnsureElevatorDoors()
    {
        var elevatorSetup = GetComponent<GatheringElevatorSetup>();
        if (elevatorSetup == null)
            elevatorSetup = gameObject.AddComponent<GatheringElevatorSetup>();

        elevatorSetup.SetupElevatorArea();
    }

    void EnsureDropZone()
    {
        var zoneObject = GameObject.Find(DropZoneObjectName);
        if (zoneObject == null)
        {
            Debug.LogWarning($"GatheringSceneSetup: Could not find '{DropZoneObjectName}' in the scene.");
            return;
        }

        var collider = zoneObject.GetComponent<Collider>();
        if (collider == null)
            collider = zoneObject.AddComponent<BoxCollider>();

        if (ensureDropZoneTrigger)
            collider.isTrigger = true;

        if (zoneObject.GetComponent<OrderItemDropZone>() == null)
            zoneObject.AddComponent<OrderItemDropZone>();
    }

    void EnsureReturnButton()
    {
        var buttonObject = FindReturnButtonObject();
        if (buttonObject == null)
        {
            Debug.LogWarning("GatheringSceneSetup: Could not find a return button in the scene.");
            return;
        }

        if (buttonObject.GetComponent<Collider>() == null)
            buttonObject.AddComponent<BoxCollider>();

        if (!buttonObject.CompareTag("interactive"))
            buttonObject.tag = "interactive";

        var returnInteractable = buttonObject.GetComponent<OrderReturnInteractable>();
        if (returnInteractable == null)
            returnInteractable = buttonObject.AddComponent<OrderReturnInteractable>();

        returnInteractable.Configure(returnSceneName);
    }

    static GameObject FindReturnButtonObject()
    {
        for (var i = 0; i < ReturnButtonObjectNames.Length; i++)
        {
            var buttonObject = GameObject.Find(ReturnButtonObjectNames[i]);
            if (buttonObject != null)
                return buttonObject;
        }

        return null;
    }
}
