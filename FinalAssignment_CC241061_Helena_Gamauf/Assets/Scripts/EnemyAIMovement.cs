using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAIMovement : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";

    [Header("Movement")]
    [SerializeField] private float playerNormalWalkSpeed = 4f;
    [SerializeField] private float enemySpeedBonus = 0.5f;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float sprintDetectionRange = 14f;
    [SerializeField] private float loseRange = 16f;
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderPauseMin = 1f;
    [SerializeField] private float wanderPauseMax = 3f;

    [Header("Player Movement Detection")]
    [SerializeField] private float sprintSpeedThreshold = 6f;
    [SerializeField] private float movementDetectionMultiplier = 1.2f;
    [SerializeField] private float detectionSmoothSpeed = 8f;

    [Header("Animation")]
    [SerializeField] private float movingSpeedThreshold = 0.1f;

    private Transform player;
    private NavMeshAgent navMeshAgent;
    private CharacterAnimatorDriver animationDriver;
    private bool isChasing;
    private Vector3 lastPlayerPosition;
    private float playerCurrentSpeed;
    private float currentDetectionRange;
    private bool canMove = true;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animationDriver = GetComponent<CharacterAnimatorDriver>();
        FindPlayer();
    }

    private void Start()
    {
        currentDetectionRange = detectionRange;

        if (player != null)
            lastPlayerPosition = player.position;

        if (navMeshAgent != null)
            navMeshAgent.speed = playerNormalWalkSpeed + enemySpeedBonus;

        if (animationDriver != null)
            animationDriver.ApplyState(CharacterAnimatorParams.Idle, force: true);

        StartCoroutine(AIRoutine());
    }

    private void Update()
    {
        UpdatePlayerSpeed();
        UpdateDetectionRange();
        CheckPlayerDistance();

        if (navMeshAgent != null)
            navMeshAgent.speed = playerNormalWalkSpeed + enemySpeedBonus;

        UpdateAnimation();
    }

    public void StartMoving()
    {
        canMove = true;

        if (navMeshAgent != null)
            navMeshAgent.isStopped = false;
    }

    public void StopMoving()
    {
        canMove = false;

        if (navMeshAgent != null)
            navMeshAgent.isStopped = true;

        if (animationDriver != null)
            animationDriver.SetIdle();
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
            player = playerObject.transform;
        else
            Debug.LogWarning("Enemy could not find the Player. Make sure the Player object is tagged 'Player'.");
    }

    private void UpdatePlayerSpeed()
    {
        if (player == null)
            return;

        if (Time.deltaTime <= 0f)
            return;

        Vector3 playerMovement = player.position - lastPlayerPosition;
        playerMovement.y = 0f;

        playerCurrentSpeed = playerMovement.magnitude / Time.deltaTime;
        lastPlayerPosition = player.position;
    }

    private void UpdateDetectionRange()
    {
        float targetRange = detectionRange;

        if (playerCurrentSpeed >= sprintSpeedThreshold)
            targetRange = sprintDetectionRange;
        else if (playerCurrentSpeed > 0.1f)
            targetRange = Mathf.Clamp(detectionRange + playerCurrentSpeed * movementDetectionMultiplier, detectionRange, sprintDetectionRange);

        currentDetectionRange = Mathf.Lerp(currentDetectionRange, targetRange, Time.deltaTime * detectionSmoothSpeed);
    }

    private void CheckPlayerDistance()
    {
        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= currentDetectionRange)
            isChasing = true;
        else if (distanceToPlayer >= loseRange)
            isChasing = false;
    }

    private IEnumerator AIRoutine()
    {
        while (true)
        {
            if (navMeshAgent == null || !canMove)
            {
                yield return null;
                continue;
            }

            if (isChasing)
            {
                ChasePlayer();
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                WanderRandomly();
                float pauseTime = Random.Range(wanderPauseMin, wanderPauseMax);
                yield return new WaitForSeconds(pauseTime);
            }
        }
    }

    private void ChasePlayer()
    {
        if (player == null || !canMove)
            return;

        navMeshAgent.SetDestination(player.position);
    }

    private void WanderRandomly()
    {
        if (!canMove)
            return;

        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            navMeshAgent.SetDestination(hit.position);
    }

    private void UpdateAnimation()
    {
        if (animationDriver == null || navMeshAgent == null)
            return;

        if (!canMove || navMeshAgent.isStopped)
        {
            animationDriver.SetIdle();
            return;
        }

        if (navMeshAgent.velocity.sqrMagnitude > movingSpeedThreshold * movingSpeedThreshold)
            animationDriver.SetWalking();
        else
            animationDriver.SetIdle();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, currentDetectionRange > 0f ? currentDetectionRange : detectionRange);
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}