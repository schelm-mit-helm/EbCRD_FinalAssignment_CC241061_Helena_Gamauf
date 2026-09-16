using UnityEngine;

public static class ElevatorCorridorUtility
{
    public const string CorridorPrefabAssetPath = "Assets/Prefabs/ElevatorCorridor.prefab";
    public const string CorridorModelAssetPath = "Assets/Prefabs/models/ElevatorCorridor.fbx";

    const float ElevatorDoorOneOpenZ = 0.578f;
    const float ElevatorDoorOneClosedZ = 1.057f;
    const float ElevatorDoorTwoOpenZ = 2.528f;
    const float ElevatorDoorTwoClosedZ = 2.03f;
    const float BlackDoorCloseOffsetZ = -0.8f;

    static readonly string[] CorridorRootNames =
    {
        "ElevatorCorridor",
        "CorridorAndElevator",
        "corrindor",
    };

    static readonly string[] ElevatorMiddleObjectNames =
    {
        "elevator middle",
        "Elevator Middle",
        "ElevatorMiddle",
        "ElevatorCameraPoint",
    };

    public static Transform FindCorridorRoot()
    {
        foreach (var objectName in CorridorRootNames)
        {
            var found = SceneTransformFinder.FindRecursive(objectName);
            if (found != null)
                return found;
        }

        return SceneTransformFinder.FindByNameContains("ElevatorCorridor")
            ?? SceneTransformFinder.FindByNameContains("CorridorElevator");
    }

    public static Transform FindElevatorMiddlePoint()
    {
        foreach (var objectName in ElevatorMiddleObjectNames)
        {
            var found = SceneTransformFinder.FindRecursiveIgnoreCase(objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    public static Transform FindElevatorDoor(Transform corridorRoot, bool isSecondDoor)
    {
        if (corridorRoot == null)
            return null;

        if (isSecondDoor)
        {
            return SceneTransformFinder.FindInHierarchy(corridorRoot, "ElevatorDoor.001", ignoreCase: true)
                ?? SceneTransformFinder.FindInHierarchy(corridorRoot, "elevatordoor.001", ignoreCase: true)
                ?? FindChildByExactName(corridorRoot, "ElevatorDoor.001");
        }

        var exact = SceneTransformFinder.FindInHierarchy(corridorRoot, "ElevatorDoor", ignoreCase: true)
            ?? SceneTransformFinder.FindInHierarchy(corridorRoot, "elevaterdoor", ignoreCase: true)
            ?? FindChildByExactName(corridorRoot, "ElevatorDoor");

        if (exact != null && exact.name.Contains(".001"))
            return null;

        return exact ?? FindFirstDoorWithoutSuffix(corridorRoot);
    }

    static Transform FindChildByExactName(Transform parent, string objectName)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var found = string.Equals(child.name, objectName, System.StringComparison.OrdinalIgnoreCase)
                ? child
                : FindChildByExactName(child, objectName);

            if (found != null)
                return found;
        }

        return null;
    }

    static Transform FindFirstDoorWithoutSuffix(Transform parent)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name.IndexOf("ElevatorDoor", System.StringComparison.OrdinalIgnoreCase) >= 0
                && child.name.IndexOf(".001", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                return child;
            }

            var nested = FindFirstDoorWithoutSuffix(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    public static SlidingDoor ConfigureElevatorDoor(
        Transform doorTransform,
        bool shouldStartOpen,
        bool isSecondDoor)
    {
        if (doorTransform == null)
            return null;

        var door = doorTransform.GetComponent<SlidingDoor>();
        if (door == null)
            door = doorTransform.gameObject.AddComponent<SlidingDoor>();

        var openZ = isSecondDoor ? ElevatorDoorTwoOpenZ : ElevatorDoorOneOpenZ;
        var closedZ = isSecondDoor ? ElevatorDoorTwoClosedZ : ElevatorDoorOneClosedZ;

        door.ConfigureLocalZPositions(openZ, closedZ, shouldStartOpen);
        return door;
    }

    public static SlidingDoor ConfigureBlackDoor(Transform corridorRoot, bool shouldStartOpen)
    {
        var doorTransform = SceneTransformFinder.FindRecursive("BlackDoor")
            ?? SceneTransformFinder.FindInHierarchy(corridorRoot, "Plane", ignoreCase: true);

        if (doorTransform == null)
            return null;

        var door = doorTransform.GetComponent<SlidingDoor>();
        if (door == null)
            door = doorTransform.gameObject.AddComponent<SlidingDoor>();

        door.Configure(new Vector3(0f, 0f, BlackDoorCloseOffsetZ), shouldStartOpen);
        return door;
    }

    public static Transform FindBackDoorTransform(Transform corridorRoot)
    {
        foreach (var name in new[] { "back door", "Back Door", "BackDoor" })
        {
            var found = SceneTransformFinder.FindRecursiveIgnoreCase(name);
            if (found != null)
                return found;
        }

        var containsMatch = SceneTransformFinder.FindByNameContains("back door")
            ?? SceneTransformFinder.FindByNameContains("backdoor");

        if (containsMatch != null)
            return containsMatch;

        if (corridorRoot == null)
            return null;

        foreach (var name in new[] { "back door", "Back Door", "BackDoor" })
        {
            var found = SceneTransformFinder.FindInHierarchy(corridorRoot, name, ignoreCase: true);
            if (found != null)
                return found;
        }

        return SceneTransformFinder.FindInHierarchyContains(corridorRoot, "back door")
            ?? SceneTransformFinder.FindInHierarchyContains(corridorRoot, "backdoor");
    }
}
