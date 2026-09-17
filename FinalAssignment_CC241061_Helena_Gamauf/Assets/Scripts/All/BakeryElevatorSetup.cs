using UnityEngine;

public class BakeryElevatorSetup : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] string destinationSceneName = SceneNames.GatheringDestination;

    [Header("Elevator Middle")]
    [SerializeField] Transform elevatorMiddleOverride;

    [Header("Environment")]
    [SerializeField] GameObject corridorPrefab;
    [SerializeField] Vector3 corridorPosition = new(0.787f, -0.025f, -10.96f);
    [SerializeField] Vector3 backDoorInteractPosition = new(5.515f, 1.2f, -9.759f);
    [SerializeField] float backDoorOpenAngle = 90f;

    BackDoorInteractable backDoorInteractable;
    bool elevatorAreaReady;

    public BackDoorInteractable BackDoorInteractable => backDoorInteractable;

    void Start()
    {
        SetupElevatorArea();
    }

    public void ApplySettings(string sceneName, Transform middleOverride)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
            destinationSceneName = sceneName;

        if (middleOverride != null)
            elevatorMiddleOverride = middleOverride;
    }

    void LoadCorridorPrefabIfNeeded()
    {
        if (corridorPrefab != null)
            return;

#if UNITY_EDITOR
        corridorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ElevatorCorridorUtility.CorridorPrefabAssetPath)
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ElevatorCorridorUtility.CorridorModelAssetPath);
#endif
    }

    public void SetupElevatorArea()
    {
        if (elevatorAreaReady)
            return;

        elevatorAreaReady = true;
        LoadCorridorPrefabIfNeeded();

        var root = GameObject.Find("ElevatorSequence");
        if (root == null)
        {
            root = new GameObject("ElevatorSequence");
            root.transform.position = Vector3.zero;
        }

        var corridorRoot = EnsureCorridor(root.transform);
        var cameraPoint = ResolveElevatorMiddlePoint(root.transform);
        var backDoor = EnsureBackDoor(corridorRoot);
        var blackDoor = ElevatorCorridorUtility.ConfigureBlackDoor(corridorRoot, shouldStartOpen: true);
        var leftDoor = ElevatorCorridorUtility.ConfigureElevatorDoor(
            ElevatorCorridorUtility.FindElevatorDoor(corridorRoot, isSecondDoor: false),
            shouldStartOpen: true,
            isSecondDoor: false);
        var rightDoor = ElevatorCorridorUtility.ConfigureElevatorDoor(
            ElevatorCorridorUtility.FindElevatorDoor(corridorRoot, isSecondDoor: true),
            shouldStartOpen: true,
            isSecondDoor: true);

        backDoorInteractable = EnsureBackDoorInteractable(
            cameraPoint,
            backDoor,
            blackDoor,
            leftDoor,
            rightDoor);
    }

    AutomaticDoor1 EnsureBackDoor(Transform corridorRoot)
    {
        var doorTransform = ElevatorCorridorUtility.FindBackDoorTransform(corridorRoot);
        if (doorTransform == null)
            return null;

        var door = doorTransform.GetComponent<AutomaticDoor1>();
        if (door == null)
            door = doorTransform.gameObject.AddComponent<AutomaticDoor1>();

        door.Configure(backDoorOpenAngle, false);
        return door;
    }

    BackDoorInteractable EnsureBackDoorInteractable(
        Transform cameraPoint,
        AutomaticDoor1 backDoor,
        SlidingDoor blackDoor,
        SlidingDoor leftDoor,
        SlidingDoor rightDoor)
    {
        Transform interactTarget = backDoor != null ? backDoor.transform : null;

        if (interactTarget == null)
        {
            var elevatorMiddle = ResolveElevatorMiddlePoint(null);
            var fallback = new GameObject("BackDoorInteractable");
            fallback.transform.position = elevatorMiddle != null
                ? elevatorMiddle.position
                : backDoorInteractPosition;
            fallback.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            interactTarget = fallback.transform;

            var fallbackCollider = fallback.AddComponent<BoxCollider>();
            fallbackCollider.size = new Vector3(1.8f, 2.2f, 0.4f);
        }
        else
        {
            EnsureDoorInteractCollider(interactTarget);
        }

        var interactable = interactTarget.GetComponent<BackDoorInteractable>();
        if (interactable == null)
            interactable = interactTarget.gameObject.AddComponent<BackDoorInteractable>();

        interactable.Configure(cameraPoint, backDoor, blackDoor, leftDoor, rightDoor, destinationSceneName);
        return interactable;
    }

    static void EnsureDoorInteractCollider(Transform doorTransform)
    {
        if (doorTransform.GetComponent<Collider>() != null)
            return;

        var renderers = doorTransform.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            var box = doorTransform.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(1.8f, 2.2f, 0.2f);
            return;
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var doorBox = doorTransform.gameObject.AddComponent<BoxCollider>();
        doorBox.center = doorTransform.InverseTransformPoint(bounds.center);
        doorBox.size = doorTransform.InverseTransformVector(bounds.size);

        var absSize = doorBox.size;
        doorBox.size = new Vector3(Mathf.Abs(absSize.x), Mathf.Abs(absSize.y), Mathf.Abs(absSize.z));
    }

    Transform ResolveElevatorMiddlePoint(Transform fallbackParent)
    {
        if (elevatorMiddleOverride != null)
            return elevatorMiddleOverride;

        var found = ElevatorCorridorUtility.FindElevatorMiddlePoint();
        if (found != null)
            return found;

        return EnsureMarker(
            fallbackParent,
            "ElevatorCameraPoint",
            backDoorInteractPosition,
            new Vector3(0f, 180f, 0f));
    }

    Transform EnsureCorridor(Transform parent)
    {
        var existing = ElevatorCorridorUtility.FindCorridorRoot();
        if (existing != null)
            return existing;

        if (corridorPrefab == null)
            return parent;

        var corridor = Instantiate(corridorPrefab, corridorPosition, Quaternion.identity, parent);
        corridor.name = "ElevatorCorridor";
        return corridor.transform;
    }

    static Transform EnsureMarker(Transform parent, string markerName, Vector3 worldPosition, Vector3 worldEuler)
    {
        var existing = SceneTransformFinder.FindRecursive(markerName);
        if (existing != null)
            return existing;

        var marker = new GameObject(markerName);
        marker.transform.SetParent(parent, true);
        marker.transform.position = worldPosition;
        marker.transform.rotation = Quaternion.Euler(worldEuler);
        return marker.transform;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (corridorPrefab != null)
            return;

        corridorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ElevatorCorridorUtility.CorridorPrefabAssetPath)
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ElevatorCorridorUtility.CorridorModelAssetPath);
    }
#endif
}
