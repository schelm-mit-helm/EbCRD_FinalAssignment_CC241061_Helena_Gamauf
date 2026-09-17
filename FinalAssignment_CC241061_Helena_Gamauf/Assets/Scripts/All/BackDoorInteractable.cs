using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class BackDoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string promptMessage = "Press E to interact";
    [SerializeField] string gamepadPromptMessage = "Press X to interact";
    [SerializeField] string waitForTaskMessage = "I probably have to get my task first - Talk to the cashier first";
    [SerializeField] string destinationSceneName = SceneNames.GatheringDestination;
    [SerializeField] bool loadDestinationScene = true;
    [SerializeField] Transform elevatorCameraPoint;
    [SerializeField] AutomaticDoor1 backDoor;
    [SerializeField] SlidingDoor blackDoor;
    [SerializeField] SlidingDoor elevatorDoorLeft;
    [SerializeField] SlidingDoor elevatorDoorRight;
    [SerializeField] float cameraMoveSpeed = 2.5f;
    [SerializeField] float cameraRotateSpeed = 180f;
    [SerializeField] Vector3 lookForwardDirection = Vector3.right;
    [SerializeField] Vector3 lookBackDirection = Vector3.left;
    [SerializeField] float elevatorCameraPitchDown = 10f;
    [SerializeField] float returnElevatorCameraYaw = -100f;
    [SerializeField] float backDoorCloseTriggerX = 5.2f;
    [SerializeField] float sequencePause = 0.35f;
    [SerializeField] float returnElevatorPause = 0.8f;

    bool sequenceRunning;
    bool backDoorCloseStarted;
    bool backDoorOpenStarted;
    bool returnSequenceActive;
    CustomerSpawner returnSpawner;

    public bool CanInteract(PlayerInteractionContext context) => !sequenceRunning;

    public string GetPromptMessage(bool useGamepad) =>
        useGamepad ? gamepadPromptMessage : promptMessage;

    public void Interact(PlayerInteractionContext context) => TryInteract(context.Player);

    public void Configure(
        Transform cameraPoint,
        AutomaticDoor1 backDoorReference,
        SlidingDoor blackDoorReference,
        SlidingDoor leftDoorReference,
        SlidingDoor rightDoorReference,
        string sceneName = null)
    {
        elevatorCameraPoint = cameraPoint;
        backDoor = backDoorReference;
        blackDoor = blackDoorReference;
        elevatorDoorLeft = leftDoorReference;
        elevatorDoorRight = rightDoorReference;

        if (!string.IsNullOrWhiteSpace(sceneName))
            destinationSceneName = sceneName;
    }

    const float BakeryReturnPlayerY = 0.805f;

    public void ElevatorCloseSound()
    {
        AkUnitySoundEngine.PostEvent("Play_elevator_door_close", gameObject);
        AkUnitySoundEngine.PostEvent("FadeBetweenScenes", null);
    }

    public void ElevatorOpenSound()
    {
        AkUnitySoundEngine.PostEvent("Play_elevator_door_close", gameObject);
    }
    
    public void TryInteract(Transform player)
    {
        if (sequenceRunning || player == null)
            return;

        var orderService = CustomerOrderService.EnsurePersistentInstance();
        if (orderService == null || !orderService.PlayerHasTaskList)
        {
            ComicSpeechBoxUI.Show(waitForTaskMessage);
            return;
        }

        StartCoroutine(ElevatorSequence(player));
    }

    IEnumerator ElevatorSequence(Transform player)
    {
        sequenceRunning = true;

        var playerMovement = player.GetComponent<PlayerMovement>();
        var playerJump = player.GetComponent<PlayerJump>();
        var playerRenderer = player.GetComponent<Renderer>();
        var playerRigidbody = player.GetComponent<Rigidbody>();
        var cameraTransform = ElevatorCutscenePlayer.ResolveCameraTransform(player, playerMovement);
        var cameraPoint = ElevatorCutscenePlayer.ResolveElevatorCameraPoint(elevatorCameraPoint);

        var preservedPlayerY = player.position.y;
        var preservedPlayerZ = player.position.z;
        var preservedCameraY = cameraTransform != null ? cameraTransform.position.y : preservedPlayerY + 1f;
        var preservedCameraZ = cameraTransform != null ? cameraTransform.position.z : preservedPlayerZ;

        backDoorCloseStarted = false;

        var orderService = CustomerOrderService.EnsurePersistentInstance();
        if (orderService != null)
        {
            var elevatorSpawn = cameraPoint != null
                ? new Vector3(cameraPoint.position.x, BakeryReturnPlayerY, cameraPoint.position.z)
                : new Vector3(player.position.x, BakeryReturnPlayerY, player.position.z);

            orderService.SavePlayerReturnPose(
                player.position,
                player.rotation,
                cameraTransform != null ? cameraTransform.localPosition : Vector3.zero,
                cameraTransform != null ? cameraTransform.localRotation : Quaternion.identity,
                playerMovement != null ? playerMovement.CameraPitch : 0f,
                preservedCameraY,
                preservedCameraZ,
                elevatorSpawn);
        }

        playerMovement?.SetControlsLocked(true);
        if (playerJump != null)
            playerJump.enabled = false;

        if (playerRenderer != null)
            playerRenderer.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        ElevatorCutscenePlayer.PreservePlayerHeight(player, preservedPlayerY);

        if (backDoor != null)
        {
            AkUnitySoundEngine.PostEvent("Play_dorm_door_opening", gameObject);
            yield return backDoor.RotateFullyOpen();
        }

        ElevatorCutscenePlayer.PreservePlayerHeight(player, preservedPlayerY);

        
        
        yield return EnterElevator(
            player,
            cameraTransform,
            cameraPoint,
            preservedPlayerY,
            preservedPlayerZ,
            preservedCameraY,
            preservedCameraZ);

        ElevatorCutscenePlayer.PreservePlayerHeight(player, preservedPlayerY);

        if (sequencePause > 0f)
            yield return new WaitForSeconds(sequencePause);

        if (blackDoor != null)
        {
            blackDoor.Close();
            yield return blackDoor.WaitUntilIdle();
        }

        ElevatorCutscenePlayer.PreservePlayerHeight(player, preservedPlayerY);

        if (elevatorDoorLeft != null)
            elevatorDoorLeft.Close();

        if (elevatorDoorRight != null)
        {
           ElevatorCloseSound();
            elevatorDoorRight.Close();
        }
           
        

        if (elevatorDoorLeft != null)
            yield return elevatorDoorLeft.WaitUntilIdle();

        if (elevatorDoorRight != null)
            yield return elevatorDoorRight.WaitUntilIdle();

        if (backDoor != null && backDoorCloseStarted)
            yield return backDoor.WaitUntilIdle();

        ElevatorCutscenePlayer.PreservePlayerHeight(player, preservedPlayerY);

        if (loadDestinationScene)
        {
            CustomerOrderService.EnsurePersistentInstance().PreserveActiveCustomerForGathering();
            SceneManager.LoadScene(destinationSceneName);
            yield break;
        }

        sequenceRunning = false;
    }

    IEnumerator EnterElevator(
        Transform player,
        Transform cameraTransform,
        Transform cameraPoint,
        float preservedPlayerY,
        float preservedPlayerZ,
        float preservedCameraY,
        float preservedCameraZ)
    {
        if (cameraTransform != null)
        {
            cameraTransform.SetParent(null);
            cameraTransform.position = new Vector3(
                cameraTransform.position.x,
                preservedCameraY,
                preservedCameraZ);
        }

        yield return ElevatorCutscenePlayer.RotateYawToLookDirection(player, lookForwardDirection, cameraRotateSpeed);
        ElevatorCutscenePlayer.PreservePlayerHeight(player, preservedPlayerY);

        if (cameraTransform != null && cameraPoint != null)
        {
            yield return ElevatorCutscenePlayer.MoveAlongXAxis(
                player,
                cameraTransform,
                cameraPoint.position.x,
                preservedPlayerY,
                preservedPlayerZ,
                preservedCameraY,
                preservedCameraZ,
                cameraMoveSpeed,
                (currentX, startX, targetX) => TryStartBackDoorClose(currentX, startX, targetX));
        }

        ElevatorCutscenePlayer.PreservePlayerHeight(player, preservedPlayerY);

        if (cameraTransform != null)
            yield return ElevatorCutscenePlayer.RotateCameraToLookDirection(
                cameraTransform,
                lookBackDirection,
                cameraRotateSpeed,
                elevatorCameraPitchDown);

        ElevatorCutscenePlayer.PreservePlayerHeight(player, preservedPlayerY);
    }

    void TryStartBackDoorClose(float currentX, float startX, float targetX)
    {
        if (backDoorCloseStarted || backDoor == null)
            return;

        if (!ElevatorCutscenePlayer.HasPassedTriggerX(currentX, startX, targetX, backDoorCloseTriggerX))
            return;

        backDoorCloseStarted = true;
        backDoor.BeginRotateFullyClosed();
    }

    void TryStartBackDoorOpen(float currentX, float startX, float targetX)
    {
        if (backDoorOpenStarted || backDoor == null)
            return;

        if (!ElevatorCutscenePlayer.HasPassedTriggerX(currentX, startX, targetX, backDoorCloseTriggerX))
            return;

        backDoorOpenStarted = true;
        backDoor.OpenOpposite();

        if (returnSequenceActive)
            returnSpawner?.ScheduleReturnCustomerHandoff();
    }

    void PrepareReturnDoorsClosed()
    {
        backDoorCloseStarted = false;
        backDoorOpenStarted = false;

        if (elevatorDoorLeft != null)
            elevatorDoorLeft.SnapClosed();

        if (elevatorDoorRight != null)
            elevatorDoorRight.SnapClosed();

        if (backDoor != null)
            backDoor.SnapClosed();
    }

    void BeginOpenReturnElevatorDoors()
    {
        if (elevatorDoorLeft != null)
        {
            ElevatorOpenSound();
            elevatorDoorLeft.Open();
        }

        if (elevatorDoorRight != null)
            elevatorDoorRight.Open();
    }

    IEnumerator OpenReturnElevatorDoors()
    {
        BeginOpenReturnElevatorDoors();

        if (elevatorDoorLeft != null)
            yield return elevatorDoorLeft.WaitUntilIdle();

        if (elevatorDoorRight != null)
            yield return elevatorDoorRight.WaitUntilIdle();
    }

    public IEnumerator ReturnFromElevatorSequence(Transform player, CustomerSpawner spawner)
    {
        const float returnPlayerY = BakeryReturnPlayerY;

        if (sequenceRunning || player == null)
        {
            spawner?.HandlePostElevatorReturn();
            yield break;
        }

        var orderService = CustomerOrderService.Instance;
        if (orderService == null || !orderService.ReturningFromGathering)
        {
            spawner?.HandlePostElevatorReturn();
            yield break;
        }

        var hasSavedPose = orderService.TryGetReturnPlayerPose(
            out var savedPlayerPosition,
            out var savedPlayerRotation,
            out var savedCameraLocalPosition,
            out var savedCameraLocalRotation,
            out var savedCameraPitch);

        if (!hasSavedPose)
        {
            var fallbackMovement = player.GetComponent<PlayerMovement>();
            var fallbackCamera = ElevatorCutscenePlayer.ResolveCameraTransform(player, fallbackMovement);
            savedPlayerPosition = player.position;
            savedPlayerRotation = player.rotation;
            savedCameraLocalPosition = fallbackCamera != null
                ? fallbackCamera.localPosition
                : new Vector3(0f, 1.6f, 0f);
            savedCameraLocalRotation = fallbackCamera != null
                ? fallbackCamera.localRotation
                : Quaternion.identity;
            savedCameraPitch = fallbackMovement != null ? fallbackMovement.CameraPitch : 0f;
        }

        sequenceRunning = true;
        returnSequenceActive = true;
        returnSpawner = spawner;

        var playerMovement = player.GetComponent<PlayerMovement>();
        var playerJump = player.GetComponent<PlayerJump>();
        var playerRigidbody = player.GetComponent<Rigidbody>();
        var cameraTransform = ElevatorCutscenePlayer.ResolveCameraTransform(player, playerMovement);
        var cameraPoint = ElevatorCutscenePlayer.ResolveElevatorCameraPoint(elevatorCameraPoint);

        orderService.TryGetReturnCameraStart(out var preservedCameraY, out var preservedCameraZ);
        if (!hasSavedPose)
        {
            preservedCameraY = savedPlayerPosition.y + savedCameraLocalPosition.y;
            preservedCameraZ = savedPlayerPosition.z + savedCameraLocalPosition.z;
        }

        var elevatorSpawn = orderService.TryGetReturnElevatorSpawn(out var savedElevatorSpawn)
            ? new Vector3(savedElevatorSpawn.x, returnPlayerY, savedElevatorSpawn.z)
            : cameraPoint != null
                ? new Vector3(cameraPoint.position.x, returnPlayerY, cameraPoint.position.z)
                : new Vector3(savedPlayerPosition.x, returnPlayerY, savedPlayerPosition.z);

        if (playerMovement != null)
        {
            playerMovement.SetControlsLocked(true);
            playerMovement.enabled = false;
        }

        if (playerJump != null)
            playerJump.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        player.SetPositionAndRotation(
            elevatorSpawn,
            Quaternion.Euler(0f, returnElevatorCameraYaw, 0f));

        if (cameraTransform != null)
        {
            cameraTransform.SetParent(null);
            cameraTransform.position = new Vector3(
                elevatorSpawn.x,
                preservedCameraY,
                preservedCameraZ);
            cameraTransform.rotation = Quaternion.Euler(
                elevatorCameraPitchDown,
                returnElevatorCameraYaw,
                0f);
        }

        PrepareReturnDoorsClosed();

        BeginOpenReturnElevatorDoors();

        if (returnElevatorPause > 0f)
            yield return new WaitForSeconds(returnElevatorPause);

        if (cameraTransform != null)
        {
            yield return ElevatorCutscenePlayer.MoveAlongXAxis(
                player,
                cameraTransform,
                savedPlayerPosition.x,
                returnPlayerY,
                savedPlayerPosition.z,
                preservedCameraY,
                preservedCameraZ,
                cameraMoveSpeed,
                (currentX, startX, targetX) => TryStartBackDoorOpen(currentX, startX, targetX));
        }
        else
        {
            player.position = savedPlayerPosition;
        }

        if (backDoor != null && backDoorOpenStarted)
            yield return backDoor.RotateFullyClosed();

        if (playerMovement != null)
        {
            playerMovement.RestoreAfterElevatorCutsceneKeepingCurrentView(cameraTransform);
            playerMovement.enabled = true;
        }

        if (playerJump != null)
            playerJump.enabled = true;

        if (playerRigidbody != null)
            playerRigidbody.isKinematic = false;

        sequenceRunning = false;
        returnSequenceActive = false;
        returnSpawner = null;
        spawner?.HandlePostElevatorReturn();
    }
}
