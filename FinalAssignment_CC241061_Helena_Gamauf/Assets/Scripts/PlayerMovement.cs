using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    static readonly Type[] InteractablePriority =
    {
        typeof(CashierInteractable),
        typeof(BackDoorInteractable),
        typeof(OrderReturnInteractable),
        typeof(OrderItemDropZone),
    };

    [SerializeField] private float movementPerSecond;
    [SerializeField] private float rotationPerSecond;
    [SerializeField] private GameObject camera;
    [SerializeField] private InputActionReference rotateCamera;
    [SerializeField] private InputActionReference movePlayer;
    [SerializeField] private Inventory inventory;
    [SerializeField] private float playerHealth;
    [SerializeField] private float mouseSpeed = 1f;
    [SerializeField] private float gamepadCameraSpeedMultiplier = 7f;
    [SerializeField] private float interactRayDistance = 5f;
    [SerializeField] private string interactiveTag = "interactive";
    [SerializeField] private string droppedItemTag = "DroppedItem";
    [SerializeField] private InteractionPromptUI interactionPrompt;
    [SerializeField] private string droppedItemPromptMessage = "Press E to pick up";
    [SerializeField] private string droppedItemGamepadPromptMessage = "Press X to pick up";
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float crouchCameraDrop = 0.35f;
    [SerializeField] private float crouchColliderHeight = 1.2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float walkShakeAmount = 0.012f;
    [SerializeField] private float walkShakeSpeed = 9f;
    [SerializeField] private float sprintShakeAmount = 0.025f;
    [SerializeField] private float sprintShakeSpeed = 13f;
    [SerializeField] private float walkShakeRoll = 0.3f;
    [SerializeField] private float sprintShakeRoll = 0.65f;
    
    [Header("Wwise Footsteps")]
    [SerializeField] private AK.Wwise.Event footstepEventPantryWashing;
    [SerializeField] private AK.Wwise.Event footstepEventFreezer;
    private AK.Wwise.Event footstepEvent;
    [SerializeField] private float footstepInterval = 0.3f;
    
    private float footstepTimer;

    private Vector3 inputDirection = Vector3.zero;
    private float mouseX;
    private float mouseY;
    private float xRotation = 0f;
    private float currentCrouchBlend;
    private float standingCapsuleHeight;
    private Vector3 standingCapsuleCenter;
    private Vector3 standingCameraLocalPosition;
    private bool isSprinting;
    private bool isCrouching;
    private bool controlsLocked;
    private bool slipperyFloorActive;
    private float baseMouseSpeed;
    private float cameraShakePhase;
    private float cameraShakeIntensity;

    public Transform CameraTransform => cameraTransform;
    public bool ControlsLocked => controlsLocked;
    public float CameraPitch => xRotation;

    
    public void PrepareForElevatorCutscene()
    {
        SetControlsLocked(true);
        inputDirection = Vector3.zero;
    }

    public void RestoreAfterElevatorCutscene(
        Vector3 worldPosition,
        Quaternion worldRotation,
        Vector3 cameraLocalPosition,
        Quaternion cameraLocalRotation,
        float cameraPitch)
    {
        transform.SetPositionAndRotation(worldPosition, worldRotation);

        if (cameraTransform != null)
        {
            cameraTransform.SetParent(transform);
            cameraTransform.localPosition = cameraLocalPosition;
            cameraTransform.localRotation = cameraLocalRotation;
        }

        xRotation = cameraPitch;
        mouseX = 0f;
        mouseY = 0f;
        SetControlsLocked(false);
    }

    public void RestoreAfterElevatorCutsceneKeepingCurrentView(Transform cutsceneCamera)
    {
        if (cutsceneCamera != null)
        {
            float yaw = cutsceneCamera.eulerAngles.y;
            float pitch = cutsceneCamera.eulerAngles.x;
            if (pitch > 180f)
                pitch -= 360f;

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cutsceneCamera.SetParent(transform);
            cutsceneCamera.localPosition = standingCameraLocalPosition;
            cutsceneCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            xRotation = pitch;
        }

        mouseX = 0f;
        mouseY = 0f;
        SetControlsLocked(false);
    }

    public void SetControlsLocked(bool locked)
    {
        controlsLocked = locked;

        if (!locked)
            return;

        inputDirection = Vector3.zero;
        mouseX = 0f;
        mouseY = 0f;

        if (rb != null)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public void SetSlipperyFloor(bool enabled)
    {
        slipperyFloorActive = enabled;
    }

    private Transform cameraTransform;
    private CapsuleCollider capsuleCollider;
    private Renderer bodyRenderer;
    private GameObject footstepEmitter;
   

    private Rigidbody rb;
    private PlayerJump playerJump;

    private InputAction dropItemAction;
    private InputAction interactionAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction selectSlot1Action;
    private InputAction selectSlot2Action;
    private InputAction selectSlot3Action;
    private InputAction selectSlot4Action;
    private InputAction selectSlot5Action;
    private InputAction inventoryScrollAction;

    private void Awake()
    {
        baseMouseSpeed = mouseSpeed;
        rb = GetComponent<Rigidbody>();
        playerJump = GetComponent<PlayerJump>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>()?.GetComponent<Renderer>()
            ?? GetComponentInChildren<MeshRenderer>();

        footstepEmitter = gameObject;
        if (footstepEmitter.GetComponent<AkGameObj>() == null)
            footstepEmitter.AddComponent<AkGameObj>();
        Debug.Log("footstepEventPantryWashing1:" + footstepEventPantryWashing.ToString());
        MazeThemeData currentTheme = MazeThemeManager.CurrentTheme;
        
        if (currentTheme != null)
        {
            Debug.Log("currentTheme:" + currentTheme.themeName.ToString());
            if (currentTheme.themeName == "Pantry" || currentTheme.themeName == "Washing")
            {
                footstepEvent = footstepEventPantryWashing;
                 Debug.Log("footstepEvent1:" + footstepEvent.ToString());
               Debug.Log("footstepEventPantryWashing2:" + footstepEventPantryWashing.ToString());
            }
            else if (currentTheme.themeName == "Freezer")
            {
                footstepEvent = footstepEventFreezer;
                Debug.Log("footstepEvent2:" + footstepEvent.ToString());
                Debug.Log("footstepEventFreezer:" + footstepEventFreezer.ToString());
                
            }
        }
        else
        {
            footstepEvent = footstepEventPantryWashing;
            Debug.Log("footstepEvent3:" + footstepEvent.ToString());
            Debug.Log("footstepEventPantryWashing3:" + footstepEventPantryWashing.ToString());
        }

        if (rb != null)
        {
            rb.freezeRotation = true;
        }

        if (camera != null)
        {
            cameraTransform = camera.transform;
            standingCameraLocalPosition = cameraTransform.localPosition;
        }

        if (capsuleCollider != null)
        {
            standingCapsuleHeight = capsuleCollider.height;
            standingCapsuleCenter = capsuleCollider.center;
        }

        if (interactionPrompt == null)
            interactionPrompt = FindFirstObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);

        if (interactionPrompt == null)
            interactionPrompt = InteractionPromptUI.EnsureInstance();

        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
        }
    }

    void BindPlayerMapActions()
    {
        var playerMap = movePlayer?.action?.actionMap;
        if (playerMap == null)
        {
            return;
        }

        dropItemAction = playerMap.FindAction("dropItem");
        interactionAction = playerMap.FindAction("interaction");
        sprintAction = playerMap.FindAction("Sprint");
        crouchAction = playerMap.FindAction("crouch");
        selectSlot1Action = playerMap.FindAction("selectSlot1");
        selectSlot2Action = playerMap.FindAction("selectSlot2");
        selectSlot3Action = playerMap.FindAction("selectSlot3");
        selectSlot4Action = playerMap.FindAction("selectSlot4");
        selectSlot5Action = playerMap.FindAction("selectSlot5");
        inventoryScrollAction = playerMap.FindAction("inventoryScroll");
    }

    private void OnEnable()
    {
        rotateCamera.action.Enable();
        rotateCamera.action.performed += OnCameraRotation;
        rotateCamera.action.canceled += OnCameraRotationStop;

        movePlayer.action.Enable();
        movePlayer.action.performed += OnPlayerMovement;
        movePlayer.action.canceled += OnPlayerMovementStop;

        BindPlayerMapActions();
        EnablePlayerMapActions(true);
    }

    private void OnDisable()
    {
        rotateCamera.action.performed -= OnCameraRotation;
        rotateCamera.action.canceled -= OnCameraRotationStop;

        movePlayer.action.performed -= OnPlayerMovement;
        movePlayer.action.canceled -= OnPlayerMovementStop;

        EnablePlayerMapActions(false);
    }

    void EnablePlayerMapActions(bool enable)
    {
        if (enable)
        {
            SubscribeInventoryAction(dropItemAction, OnDropItem);
            SubscribeInventoryAction(interactionAction, OnInteract);
            SubscribeInventoryAction(selectSlot1Action, OnSelectSlot1);
            SubscribeInventoryAction(selectSlot2Action, OnSelectSlot2);
            SubscribeInventoryAction(selectSlot3Action, OnSelectSlot3);
            SubscribeInventoryAction(selectSlot4Action, OnSelectSlot4);
            SubscribeInventoryAction(selectSlot5Action, OnSelectSlot5);
            SubscribeInventoryAction(inventoryScrollAction, OnInventoryScroll);

            sprintAction?.Enable();
            crouchAction?.Enable();
        }
        else
        {
            UnsubscribeInventoryAction(dropItemAction, OnDropItem);
            UnsubscribeInventoryAction(interactionAction, OnInteract);
            UnsubscribeInventoryAction(selectSlot1Action, OnSelectSlot1);
            UnsubscribeInventoryAction(selectSlot2Action, OnSelectSlot2);
            UnsubscribeInventoryAction(selectSlot3Action, OnSelectSlot3);
            UnsubscribeInventoryAction(selectSlot4Action, OnSelectSlot4);
            UnsubscribeInventoryAction(selectSlot5Action, OnSelectSlot5);
            UnsubscribeInventoryAction(inventoryScrollAction, OnInventoryScroll);

            sprintAction?.Disable();
            crouchAction?.Disable();
        }
    }

    static void SubscribeInventoryAction(InputAction action, Action<InputAction.CallbackContext> handler)
    {
        if (action == null)
        {
            return;
        }

        action.Enable();
        action.performed += handler;
    }

    static void UnsubscribeInventoryAction(InputAction action, Action<InputAction.CallbackContext> handler)
    {
        if (action == null)
        {
            return;
        }

        action.performed -= handler;
        action.Disable();
    }

    private void Start()
    {
        ApplyMouseSensitivity();
        GameSettings.SettingsChanged += ApplyMouseSensitivity;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        GameSettings.SettingsChanged -= ApplyMouseSensitivity;
    }

    void ApplyMouseSensitivity()
    {
        mouseSpeed = baseMouseSpeed * GameSettings.MouseSensitivity;
    }

    private void OnPlayerMovement(InputAction.CallbackContext obj)
    {
        Vector2 inputVector = obj.ReadValue<Vector2>();
        StartPlayerMovement(inputVector.y, inputVector.x);
    }

    private void OnPlayerMovementStop(InputAction.CallbackContext obj)
    {
        StopPlayerMovement();
    }

    private void OnCameraRotation(InputAction.CallbackContext obj)
    {
        Vector2 input = obj.ReadValue<Vector2>();
        float speed = mouseSpeed;
        if (obj.control.device is Gamepad)
        {
            speed *= gamepadCameraSpeedMultiplier;
        }

        mouseX = input.x * speed;
        mouseY = input.y * speed;
    }

    private void OnCameraRotationStop(InputAction.CallbackContext obj)
    {
        mouseX = 0f;
        mouseY = 0f;
    }

    private void StartPlayerMovement(float inputDirectionForwardAxis, float inputDirectionRightAxis)
    {
        inputDirection = new Vector3(inputDirectionRightAxis, 0, inputDirectionForwardAxis);
    }

    private void StopPlayerMovement()
    {
        inputDirection = Vector3.zero;
    }

    public bool IsWalking => inputDirection.sqrMagnitude > 0.01f;

    private void Update()
    {
        if (controlsLocked)
        {
            interactionPrompt?.Hide();
            return;
        }
        
        //Footstep sound logic
        if (inputDirection != Vector3.zero)
        {
            Debug.Log("We are walking");
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                footstepEvent.Post(gameObject);
                footstepTimer = footstepInterval;
            }
            
        }
        else
        {
            Debug.Log("We are not walking");
        }

        InputDeviceTracker.UpdateFromInput();
        RotatePlayerAndCamera();
        UpdateSprintAndCrouch();
        ApplyCameraShake();

        Vector2 moveInput = movePlayer.action.ReadValue<Vector2>();
        inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        UpdateMovementRtpc(IsWalking);
        UpdateInteractionPrompt();
        
        //HandleFootsteps(); -- I put this logic at the top of update
    }

    void UpdateSprintAndCrouch()
    {
        isCrouching = crouchAction != null && crouchAction.IsPressed();
        isSprinting = !isCrouching && sprintAction != null && sprintAction.IsPressed();

        var targetCrouchBlend = isCrouching ? 1f : 0f;
        currentCrouchBlend = Mathf.MoveTowards(
            currentCrouchBlend,
            targetCrouchBlend,
            crouchTransitionSpeed * Time.deltaTime);

        ApplyCrouch(currentCrouchBlend);
    }

    void ApplyCrouch(float blend)
    {
        if (cameraTransform != null)
        {
            var cameraPosition = standingCameraLocalPosition;
            cameraPosition.y -= crouchCameraDrop * blend;
            cameraTransform.localPosition = cameraPosition;
        }

        if (capsuleCollider == null)
        {
            return;
        }

        var standingBottom = standingCapsuleCenter.y - standingCapsuleHeight * 0.5f;
        var crouchedCenterY = standingBottom + crouchColliderHeight * 0.5f;

        capsuleCollider.height = Mathf.Lerp(standingCapsuleHeight, crouchColliderHeight, blend);
        capsuleCollider.center = new Vector3(
            standingCapsuleCenter.x,
            Mathf.Lerp(standingCapsuleCenter.y, crouchedCenterY, blend),
            standingCapsuleCenter.z);
    }

    void ApplyCameraShake()
    {
        if (cameraTransform == null)
            return;

        bool shouldShake = !controlsLocked
            && IsWalking
            && (playerJump == null || playerJump.IsGrounded);

        cameraShakeIntensity = Mathf.MoveTowards(
            cameraShakeIntensity,
            shouldShake ? 1f : 0f,
            Time.deltaTime * 8f);

        if (cameraShakeIntensity <= 0.001f)
            return;

        float amount = isSprinting ? sprintShakeAmount : walkShakeAmount;
        float speed = isSprinting ? sprintShakeSpeed : walkShakeSpeed;
        float rollAmount = isSprinting ? sprintShakeRoll : walkShakeRoll;
        amount *= cameraShakeIntensity;
        rollAmount *= cameraShakeIntensity;

        if (shouldShake)
            cameraShakePhase += Time.deltaTime * speed;

        float bobY = Mathf.Sin(cameraShakePhase * Mathf.PI * 2f) * amount;
        float bobX = Mathf.Sin(cameraShakePhase * Mathf.PI) * amount * 0.4f;
        float roll = Mathf.Sin(cameraShakePhase * Mathf.PI) * rollAmount;

        var position = cameraTransform.localPosition;
        position.y += bobY;
        position.x += bobX;
        cameraTransform.localPosition = position;
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, roll);
    }

    void OnInventoryScroll(InputAction.CallbackContext context)
    {
        if (inventory == null)
        {
            return;
        }

        var input = context.ReadValue<Vector2>();

        if (input.y > 0.01f || input.x < -0.01f)
        {
            inventory.SelectAdjacentFilledSlot(-1);
        }
        else if (input.y < -0.01f || input.x > 0.01f)
        {
            inventory.SelectAdjacentFilledSlot(1);
        }
    }

    void OnDropItem(InputAction.CallbackContext _) => inventory?.DropSelectedItem();
    void OnInteract(InputAction.CallbackContext _) => TryInteract();
    void OnSelectSlot1(InputAction.CallbackContext _) => inventory?.TrySelectSlot(0);
    void OnSelectSlot2(InputAction.CallbackContext _) => inventory?.TrySelectSlot(1);
    void OnSelectSlot3(InputAction.CallbackContext _) => inventory?.TrySelectSlot(2);
    void OnSelectSlot4(InputAction.CallbackContext _) => inventory?.TrySelectSlot(3);
    void OnSelectSlot5(InputAction.CallbackContext _) => inventory?.TrySelectSlot(4);

    private void FixedUpdate()
    {
        if (controlsLocked)
            return;

        MovePlayer();
    }

    private void RotatePlayerAndCamera()
    {
        transform.Rotate(Vector3.up * mouseX * rotationPerSecond * Time.deltaTime);

        xRotation -= mouseY * rotationPerSecond * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void MovePlayer()
    {
        if (rb == null)
            return;

        Vector3 moveDirection =
            transform.forward * inputDirection.z +
            transform.right * inputDirection.x;

        moveDirection = moveDirection.normalized;

        var speed = movementPerSecond;
        if (currentCrouchBlend > 0f)
        {
            speed *= Mathf.Lerp(1f, crouchSpeedMultiplier, currentCrouchBlend);
        }
        else if (isSprinting && inputDirection.sqrMagnitude > 0.01f)
        {
            speed *= sprintMultiplier;
        }

        Vector3 targetVelocity = moveDirection * speed;

        if (slipperyFloorActive)
        {
            const float acceleration = 1.5f;
            const float deceleration = 0.5f;

            var horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (inputDirection.sqrMagnitude > 0.01f)
            {
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetVelocity,
                    acceleration * speed * Time.fixedDeltaTime);
            }
            else
            {
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    deceleration * speed * Time.fixedDeltaTime);
            }

            rb.linearVelocity = new Vector3(
                horizontalVelocity.x,
                rb.linearVelocity.y,
                horizontalVelocity.z);
            return;
        }

        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            rb.linearVelocity.y,
            targetVelocity.z
        );
    }

    private void UpdateMovementRtpc(bool isWalking)
    {
        if (!AkUnitySoundEngine.IsInitialized())
            return;

        float rtpcValue = 0f;

        if (isWalking && movementPerSecond > 0f)
        {
            var speedMultiplier = 1f;
            if (currentCrouchBlend > 0f)
            {
                speedMultiplier = Mathf.Lerp(1f, crouchSpeedMultiplier, currentCrouchBlend);
            }
            else if (isSprinting)
            {
                speedMultiplier = sprintMultiplier;
            }

            rtpcValue = Mathf.Clamp01(inputDirection.magnitude) * 100f * speedMultiplier;
        }

        //AkUnitySoundEngine.SetRTPCValue("PlayerMovementSpeed", rtpcValue);
    }

    void TryInteract()
    {
        if (controlsLocked || InteractionPromptUI.IsBlocked)
            return;

        var context = new PlayerInteractionContext
        {
            Player = transform,
            Inventory = inventory
        };

        var interactable = FindLookedAtInteractable(context);
        if (interactable != null)
        {
            interactable.Interact(context);
            return;
        }

        if (inventory == null)
            return;

        var droppedItem = GetLookedAtDroppedItem();
        if (droppedItem != null)
        {
            inventory.PickupDroppedItem(droppedItem);
            return;
        }

        var itemPickup = GetLookedAtItemPickup();
        if (itemPickup != null)
            itemPickup.TryPickup(inventory);
    }

    void UpdateInteractionPrompt()
    {
        if (interactionPrompt == null)
            return;

        if (InteractionPromptUI.IsBlocked)
        {
            interactionPrompt.Hide();
            return;
        }

        var useGamepad = InputDeviceTracker.IsUsingGamepad;
        var context = new PlayerInteractionContext
        {
            Player = transform,
            Inventory = inventory
        };

        var interactable = FindLookedAtInteractable(context);
        if (interactable != null)
        {
            interactionPrompt.Show(interactable.GetPromptMessage(useGamepad));
            return;
        }

        var droppedItem = GetLookedAtDroppedItem();
        if (droppedItem != null && droppedItem.CanBePickedUp)
        {
            interactionPrompt.Show(useGamepad ? droppedItemGamepadPromptMessage : droppedItemPromptMessage);
            return;
        }

        var itemPickup = GetLookedAtItemPickup();
        if (itemPickup != null && itemPickup.CanBePickedUp)
        {
            interactionPrompt.Show(itemPickup.GetPromptMessage(useGamepad));
            return;
        }

        interactionPrompt.Hide();
    }

    IInteractable FindLookedAtInteractable(PlayerInteractionContext context)
    {
        foreach (var hit in GetSortedInteractionHits())
        {
            foreach (var interactableType in InteractablePriority)
            {
                if (hit.collider.GetComponent(interactableType) is IInteractable interactable
                    && interactable.CanInteract(context))
                    return interactable;

                if (hit.collider.GetComponentInParent(interactableType) is IInteractable parentInteractable
                    && parentInteractable.CanInteract(context))
                    return parentInteractable;
            }
        }

        return null;
    }

    RaycastHit[] GetSortedInteractionHits()
    {
        if (camera == null)
            return Array.Empty<RaycastHit>();

        var ray = new Ray(camera.transform.position, camera.transform.forward);
        var hits = Physics.RaycastAll(ray, interactRayDistance, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        return hits;
    }

    DroppedItem GetLookedAtDroppedItem()
    {
        foreach (var hit in GetSortedInteractionHits())
        {
            DroppedItem droppedItem = null;
            if (hit.collider.CompareTag(droppedItemTag))
                droppedItem = hit.collider.GetComponent<DroppedItem>();
            else
                droppedItem = hit.collider.GetComponentInParent<DroppedItem>();

            if (droppedItem != null)
                return droppedItem;
        }

        return null;
    }

    InteractableItemPickup GetLookedAtItemPickup()
    {
        foreach (var hit in GetSortedInteractionHits())
        {
            InteractableItemPickup itemPickup = null;
            if (hit.collider.CompareTag(interactiveTag))
                itemPickup = hit.collider.GetComponent<InteractableItemPickup>();
            else
                itemPickup = hit.collider.GetComponentInParent<InteractableItemPickup>();

            if (itemPickup != null)
                return itemPickup;
        }

        return null;
    }
    
    //private void HandleFootsteps()
    //{
        
        //if (!IsWalking)
        //{
            //footstepTimer = 0f;
            //return;
        //}
        

        //footstepTimer += Time.deltaTime;
        //Debug.Log("footstepTimer1: " + footstepTimer);
        //Debug.Log("footstepEvent4: " + footstepEvent);
        //if (footstepTimer >= footstepInterval && footstepEvent != null)
        //{
           // Debug.Log("made it past the footstepInterval");
            //footstepEvent.Post(gameObject);
            //footstepTimer = 0f;
            //Debug.Log("Footstep event posted");
        //}
    //}
}