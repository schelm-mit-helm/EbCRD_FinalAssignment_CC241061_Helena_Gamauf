using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DroppedItem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    bool autoStart;

    [SerializeField]
    float enabledPickupDelay = 1f;

    [SerializeField]
    float fallSpeed = 12f;

    [SerializeField]
    float groundOffset = 0.05f;

    [SerializeField]
    float groundCheckDistance = 3f;

    [Header("State")]
    public Item item;
    public bool pickedUp;

    Collider pickupCollider;
    bool pickupEnabled;
    bool isGrounded;
    bool initialized;
    int groundMask;

    void Awake()
    {
        pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
        groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast", "UI");
        RemoveRotators();
    }

    void Start()
    {
        if (autoStart && item != null && !initialized)
            Initialize(item);
    }

    public void Initialize(Item item, float pickupDelay = -1f, bool settleImmediately = false)
    {
        if (initialized)
            return;

        if (item == null || item.prefab == null)
        {
            Debug.LogError(item == null
                ? "DroppedItem: Cannot initialize with a null item."
                : $"DroppedItem: Item '{item.name}' has no prefab assigned.");
            return;
        }

        initialized = true;
        this.item = item;
        pickedUp = false;
        pickupEnabled = false;
        isGrounded = false;

        RemoveRotators();

        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;

            if (pickupCollider is CapsuleCollider capsule)
                capsule.center = new Vector3(0f, 0.5f, 0f);
        }

        var prefabRoot = item.prefab.transform;
        var visual = Instantiate(item.prefab);
        var prefabLocalPosition = prefabRoot.localPosition;
        var prefabLocalRotation = prefabRoot.localRotation;
        var extraScale = item.GetDisplayScale();
        ItemVisualUtility.PrepareForDisplay(
            visual,
            ItemVisualUtility.DroppedTargetMaxSize,
            extraScale,
            preservePrefabTransform: true);
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = prefabLocalPosition;
        visual.transform.localRotation = prefabLocalRotation;

        if (settleImmediately)
            SettleAtCurrentPosition();
        else
            ItemVisualUtility.AlignBottomToWorldY(transform, transform.position.y + groundOffset);

        StartCoroutine(EnablePickup(pickupDelay >= 0f ? pickupDelay : enabledPickupDelay));
    }

    void SettleAtCurrentPosition()
    {
        isGrounded = true;
        ItemVisualUtility.AlignBottomToWorldY(transform, transform.position.y + groundOffset);
    }

    void RemoveRotators()
    {
        foreach (var rotator in GetComponents<Rotator>())
            Destroy(rotator);
    }

    void FixedUpdate()
    {
        if (isGrounded)
            return;

        var origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            var targetY = hit.point.y + groundOffset;
            if (transform.position.y <= targetY)
            {
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                isGrounded = true;
                ItemVisualUtility.AlignBottomToWorldY(transform, transform.position.y + groundOffset);
                return;
            }
        }

        transform.position += Vector3.down * fallSpeed * Time.fixedDeltaTime;
    }

    IEnumerator EnablePickup(float delay)
    {
        yield return new WaitForSeconds(delay);
        pickupEnabled = true;
        if (pickupCollider != null)
            pickupCollider.enabled = true;
    }

    public bool CanBePickedUp => pickupEnabled && !pickedUp && item != null;
}
