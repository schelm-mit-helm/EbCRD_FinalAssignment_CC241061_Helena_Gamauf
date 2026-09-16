using System;
using System.Collections;
using AK.Wwise;
using UnityEngine;
using Event = AK.Wwise.Event;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class CharacterAnimatorDriver : MonoBehaviour
{
    static readonly int StateHash = Animator.StringToHash(CharacterAnimatorParams.State);

    static readonly string[] StateNames =
    {
        "idle",
        "Walking",
        "Talking",
        "turn to the left",
        "turn to the right",
    };

    [SerializeField] Animator animator;
    [SerializeField] float fallbackStateDuration = 1f;
    [SerializeField] float stateEnterTimeout = 0.5f;
    
    // Wwise audio settings
    [SerializeField] Event walkingEvent;
    [SerializeField] Event talkingEvent;
    
    [SerializeField] Switch walkOutsideSwitch;
    [SerializeField] Switch walkInsideSwitch;
    [SerializeField] private Collider indoorTrigger; 
    private float footstepInterval = 0.48f;
    private float footstepTimer = 0f;

    int currentState = CharacterAnimatorParams.Idle;
    bool isIndoors;
    
    uint talkingPlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
    
    public int CurrentState => currentState;
    public Animator Animator => animator;

    void Awake()
    {
        ResolveAnimator();
        EnsureAkGameObj();
    }
    
    void OnDisable()
    {
        StopTalkingIfPlaying();
    }

    void OnDestroy()
    {
        StopTalkingIfPlaying();
    }
    void ResolveAnimator()
    {
        if (animator != null)
            return;

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }
    
    void EnsureAkGameObj()
    {
        if (GetComponent<AkGameObj>() == null)
            gameObject.AddComponent<AkGameObj>();
    }
    void Update()
    {
        
        if (currentState == CharacterAnimatorParams.Walking)
        {
            //UpdateIndoorState();
            footstepTimer += Time.deltaTime;
    
            if (footstepTimer >= footstepInterval)
            {
                walkingEvent.Post(gameObject);
                footstepTimer = 0f;
            }
        }
    }
    void HandleWalkingEvent()
    {
        if (walkingEvent == null)
            return;

        footstepTimer += Time.deltaTime;
    
        if (footstepTimer >= footstepInterval)
        {
            walkingEvent.Post(gameObject);
            footstepTimer = 0f;
        }
    }

    void UpdateIndoorState()
    {
        if (indoorTrigger == null )
            return;

        bool wasIndoors = isIndoors;
        isIndoors = indoorTrigger.bounds.Contains(transform.position);

        if (isIndoors != wasIndoors)
        {
            var sw = isIndoors ? walkInsideSwitch : walkOutsideSwitch;
            if (sw != null)
                sw.SetValue(gameObject);
        }
    }
    
    public void BindAnimator(Animator target)
    {
        if (target != null)
            animator = target;
        else
            ResolveAnimator();
    }

    public void SetIdle() => ApplyState(CharacterAnimatorParams.Idle);
    public void SetWalking() => ApplyState(CharacterAnimatorParams.Walking);
    public void SetTalking() => ApplyState(CharacterAnimatorParams.Talking);
    public void SetTurnLeft() => ApplyState(CharacterAnimatorParams.TurnLeft);
    public void SetTurnRight() => ApplyState(CharacterAnimatorParams.TurnRight);

    public void ApplyState(int state, bool force = false)
    {
        ResolveAnimator();
        if (animator == null)
            return;

        if (!force && currentState == state)
            return;

        int previousState = currentState;
        currentState = state;
        animator.SetInteger(StateHash, state);
        animator.Update(0f);
        
        HandleStateSoundTransition(previousState, currentState);
    }
    
    void HandleStateSoundTransition(int previousState, int newState)
    {
        // Entering Talking: Post the talk event once and remember its playing ID
        if (previousState != CharacterAnimatorParams.Talking && newState == CharacterAnimatorParams.Talking)
        {
            if (talkingEvent != null)
            {
                // AkWwise Event.Post returns a playing ID; store it so we can stop later
                talkingPlayingId = talkingEvent.Post(gameObject);
                Debug.Log($"Talking event playing ID: {talkingPlayingId}");
            }
        }

        // Exiting Talking: stop the playing ID if any
        if (previousState == CharacterAnimatorParams.Talking && newState != CharacterAnimatorParams.Talking)
        {
            StopTalkingIfPlaying();
        }
    }

    void StopTalkingIfPlaying()
    {
        if (talkingPlayingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.StopPlayingID(talkingPlayingId);
            talkingPlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        }
    }

    public int PickTurnStateToward(Vector3 worldPosition, bool invertDirection = false)
    {
        ResolveAnimator();

        var facingTransform = animator != null ? animator.transform : transform;
        var toTarget = worldPosition - facingTransform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f)
            return CharacterAnimatorParams.TurnRight;

        var signedAngle = Vector3.SignedAngle(
            facingTransform.forward,
            toTarget.normalized,
            Vector3.up);

        if (invertDirection)
            signedAngle = -signedAngle;

        return signedAngle >= 0f
            ? CharacterAnimatorParams.TurnRight
            : CharacterAnimatorParams.TurnLeft;
    }

    public IEnumerator WaitForStateFinish(int expectedState)
    {
        ResolveAnimator();
        if (animator == null)
        {
            yield return new WaitForSeconds(fallbackStateDuration);
            yield break;
        }

        if (expectedState < 0 || expectedState >= StateNames.Length)
        {
            yield return new WaitForSeconds(fallbackStateDuration);
            yield break;
        }

        const int layer = 0;
        var stateName = StateNames[expectedState];
        var elapsed = 0f;

        while (elapsed < stateEnterTimeout)
        {
            if (animator.IsInTransition(layer) || IsInState(stateName, layer))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        while (animator.IsInTransition(layer))
            yield return null;

        if (!IsOneShotState(expectedState))
            yield break;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        while (stateInfo.IsName(stateName) && stateInfo.normalizedTime < 1f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        }
    }

    public IEnumerator WaitForCurrentCycleEnd(int expectedState)
    {
        ResolveAnimator();
        if (animator == null)
        {
            yield return new WaitForSeconds(fallbackStateDuration);
            yield break;
        }

        if (expectedState < 0 || expectedState >= StateNames.Length)
        {
            yield return new WaitForSeconds(fallbackStateDuration);
            yield break;
        }

        const int layer = 0;
        var stateName = StateNames[expectedState];
        var elapsed = 0f;

        while (elapsed < stateEnterTimeout)
        {
            if (animator.IsInTransition(layer) || IsInState(stateName, layer))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        while (animator.IsInTransition(layer))
            yield return null;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        if (!stateInfo.IsName(stateName))
            yield break;

        var cycleEnd = Mathf.Floor(stateInfo.normalizedTime) + 1f;
        while (stateInfo.IsName(stateName) && stateInfo.normalizedTime < cycleEnd)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        }
    }

    public IEnumerator WaitForCurrentStateFinish()
    {
        yield return WaitForStateFinish(currentState);
    }

    public IEnumerator WaitUntilStateActive(int expectedState)
    {
        ResolveAnimator();
        if (animator == null)
            yield break;

        if (expectedState < 0 || expectedState >= StateNames.Length)
            yield break;

        const int layer = 0;
        var stateName = StateNames[expectedState];
        var elapsed = 0f;

        while (elapsed < stateEnterTimeout)
        {
            if (!animator.IsInTransition(layer) && IsInState(stateName, layer))
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        while (animator.IsInTransition(layer))
            yield return null;
    }

    static bool IsOneShotState(int state) =>
        state is CharacterAnimatorParams.TurnLeft
            or CharacterAnimatorParams.TurnRight
            or CharacterAnimatorParams.Talking;

    bool IsInState(string stateName, int layer)
    {
        var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        return stateInfo.IsName(stateName);
    }
}
