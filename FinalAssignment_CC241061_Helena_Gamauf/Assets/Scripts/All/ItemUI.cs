using System;

using UnityEngine;

using UnityEngine.UI;



[RequireComponent(typeof(Button))]

public class ItemUI : MonoBehaviour

{

    static readonly Color NormalColor = new(0.159f, 0.185f, 0.238f, 1f);

    static readonly Color SelectedColor = new(0.42f, 0.48f, 0.62f, 1f);

    static readonly Color EmptyColor = new(0.12f, 0.14f, 0.18f, 0.55f);



    [Header("References")]

    [SerializeField]

    Image iconImage;



    [SerializeField]

    Button button;



    [Header("State")]

    [SerializeField]

    Image backgroundImage;



    const float ItemSize = 96f;



    int slotIndex = -1;

    Inventory inventory;

    bool hasItem;



    void Awake()

    {

        if (backgroundImage == null && button != null)

        {

            backgroundImage = button.targetGraphic as Image;

        }

    }



    public void InitializeSlot(int index, Inventory owner)

    {

        slotIndex = index;

        inventory = owner;



        var rect = (RectTransform)transform;

        rect.localScale = Vector3.one;

        rect.anchorMin = new Vector2(0, 0.5f);

        rect.anchorMax = new Vector2(0, 0.5f);

        rect.pivot = new Vector2(0, 0.5f);

        rect.sizeDelta = new Vector2(ItemSize, ItemSize);



        var layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();

        layoutElement.minWidth = ItemSize;

        layoutElement.preferredWidth = ItemSize;

        layoutElement.minHeight = ItemSize;

        layoutElement.preferredHeight = ItemSize;

        layoutElement.flexibleWidth = 0;

        layoutElement.flexibleHeight = 0;



        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(OnClicked);

        Clear();

    }



    public void SetItem(Item item)

    {

        hasItem = item != null;

        iconImage.sprite = item?.icon;

        iconImage.enabled = item?.icon != null;

        ApplyVisualState(false);

    }



    public void Clear()

    {

        hasItem = false;

        iconImage.sprite = null;

        iconImage.enabled = false;

        ApplyVisualState(false);

    }



    public void SetSelected(bool selected)

    {

        ApplyVisualState(selected);

    }



    void ApplyVisualState(bool selected)

    {

        if (backgroundImage == null)

        {

            return;

        }



        if (!hasItem)

        {

            backgroundImage.color = EmptyColor;

            return;

        }



        backgroundImage.color = selected ? SelectedColor : NormalColor;

    }



    void OnClicked()

    {

        if (inventory != null && hasItem)

        {

            inventory.TrySelectSlot(slotIndex);

        }

    }



    void OnDestroy()

    {

        button.onClick.RemoveAllListeners();

    }

}


