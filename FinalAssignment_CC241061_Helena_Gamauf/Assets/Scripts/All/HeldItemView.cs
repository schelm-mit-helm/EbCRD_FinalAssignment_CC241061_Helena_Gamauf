using UnityEngine;

public class HeldItemView : MonoBehaviour
{
    [SerializeField]
    Transform cameraTransform;

    [SerializeField]
    Vector3 holdLocalPosition = new(0.35f, -0.3f, 0.55f);

    [SerializeField]
    float holdScale = 1f;

    Transform holdAnchor;
    GameObject currentModel;

    void Awake()
    {
        ResolveCameraTransform();
        EnsureAnchor();
    }

    void ResolveCameraTransform()
    {
        if (cameraTransform != null)
        {
            return;
        }

        var playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null && playerMovement.CameraTransform != null)
        {
            cameraTransform = playerMovement.CameraTransform;
        }
    }

    void EnsureAnchor()
    {
        ResolveCameraTransform();

        if (cameraTransform == null)
        {
            return;
        }

        if (holdAnchor != null && holdAnchor.parent == cameraTransform)
        {
            holdAnchor.localPosition = holdLocalPosition;
            holdAnchor.localRotation = Quaternion.identity;
            return;
        }

        if (holdAnchor != null)
        {
            Destroy(holdAnchor.gameObject);
        }

        var anchorObject = new GameObject("HeldItemAnchor");
        holdAnchor = anchorObject.transform;
        holdAnchor.SetParent(cameraTransform, false);
        holdAnchor.localPosition = holdLocalPosition;
        holdAnchor.localRotation = Quaternion.identity;
    }

    public void SetItem(Item item)
    {
        Clear();

        if (item == null || item.prefab == null)
        {
            if (item != null && item.prefab == null)
            {
                Debug.LogError($"HeldItemView: Item '{item.name}' has no prefab assigned.");
            }

            return;
        }

        EnsureAnchor();

        if (holdAnchor == null)
        {
            Debug.LogError("HeldItemView: Could not create hold anchor. Assign cameraTransform.");
            return;
        }

        currentModel = Instantiate(item.prefab);
        var prefabRoot = item.prefab.transform;
        var prefabLocalPosition = prefabRoot.localPosition;
        var prefabLocalRotation = prefabRoot.localRotation;
        var extraScale = holdScale * item.GetDisplayScale();
        ItemVisualUtility.PrepareForDisplay(
            currentModel,
            ItemVisualUtility.HeldTargetMaxSize,
            extraScale,
            preservePrefabTransform: true);
        currentModel.transform.SetParent(holdAnchor, false);
        currentModel.transform.localPosition = prefabLocalPosition;
        currentModel.transform.localRotation = prefabLocalRotation;
    }

    public void Clear()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        currentModel = null;
    }
}
