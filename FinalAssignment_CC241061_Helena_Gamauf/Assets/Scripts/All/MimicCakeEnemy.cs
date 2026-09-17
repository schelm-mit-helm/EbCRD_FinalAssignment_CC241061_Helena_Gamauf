using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MimicCakeEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float chaseUpdateRate = 0.1f;
    [SerializeField] private float facePlayerSpeed = 15f;
    [SerializeField] private float navMeshSampleDistance = 3f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string awakenTriggerName = "Awaken";
    [SerializeField] private string chasingBoolName = "IsChasing";
    [SerializeField] private float awakenDelay = 1.2f;

    [Header("Visual Fix")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float visualYawOffset = 180f;
    [SerializeField] private bool keepVisualCentered = true;

    [Header("Kill")]
    [SerializeField] private string gameOverSceneName = "Game_Over_Scene";

    private Transform player;
    private NavMeshAgent navMeshAgent;
    private bool awakened;
    private bool chasing;
    private bool canKill;
    private Vector3 visualStartLocalPosition;
    private Quaternion visualStartLocalRotation;
    private bool canApplyVisualFix;

    public bool IsAwakened => awakened;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (visualRoot == null && animator != null && animator.transform != transform)
            visualRoot = animator.transform;

        if (visualRoot != null && visualRoot != transform)
        {
            visualStartLocalPosition = visualRoot.localPosition;
            visualStartLocalRotation = visualRoot.localRotation;
            canApplyVisualFix = true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
            player = playerObject.transform;

        if (navMeshAgent != null)
        {
            navMeshAgent.speed = chaseSpeed;
            navMeshAgent.isStopped = true;
            navMeshAgent.updatePosition = true;
            navMeshAgent.updateRotation = false;
        }

        if (animator != null)
            animator.applyRootMotion = false;

        awakened = false;
        chasing = false;
        canKill = false;
    }

    private void Update()
    {
        if (!chasing)
            return;

        if (player == null || navMeshAgent == null)
            return;

        if (!navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            return;

        navMeshAgent.SetDestination(player.position);
        FacePlayer();
    }

    private void LateUpdate()
    {
        if (!canApplyVisualFix)
            return;

        if (keepVisualCentered)
            visualRoot.localPosition = visualStartLocalPosition;

        visualRoot.localRotation = visualStartLocalRotation * Quaternion.Euler(0f, 0f, visualYawOffset);
    }

    public void Awaken()
    {
        if (awakened)
            return;

        awakened = true;

        if (animator != null && !string.IsNullOrEmpty(awakenTriggerName))
            animator.SetTrigger(awakenTriggerName);

        StartCoroutine(AwakenRoutine());
    }

    private IEnumerator AwakenRoutine()
    {
        yield return new WaitForSeconds(awakenDelay);

        if (navMeshAgent != null)
        {
            if (!navMeshAgent.enabled)
                navMeshAgent.enabled = true;

            if (!navMeshAgent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                    navMeshAgent.Warp(hit.position);
            }

            navMeshAgent.speed = chaseSpeed;
            navMeshAgent.isStopped = false;
        }

        chasing = true;
        canKill = true;

        if (animator != null && !string.IsNullOrEmpty(chasingBoolName))
            animator.SetBool(chasingBoolName, true);

        while (chasing)
        {
            if (player != null && navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(player.position);
                FacePlayer();
            }

            yield return new WaitForSeconds(chaseUpdateRate);
        }
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * facePlayerSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canKill)
            return;

        if (other.CompareTag(playerTag))
            SceneManager.LoadScene(gameOverSceneName);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!canKill)
            return;

        if (collision.collider.CompareTag(playerTag))
            SceneManager.LoadScene(gameOverSceneName);
    }
}