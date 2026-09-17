using System;
using UnityEngine;
using AK.Wwise;

[RequireComponent(typeof(Collider))]
public class Inventory : MonoBehaviour
{
    public const int MaxSlots = 5;

    [Header("References")]
    [SerializeField]
    InventoryUI ui;

    [SerializeField]
    HeldItemView heldItemView;
    

    [Header("Prefabs")]
    [SerializeField]
    GameObject droppedItemPrefab;

        

    [Header("Wwise Events")]
    [SerializeField]
    AK.Wwise.Event pickUpItemEvent;

    [SerializeField]
    AK.Wwise.Event dropItemEvent;

    [Header("Drop Settings")]
    [SerializeField]
    float dropForwardDistance = 0.6f;

    [SerializeField]
    float dropPickupDelay = 0.75f;

    [SerializeField]
    float dropGroundOffset = 0.05f;

    [SerializeField]
    float dropGroundRayHeight = 2f;

    [SerializeField]
    float dropGroundRayDistance = 10f;

    int groundMask;
    bool uiInitialized;

    readonly Item[] slots = new Item[MaxSlots];
    readonly string[] slotIds = new string[MaxSlots];
    int selectedSlot = -1;

    public int SelectedSlot => selectedSlot;

    public int ItemCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < MaxSlots; i++)
            {
                if (slots[i] != null)
                {
                    count++;
                }
            }

            return count;
        }
    }

    void Awake()
    {
        groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast", "UI");

        if (ui == null)
        {
            ui = FindFirstObjectByType<InventoryUI>();
        }

        TryInitializeUi();

        // Ensure this GameObject has an AkGameObj (for Wwise postings)
        if (GetComponent<AkGameObj>() == null)
            gameObject.AddComponent<AkGameObj>();
    }

    void Start()
    {
        TryInitializeUi();
    }

    void TryInitializeUi()
    {
        if (uiInitialized || ui == null)
        {
            return;
        }

        ui.ResolveReferences();
        ui.InitializeSlots(this);
        uiInitialized = ui.IsInitialized;
    }

    bool EnsureUiReady()
    {
        TryInitializeUi();
        return uiInitialized;
    }

    public void PickupDroppedItem(DroppedItem droppedItem)
    {
        if (droppedItem == null || !droppedItem.CanBePickedUp)
        {
            return;
        }

        if (!TryPickupItem(droppedItem.item))
        {
            return;
        }

        droppedItem.pickedUp = true;
        Destroy(droppedItem.gameObject);
    }

    public bool TryPickupItem(Item item)
    {
        if (!TryAddItem(item))
        {
            return false;
        }

        if (pickUpItemEvent != null && AkUnitySoundEngine.IsInitialized())
        {
            pickUpItemEvent.Post(gameObject);
        }

        return true;
    }

    bool TryAddItem(Item item)
    {
        if (!EnsureUiReady())
        {
            Debug.LogError("Inventory: UI is not initialized. Assign InventoryUI references in the scene.");
            return false;
        }

        var slot = FindFirstEmptySlot();
        if (slot < 0)
        {
            return false;
        }

        slotIds[slot] = Guid.NewGuid().ToString();
        slots[slot] = item;
        ui.SetSlot(slot, item);
        SelectSlot(slot);

        return true;
    }

    public void TrySelectSlot(int slot)
    {
        if (slot < 0 || slot >= MaxSlots || slots[slot] == null)
        {
            return;
        }

        SelectSlot(slot);
    }

    public void SelectSlot(int slot)
    {
        if (slot < 0 || slot >= MaxSlots || slots[slot] == null)
        {
            return;
        }

        selectedSlot = slot;
        ui.SetSelectedSlot(slot);
        heldItemView?.SetItem(slots[slot]);
    }

    public void SelectAdjacentFilledSlot(int direction)
    {
        if (ItemCount == 0)
        {
            return;
        }

        var slot = selectedSlot >= 0 ? selectedSlot : FindLeftmostFilledSlot();
        var start = slot;

        do
        {
            slot = (slot + direction + MaxSlots) % MaxSlots;
            if (slots[slot] != null)
            {
                SelectSlot(slot);
                return;
            }
        }
        while (slot != start);
    }

    public void DropSelectedItem()
    {
        if (selectedSlot < 0 || slots[selectedSlot] == null)
        {
            return;
        }

        DropSlot(selectedSlot);
    }

    public Item GetSelectedItem() =>
        selectedSlot >= 0 && selectedSlot < MaxSlots ? slots[selectedSlot] : null;

    public bool TryConsumeSelectedItem()
    {
        if (selectedSlot < 0 || slots[selectedSlot] == null)
        {
            return false;
        }

        var slot = selectedSlot;
        slots[slot] = null;
        slotIds[slot] = null;
        ui.ClearSlot(slot);

        if (ItemCount == 0)
        {
            selectedSlot = -1;
            ui.SetSelectedSlot(-1);
            heldItemView?.Clear();
        }
        else
        {
            SelectSlot(FindLeftmostFilledSlot());
        }

        if (dropItemEvent != null && AkUnitySoundEngine.IsInitialized())
        {
            dropItemEvent.Post(gameObject);
        }

        return true;
    }

    void DropSlot(int slot)
    {
        var item = slots[slot];
        if (item == null)
        {
            return;
        }

        if (droppedItemPrefab == null)
        {
            Debug.LogError("Inventory: Assign the Dropped Item Prefab from Project/Prefabs, not a scene object.");
            return;
        }

        var dropPosition = GetDropPosition();
        var droppedObject = Instantiate(droppedItemPrefab, dropPosition, Quaternion.identity);
        var droppedItem = droppedObject.GetComponent<DroppedItem>();
        droppedItem.Initialize(item, dropPickupDelay, settleImmediately: true);

        slots[slot] = null;
        slotIds[slot] = null;
        ui.ClearSlot(slot);

        if (ItemCount == 0)
        {
            selectedSlot = -1;
            ui.SetSelectedSlot(-1);
            heldItemView?.Clear();
        }
        else
        {
            SelectSlot(FindLeftmostFilledSlot());
        }

        if (dropItemEvent != null && AkUnitySoundEngine.IsInitialized())
            dropItemEvent.Post(gameObject);
    }

    int FindFirstEmptySlot()
    {
        for (var i = 0; i < MaxSlots; i++)
        {
            if (slots[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    int FindLeftmostFilledSlot()
    {
        for (var i = 0; i < MaxSlots; i++)
        {
            if (slots[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    Vector3 GetDropPosition()
    {
        var playerCollider = GetComponent<Collider>();
        var footY = playerCollider != null ? playerCollider.bounds.min.y : transform.position.y;

        var forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        var dropPoint = new Vector3(transform.position.x, footY, transform.position.z) + forward * dropForwardDistance;
        var rayOrigin = dropPoint + Vector3.up * dropGroundRayHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, dropGroundRayHeight + dropGroundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            dropPoint.y = hit.point.y + dropGroundOffset;
        else
            dropPoint.y = footY + dropGroundOffset;

        return dropPoint;
    }
}
