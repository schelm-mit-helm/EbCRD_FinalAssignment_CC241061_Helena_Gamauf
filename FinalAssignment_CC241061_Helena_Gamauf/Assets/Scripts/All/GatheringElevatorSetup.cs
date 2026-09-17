using UnityEngine;

public class GatheringElevatorSetup : MonoBehaviour
{
    bool elevatorAreaReady;

    public GatheringElevatorDoors ElevatorDoors { get; private set; }

    public void SetupElevatorArea()
    {
        if (elevatorAreaReady)
            return;

        elevatorAreaReady = true;

        ElevatorDoors = GetComponent<GatheringElevatorDoors>();
        if (ElevatorDoors == null)
            ElevatorDoors = gameObject.AddComponent<GatheringElevatorDoors>();

        ElevatorDoors.ConfigureElevatorArea();
        ElevatorDoors.BeginArrivalSequence();
    }
}
