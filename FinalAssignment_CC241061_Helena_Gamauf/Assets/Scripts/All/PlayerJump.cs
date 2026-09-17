using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJump : MonoBehaviour
{
    [SerializeField] private InputActionReference jump;
    [SerializeField] private float forceValue;
    [SerializeField] private GameObject PlayerGround;
    [SerializeField] private float groundRayOffset;
    [SerializeField] private float groundRayDistanceThreshold;

    private bool isJumping;
    private bool isGrounded;

    public bool IsGrounded => isGrounded;

    private Rigidbody rb;
    private CapsuleCollider collider;
    private BoxCollider boxCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();

        if (PlayerGround != null)
            boxCollider = PlayerGround.GetComponent<BoxCollider>();

        if (rb != null)
            rb.freezeRotation = true;
    }

    private void OnEnable()
    {
        jump.action.Enable();
        jump.action.performed += OnJump;
    }

    private void OnDisable()
    {
        jump.action.performed -= OnJump;
    }

    private void Update()
    {
        CheckGrounded();
    }

    private void OnJump(InputAction.CallbackContext callbackContext)
    {
        if (!isGrounded || isJumping)
            return;

        Jump();
    }

    private void Jump()
    {
        AkUnitySoundEngine.PostEvent("Play_swoosh_2_jump", gameObject);
        isGrounded = false;
        isJumping = true;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        rb.AddForce(Vector3.up * forceValue, ForceMode.Impulse);
    }

    private void CheckGrounded()
    {
        if (collider == null)
            return;

        float rayDistance = (collider.height / 2f) + groundRayDistanceThreshold;
        Vector3 rayOrigin = transform.position + Vector3.up * groundRayOffset;

        int layerMask = ~LayerMask.GetMask("Player");

        isGrounded = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            rayDistance,
            layerMask,
            QueryTriggerInteraction.Ignore
        );

        if (isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            isJumping = false;
        }

        if (PlayerGround != null)
        {
            PlayerGround.transform.position = new Vector3(
                transform.position.x,
                collider.bounds.min.y,
                transform.position.z
            );
        }

        Debug.DrawRay(
            rayOrigin,
            Vector3.down * rayDistance,
            isGrounded ? Color.green : Color.red
        );
    }
}