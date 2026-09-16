using UnityEngine;

public class CustomerDoorEntryZone : MonoBehaviour
{
    const float DefaultTriggerWidth = 1.8f;
    const float DefaultTriggerHeight = 1.8f;
    const float DefaultTriggerLengthScale = 0.65f;
    const float DefaultTriggerLengthPadding = 0f;

    [SerializeField] float triggerWidth = DefaultTriggerWidth;
    [SerializeField] float triggerHeight = DefaultTriggerHeight;
    [SerializeField] float triggerLengthPadding = DefaultTriggerLengthPadding;

    void OnTriggerEnter(Collider other)
    {
        var movement = other.GetComponentInParent<CustomerPathMovement>();
        if (movement == null)
            return;

        OpenMainDoors(movement.IsExiting);
    }

    public static void CloseMainDoors()
    {
        CloseDoor("Door.001");
        CloseDoor("Door.002");
    }

    static void CloseDoor(string doorName)
    {
        var door = SceneTransformFinder.FindRecursive(doorName);
        if (door == null)
            return;

        door.GetComponent<AutomaticDoor1>()?.Close();
    }

    static void OpenMainDoors(bool oppositeDirection)
    {
        OpenDoor("Door.001", oppositeDirection);
        OpenDoor("Door.002", oppositeDirection);
    }

    static void OpenDoor(string doorName, bool oppositeDirection)
    {
        var door = SceneTransformFinder.FindRecursive(doorName);
        if (door == null)
            return;

        var automaticDoor = door.GetComponent<AutomaticDoor1>();
        if (automaticDoor == null)
            return;

        if (oppositeDirection)
            automaticDoor.OpenOpposite();
        else
            automaticDoor.Open();
    }

    public static void EnsureBetweenCheckpoints(Transform checkpointBeforeDoor, Transform checkpointAfterDoor)
    {
        if (checkpointBeforeDoor == null || checkpointAfterDoor == null)
            return;

        var zoneName = $"DoorEntryZone ({checkpointBeforeDoor.name} -> {checkpointAfterDoor.name})";
        if (GameObject.Find(zoneName) != null)
            return;

        var delta = checkpointAfterDoor.position - checkpointBeforeDoor.position;
        var flatDelta = delta;
        flatDelta.y = 0f;

        var zoneObject = new GameObject(zoneName);
        zoneObject.transform.position = checkpointBeforeDoor.position + delta * 0.5f;

        if (flatDelta.sqrMagnitude > 0.001f)
            zoneObject.transform.rotation = Quaternion.LookRotation(flatDelta.normalized);

        var box = zoneObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = Vector3.zero;

        var lengthAlongPath = flatDelta.magnitude > 0.001f
            ? flatDelta.magnitude * DefaultTriggerLengthScale
            : 0.8f;
        box.size = new Vector3(
            DefaultTriggerWidth,
            DefaultTriggerHeight,
            lengthAlongPath + DefaultTriggerLengthPadding);

        zoneObject.AddComponent<CustomerDoorEntryZone>();
    }
}
