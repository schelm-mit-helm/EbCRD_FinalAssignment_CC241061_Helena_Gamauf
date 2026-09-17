using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(CharacterAnimatorDriver))]
public class CashierInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string promptMessage = "Press E to talk";
    [SerializeField] string gamepadPromptMessage = "Press X to talk";
    [SerializeField] bool invertTurnDirection;
    [SerializeField] float faceAngleThreshold = 2f;
    [SerializeField] float facingYawOffset = 2f;
    [SerializeField] float customerFacingYawOffset;
    [SerializeField] float playerTalkDuration = 1.3f;
    [SerializeField] Vector3 turnLeftLocalOffset = new(0f, 0f, 0.02f);
    [SerializeField] Vector3 turnRightLocalOffset = new(0f, 0f, -0.02f);

    [Header("Dialogue")]
    [SerializeField] string[] taskRequestLines =
    {
        "Can you get these for me from the pantry..."
    };
    [SerializeField] string[] returnGreetingLines =
    {
        "Ah, you finally back."
    };
    [SerializeField] string[] idleNudgeLines =
    {
        "Pss, can you help me out a little."
    };

    CharacterAnimatorDriver animationDriver;
    Transform modelTransform;
    bool isInteractable;
    Coroutine behaviorCoroutine;
    Quaternion homeModelLocalRotation;
    Quaternion modelRestLocalRotation;
    Vector3 modelRestLocalPosition;
    Vector3 modelForwardInParentSpace = Vector3.forward;
    bool hasModelPose;

    public static CashierInteractable Instance { get; private set; }

    public bool CanInteract => isInteractable;

    bool IInteractable.CanInteract(PlayerInteractionContext context) => CanInteract;

    public string GetPromptMessage(bool useGamepad) =>
        useGamepad ? gamepadPromptMessage : promptMessage;

    void IInteractable.Interact(PlayerInteractionContext context) =>
        TryInteract(context.Player);

    void Awake()
    {
        Instance = this;

        animationDriver = GetComponent<CharacterAnimatorDriver>();
        if (animationDriver == null)
            animationDriver = gameObject.AddComponent<CharacterAnimatorDriver>();

        var childAnimator = GetComponentInChildren<Animator>();
        if (childAnimator != null)
        {
            animationDriver.BindAnimator(childAnimator);
            modelTransform = childAnimator.transform;
            CacheModelRestPose(modelTransform);
        }

        homeModelLocalRotation = hasModelPose
            ? modelTransform.localRotation
            : Quaternion.identity;

        animationDriver.ApplyState(CharacterAnimatorParams.Idle, force: true);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (behaviorCoroutine == null)
            StartCoroutine(EnsureLoopingIdleAtStart());
    }

    IEnumerator EnsureLoopingIdleAtStart()
    {
        animationDriver.ApplyState(CharacterAnimatorParams.Idle, force: true);
        yield return animationDriver.WaitUntilStateActive(CharacterAnimatorParams.Idle);
    }

    void CacheModelRestPose(Transform bodyTransform)
    {
        modelTransform = bodyTransform;
        modelRestLocalRotation = bodyTransform.localRotation;
        modelRestLocalPosition = bodyTransform.localPosition;
        modelForwardInParentSpace = modelRestLocalRotation * Vector3.forward;
        hasModelPose = true;
    }

    void ResetModelLocalPosition()
    {
        if (!hasModelPose || modelTransform == null)
            return;

        modelTransform.localPosition = modelRestLocalPosition;
    }

    public void SetInteractable(bool value)
    {
        isInteractable = value;

        if (!value)
            StopEngagement();
    }

    public void OnCustomerAtCounter()
    {
        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);

        behaviorCoroutine = StartCoroutine(CustomerArrivalSequence());
    }

    public void TryInteract(Transform player)
    {
        if (!isInteractable || player == null)
            return;

        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);

        behaviorCoroutine = StartCoroutine(InteractSequence(player));
    }

    void StopEngagement()
    {
        if (behaviorCoroutine != null)
        {
            StopCoroutine(behaviorCoroutine);
            behaviorCoroutine = null;
        }

        if (hasModelPose)
            modelTransform.localRotation = homeModelLocalRotation;

        ResetModelLocalPosition();
        animationDriver?.SetIdle();
    }

    IEnumerator CustomerArrivalSequence()
    {
        isInteractable = false;

        ResetModelLocalPosition();
        if (hasModelPose)
            homeModelLocalRotation = modelTransform.localRotation;

        animationDriver.ApplyState(CharacterAnimatorParams.Idle, force: true);
        yield return animationDriver.WaitUntilStateActive(CharacterAnimatorParams.Idle);
        yield return animationDriver.WaitForCurrentCycleEnd(CharacterAnimatorParams.Idle);

        var customer = CustomerOrderService.Instance?.ActiveCustomer;
        if (customer != null)
        {
            yield return TurnToRotation(
                FacingLocalRotation(customer.position, customerFacingYawOffset),
                applyPositionOffsetAfter: true);
        }

        animationDriver.ApplyState(CharacterAnimatorParams.Talking, force: true);
        yield return animationDriver.WaitForStateFinish(CharacterAnimatorParams.Talking);

        animationDriver.ApplyState(CharacterAnimatorParams.Idle, force: true);
        yield return animationDriver.WaitUntilStateActive(CharacterAnimatorParams.Idle);

        isInteractable = true;
        BakeryCashierDialogue.EnsureInstance();
        BakeryCashierDialogue.Instance?.OnCashierWaitingForPlayer();
        behaviorCoroutine = null;
    }

    IEnumerator InteractSequence(Transform player)
    {
        isInteractable = false;

        yield return TurnBodyToward(player.position);

        BakeryCashierDialogue.EnsureInstance();
        BakeryCashierDialogue.OnPlayerTalkedToCashier();
        ShowTaskRequestDialogue();

        animationDriver.SetTalking();
        yield return new WaitForSeconds(playerTalkDuration);
        yield return animationDriver.WaitForCurrentCycleEnd(CharacterAnimatorParams.Talking);

        CustomerOrderService.Instance?.NotifyPlayerReceivedTask();
        CustomerOrderUI.Instance?.ShowOrder(CustomerOrderService.Instance?.CurrentOrder);

        var customer = CustomerOrderService.Instance?.ActiveCustomer;
        if (customer != null)
        {
            yield return TurnToRotation(
                homeModelLocalRotation,
                endState: CharacterAnimatorParams.Talking);
        }

        animationDriver.SetTalking();
        CustomerOrderService.Instance?.StartCustomerLoopingTalk();
        isInteractable = true;
        behaviorCoroutine = null;
    }

    IEnumerator TurnBodyToward(Vector3 targetPosition)
    {
        var targetRotation = FacingLocalRotation(targetPosition, facingYawOffset);
        yield return TurnToRotation(
            targetRotation,
            applyPositionOffsetAfter: true);
    }

    IEnumerator TurnToRotation(
        Quaternion targetLocalRotation,
        int endState = CharacterAnimatorParams.Idle,
        bool applyPositionOffsetAfter = false)
    {
        if (!hasModelPose)
            yield break;

        ResetModelLocalPosition();

        if (Quaternion.Angle(modelTransform.localRotation, targetLocalRotation) <= faceAngleThreshold)
            yield break;

        var turnState = PickTurnState(targetLocalRotation);
        var turnOffset = GetTurnLocalOffset(turnState);

        animationDriver.ApplyState(turnState, force: true);
        yield return animationDriver.WaitForStateFinish(turnState);

        modelTransform.localRotation = targetLocalRotation;
        modelTransform.localPosition = applyPositionOffsetAfter
            ? modelRestLocalPosition + turnOffset
            : modelRestLocalPosition;
        animationDriver.ApplyState(endState, force: true);
    }

    Vector3 GetTurnLocalOffset(int turnState)
    {
        if (turnState == CharacterAnimatorParams.TurnLeft)
            return turnLeftLocalOffset;

        if (turnState == CharacterAnimatorParams.TurnRight)
            return turnRightLocalOffset;

        return Vector3.zero;
    }

    Quaternion FacingLocalRotation(Vector3 targetPosition, float yawOffset)
    {
        var parentTarget = FacingParentRotation(targetPosition, yawOffset);
        return Quaternion.Inverse(transform.rotation) * parentTarget * modelRestLocalRotation;
    }

    Quaternion FacingParentRotation(Vector3 targetPosition, float yawOffset)
    {
        var direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return transform.rotation;

        var modelForward = hasModelPose ? modelForwardInParentSpace : Vector3.forward;

        var facing = Quaternion.LookRotation(direction.normalized, Vector3.up)
            * Quaternion.Inverse(Quaternion.LookRotation(modelForward, Vector3.up));

        if (Mathf.Abs(yawOffset) > 0.01f)
            facing *= Quaternion.Euler(0f, yawOffset, 0f);

        return facing;
    }

    Vector3 VisualForward
    {
        get
        {
            if (!hasModelPose)
                return transform.forward;

            return transform.rotation * modelTransform.localRotation * Vector3.forward;
        }
    }

    int PickTurnState(Quaternion targetLocalRotation)
    {
        var targetForward = transform.rotation * targetLocalRotation * Vector3.forward;
        targetForward.y = 0f;

        if (targetForward.sqrMagnitude < 0.001f)
            return CharacterAnimatorParams.TurnRight;

        var signedAngle = Vector3.SignedAngle(
            VisualForward,
            targetForward.normalized,
            Vector3.up);

        if (invertTurnDirection)
            signedAngle = -signedAngle;

        return signedAngle >= 0f
            ? CharacterAnimatorParams.TurnRight
            : CharacterAnimatorParams.TurnLeft;
    }

    public string GetRandomTaskRequestLine() => PickRandomLine(taskRequestLines);

    public string GetRandomReturnGreetingLine() => PickRandomLine(returnGreetingLines);

    public string GetRandomIdleNudgeLine() => PickRandomLine(idleNudgeLines);

    public void ShowTaskRequestDialogue() =>
        ShowDialogueLine(GetRandomTaskRequestLine(), "Can you get these for me from the pantry...");

    public void ShowReturnGreetingDialogue() =>
        ShowDialogueLine(GetRandomReturnGreetingLine(), "Ah, you finally back.");

    public void ShowIdleNudgeDialogue() =>
        ShowDialogueLine(GetRandomIdleNudgeLine(), "Pss, can you help me out a little.");

    public static CashierInteractable FindInScene()
    {
        if (Instance != null)
            return Instance;

        var matches = FindObjectsByType<CashierInteractable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        return matches.Length > 0 ? matches[0] : null;
    }

    static void ShowDialogueLine(string line, string fallback)
    {
        if (string.IsNullOrWhiteSpace(line))
            line = fallback;

        if (string.IsNullOrWhiteSpace(line))
            return;

        ComicSpeechBoxUI.EnsureInstance();
        ComicSpeechBoxUI.Show(line);
    }

    static string PickRandomLine(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return string.Empty;

        var validCount = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
                validCount++;
        }

        if (validCount == 0)
            return string.Empty;

        var pick = Random.Range(0, validCount);
        for (var i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            if (pick == 0)
                return lines[i];

            pick--;
        }

        return string.Empty;
    }
}
