using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterAnimatorDriver))]
public class CustomerPathMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float arrivalThreshold = 0.3f;
    [SerializeField] float rotationSpeed = 8f;

    Transform[] checkpoints;
    Transform cashier;
    NavMeshAgent agent;
    bool useNavMesh;
    bool counterArrivalHandled;
    bool counterArrivalSequenceRunning;
    bool isExiting;
    int pathNumber = 1;
    CharacterAnimatorDriver animationDriver;
    Transform exitSpawnPoint1;
    Transform exitSpawnPoint2;
    Action onReachedSpawnPoint;

    public int PathNumber => pathNumber;
    public bool IsExiting => isExiting;

    void Awake()
    {
        animationDriver = GetComponent<CharacterAnimatorDriver>();
        if (animationDriver == null)
            animationDriver = gameObject.AddComponent<CharacterAnimatorDriver>();

        animationDriver.BindAnimator(GetComponent<Animator>());
    }

    public void Initialize(Transform[] pathCheckpoints, Transform cashierTarget, int customerPathNumber = 1)
    {
        checkpoints = pathCheckpoints;
        cashier = cashierTarget;
        pathNumber = customerPathNumber;

        if (animationDriver == null)
            animationDriver = GetComponent<CharacterAnimatorDriver>();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = arrivalThreshold;
            agent.updateRotation = false;
            agent.Warp(transform.position);
            useNavMesh = agent.isOnNavMesh;
        }

        StartCoroutine(FollowPath());
    }

    IEnumerator FollowPath()
    {
        if (checkpoints == null || checkpoints.Length == 0)
            yield break;

        yield return null;

        for (var i = 0; i < checkpoints.Length; i++)
        {
            var checkpoint = checkpoints[i];
            if (checkpoint == null)
                continue;

            yield return MoveToPosition(checkpoint.position);

            if (isExiting)
                yield break;

            if (IsCounterCheckpoint(checkpoint))
            {
                yield return HandleCounterArrival();
                yield break;
            }

            if (IsPostDoorCheckpoint(checkpoint))
                CustomerDoorEntryZone.CloseMainDoors();
        }

        if (useNavMesh && agent != null)
        {
            agent.isStopped = true;
            agent.autoBraking = true;
        }

        animationDriver.ApplyState(CharacterAnimatorParams.Idle, force: true);
    }

    public void StartLoopingTalk()
    {
        animationDriver?.SetTalking();
    }

    public void PrepareForReturnTalk()
    {
        StopAllCoroutines();
        isExiting = false;

        if (useNavMesh && agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        animationDriver?.SetIdle();
    }

    public IEnumerator PlayReturnTalkSequence(Transform lookTarget)
    {
        PrepareForReturnTalk();

        if (lookTarget != null)
            yield return LookAtTarget(lookTarget.position);

        animationDriver.SetTalking();
        yield return animationDriver.WaitForStateFinish(CharacterAnimatorParams.Talking);
        animationDriver.SetIdle();
    }

    public void StartExitPath(
        Transform[] exitCheckpoints,
        Transform spawnPoint1 = null,
        Transform spawnPoint2 = null,
        Action reachedSpawnPoint = null)
    {
        if (exitCheckpoints == null || exitCheckpoints.Length == 0)
        {
            reachedSpawnPoint?.Invoke();
            Destroy(gameObject);
            return;
        }

        exitSpawnPoint1 = spawnPoint1;
        exitSpawnPoint2 = spawnPoint2;
        onReachedSpawnPoint = reachedSpawnPoint;

        StopAllCoroutines();
        isExiting = true;
        counterArrivalHandled = true;

        if (useNavMesh && agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        StartCoroutine(FollowExitPath(exitCheckpoints));
    }

    IEnumerator FollowExitPath(Transform[] exitCheckpoints)
    {
        yield return null;

        for (var i = 0; i < exitCheckpoints.Length; i++)
        {
            var checkpoint = exitCheckpoints[i];
            if (checkpoint == null)
                continue;

            yield return MoveToPosition(checkpoint.position);

            if (IsDoorCheckpoint(checkpoint))
                CustomerDoorEntryZone.CloseMainDoors();

            if (IsSpawnCheckpoint(checkpoint))
            {
                DespawnAtSpawnPoint();
                yield break;
            }
        }

        if (useNavMesh && agent != null)
        {
            agent.isStopped = true;
            agent.autoBraking = true;
        }

        DespawnAtSpawnPoint();
    }

    void DespawnAtSpawnPoint()
    {
        var callback = onReachedSpawnPoint;
        onReachedSpawnPoint = null;

        gameObject.SetActive(false);

        if (useNavMesh && agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        callback?.Invoke();
        Destroy(gameObject);
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        if (useNavMesh && agent != null)
            yield return MoveToPositionWithNavMesh(target);
        else
            yield return MoveToPositionDirectly(target);
    }

    IEnumerator MoveToPositionWithNavMesh(Vector3 target)
    {
        animationDriver.ApplyState(CharacterAnimatorParams.Idle, force: true);

        agent.isStopped = false;
        agent.autoBraking = false;
        agent.SetDestination(target);

        while (agent.pathPending)
            yield return null;

        animationDriver.ApplyState(CharacterAnimatorParams.Walking, force: true);

        while (agent.remainingDistance > arrivalThreshold)
        {
            var moveDirection = agent.velocity;
            if (moveDirection.sqrMagnitude < 0.01f)
                moveDirection = agent.desiredVelocity;

            FaceMovementDirection(moveDirection);
            yield return null;
        }
    }

    IEnumerator MoveToPositionDirectly(Vector3 target)
    {
        animationDriver.ApplyState(CharacterAnimatorParams.Walking, force: true);

        while (Vector3.Distance(transform.position, target) > arrivalThreshold)
        {
            var direction = target - transform.position;
            direction.y = 0f;

            FaceMovementDirection(direction);
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    void FaceMovementDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        var targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    IEnumerator LookAtTarget(Vector3 target)
    {
        var direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            yield break;

        var targetRotation = Quaternion.LookRotation(direction.normalized);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    IEnumerator HandleCounterArrival()
    {
        if (counterArrivalHandled)
            yield break;

        counterArrivalHandled = true;

        if (useNavMesh && agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        animationDriver.ApplyState(CharacterAnimatorParams.Idle, force: true);
        CustomerOrderService.Instance?.PlaceOrder(transform);
        yield return CounterArrivalSequence();
    }

    IEnumerator CounterArrivalSequence()
    {
        if (counterArrivalSequenceRunning)
            yield break;

        counterArrivalSequenceRunning = true;

        yield return animationDriver.WaitUntilStateActive(CharacterAnimatorParams.Idle);

        if (cashier != null)
            yield return LookAtTarget(cashier.position);

        yield return animationDriver.WaitForCurrentCycleEnd(CharacterAnimatorParams.Idle);

        animationDriver.ApplyState(CharacterAnimatorParams.Talking, force: true);
        yield return animationDriver.WaitForStateFinish(CharacterAnimatorParams.Talking);

        animationDriver.ApplyState(CharacterAnimatorParams.Idle, force: true);
        yield return animationDriver.WaitUntilStateActive(CharacterAnimatorParams.Idle);

        counterArrivalSequenceRunning = false;
    }

    static bool IsCounterCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null)
            return false;

        var checkpointName = checkpoint.name.Trim();
        return checkpointName == "Checkpoint 1.4" || checkpointName == "Checkpoint 2.4";
    }

    static bool IsPostDoorCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null)
            return false;

        var checkpointName = checkpoint.name.Trim();
        return checkpointName == "Checkpoint 1.3" || checkpointName == "Checkpoint 2.3";
    }

    static bool IsDoorCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null)
            return false;

        var checkpointName = checkpoint.name.Trim();
        return checkpointName == "Checkpoint 1.2" || checkpointName == "Checkpoint 2.2";
    }

    bool IsSpawnCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null)
            return false;

        if (checkpoint == exitSpawnPoint1 || checkpoint == exitSpawnPoint2)
            return true;

        var checkpointName = checkpoint.name.Trim();
        return string.Equals(checkpointName, "Spawnpoint 1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(checkpointName, "spawnpoint 2", StringComparison.OrdinalIgnoreCase);
    }
}
