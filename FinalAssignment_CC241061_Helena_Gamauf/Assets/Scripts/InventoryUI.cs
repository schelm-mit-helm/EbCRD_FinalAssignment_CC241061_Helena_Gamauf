using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class InventoryUI : MonoBehaviour
{
    const string UiItemPrefabGuid = "94757972e5314f34f99ea7cdf1326e51";

    [Header("Prefabs")]
    [SerializeField]
    GameObject uiItemPrefab;

    [Header("References")]
    [SerializeField]
    Transform uiInventoryParent;

    readonly ItemUI[] slotUIs = new ItemUI[Inventory.MaxSlots];
    int selectedSlot = -1;
    bool slotsInitialized;

    public bool IsInitialized => slotsInitialized;

    void Awake()
    {
        ResolveReferences();
    }

    public void ResolveReferences()
    {
        if (uiInventoryParent == null)
        {
            var scrollRect = GetComponent<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null)
            {
                uiInventoryParent = scrollRect.content;
            }
        }

        if (uiItemPrefab != null)
        {
            return;
        }

#if UNITY_EDITOR
        var prefabPath = UnityEditor.AssetDatabase.GUIDToAssetPath(UiItemPrefabGuid);
        if (!string.IsNullOrEmpty(prefabPath))
        {
            uiItemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
#endif

        if (uiItemPrefab == null)
        {
            uiItemPrefab = Resources.Load<GameObject>("UI Item Prefab");
        }
    }

    public void InitializeSlots(Inventory inventory)
    {
        if (slotsInitialized)
        {
            return;
        }

        ResolveReferences();

        if (uiItemPrefab == null || uiInventoryParent == null)
        {
            Debug.LogError($"{nameof(InventoryUI)} on {name}: Assign uiItemPrefab and uiInventoryParent in the Inspector.");
            return;
        }

        for (var i = 0; i < Inventory.MaxSlots; i++)
        {
            var itemUI = Instantiate(uiItemPrefab, uiInventoryParent).GetComponent<ItemUI>();
            itemUI.InitializeSlot(i, inventory);
            slotUIs[i] = itemUI;
        }

        slotsInitialized = true;
        RebuildLayout();
    }

    public void SetSlot(int slot, Item item)
    {
        if (slot < 0 || slot >= Inventory.MaxSlots)
        {
            return;
        }

        if (slotUIs[slot] == null)
        {
            return;
        }

        slotUIs[slot].SetItem(item);
        RebuildLayout();
    }

    public void ClearSlot(int slot)
    {
        if (slot < 0 || slot >= Inventory.MaxSlots)
        {
            return;
        }

        if (slotUIs[slot] == null)
        {
            return;
        }

        slotUIs[slot].Clear();

        if (selectedSlot == slot)
        {
            selectedSlot = -1;
        }

        RebuildLayout();
    }

    public void SetSelectedSlot(int slot)
    {
        selectedSlot = slot;

        for (var i = 0; i < Inventory.MaxSlots; i++)
        {
            if (slotUIs[i] == null)
            {
                continue;
            }

            slotUIs[i].SetSelected(i == slot);
        }
    }

    void RebuildLayout()
    {
        if (uiInventoryParent == null)
        {
            return;
        }

        var content = (RectTransform)uiInventoryParent;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }
}
