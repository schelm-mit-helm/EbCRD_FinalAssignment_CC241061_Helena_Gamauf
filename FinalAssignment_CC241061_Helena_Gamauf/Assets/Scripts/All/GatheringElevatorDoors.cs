using System.Collections;
using UnityEngine;

public class GatheringElevatorDoors : MonoBehaviour
{
    public static GatheringElevatorDoors Instance { get; private set; }

    [SerializeField] bool openDoorsOnArrival = true;
    [SerializeField] float returnMoveSpeed = 4.5f;

    SlidingDoor leftDoor;
    SlidingDoor rightDoor;
    SlidingDoor blackDoor;
    bool doorsConfigured;
    Coroutine arrivalRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ConfigureElevatorArea()
    {
        ConfigureDoors();
    }

    public void BeginArrivalSequence()
    {
        if (!openDoorsOnArrival)
            return;

        if (arrivalRoutine != null)
            StopCoroutine(arrivalRoutine);

        arrivalRoutine = StartCoroutine(ArrivalSequence());
    }

    public IEnumerator CloseDoors()
    {
        if (!ConfigureDoors())
            yield break;

        if (blackDoor != null)
        {
            blackDoor.Close();
            yield return blackDoor.WaitUntilIdle();
        }

        leftDoor?.Close();
        rightDoor?.Close();

        if (leftDoor != null)
            yield return leftDoor.WaitUntilIdle();

        if (rightDoor != null)
            yield return rightDoor.WaitUntilIdle();
    }

    public IEnumerator MovePlayerToStartPose(Transform player)
    {
        if (player == null || !GatheringPlayerStartPose.HasPose)
            yield break;

        var targetPosition = GatheringPlayerStartPose.Position;
        var targetRotation = GatheringPlayerStartPose.Rotation;
        var playerMovement = player.GetComponent<PlayerMovement>();
        var cameraTransform = ElevatorCutscenePlayer.ResolveCameraTransform(player, playerMovement);
        var cameraOffset = cameraTransform != null
            ? cameraTransform.position - player.position
            : Vector3.zero;

        const float positionThreshold = 0.03f;
        const float rotationThreshold = 0.5f;

        while (Vector3.Distance(player.position, targetPosition) > positionThreshold
            || Quaternion.Angle(player.rotation, targetRotation) > rotationThreshold)
        {
            player.position = Vector3.MoveTowards(
                player.position,
                targetPosition,
                returnMoveSpeed * Time.deltaTime);

            player.rotation = Quaternion.RotateTowards(
                player.rotation,
                targetRotation,
                returnMoveSpeed * 90f * Time.deltaTime);

            if (cameraTransform != null)
                cameraTransform.position = player.position + cameraOffset;

            yield return null;
        }

        player.SetPositionAndRotation(targetPosition, targetRotation);

        if (cameraTransform != null)
            cameraTransform.position = targetPosition + cameraOffset;
    }

    IEnumerator ArrivalSequence()
    {
        if (!ConfigureDoors())
            yield break;

        if (blackDoor != null)
            blackDoor.Open();

        leftDoor?.Open();
        rightDoor?.Open();

        if (blackDoor != null)
            yield return blackDoor.WaitUntilIdle();

        if (leftDoor != null)
            yield return leftDoor.WaitUntilIdle();

        if (rightDoor != null)
            yield return rightDoor.WaitUntilIdle();

        arrivalRoutine = null;
    }

    bool ConfigureDoors()
    {
        if (doorsConfigured)
            return leftDoor != null || rightDoor != null || blackDoor != null;

        doorsConfigured = true;

        var corridorRoot = ElevatorCorridorUtility.FindCorridorRoot();
        if (corridorRoot == null)
        {
            Debug.LogWarning($"{nameof(GatheringElevatorDoors)}: Could not find corridor in the scene.");
            return false;
        }

        leftDoor = ElevatorCorridorUtility.ConfigureElevatorDoor(
            ElevatorCorridorUtility.FindElevatorDoor(corridorRoot, isSecondDoor: false),
            shouldStartOpen: false,
            isSecondDoor: false);

        rightDoor = ElevatorCorridorUtility.ConfigureElevatorDoor(
            ElevatorCorridorUtility.FindElevatorDoor(corridorRoot, isSecondDoor: true),
            shouldStartOpen: false,
            isSecondDoor: true);

        blackDoor = ElevatorCorridorUtility.ConfigureBlackDoor(corridorRoot, shouldStartOpen: false);

        return leftDoor != null || rightDoor != null || blackDoor != null;
    }
}
