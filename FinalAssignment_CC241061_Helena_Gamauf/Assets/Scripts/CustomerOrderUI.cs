using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CustomerOrderUI : MonoBehaviour
{
    public static CustomerOrderUI Instance { get; private set; }

    const float IconSize = 90f;
    const float Padding = 18f;
    const float SlotSpacing = 4f;
    const float IconInset = 10f;
    const float QuantityBadgeSize = 24f;

    static readonly Color SlotColor = new(0.16f, 0.19f, 0.24f, 1f);
    static readonly Color GatheredSlotColor = new(0.2f, 0.62f, 0.32f, 1f);

    [SerializeField] RectTransform listRoot;

    readonly List<GameObject> rowInstances = new();
    readonly List<Image> slotImages = new();
    bool isBuilt;

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        var existing = FindFirstObjectByType<CustomerOrderUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.MakePersistent();
            return;
        }

        var uiObject = new GameObject(nameof(CustomerOrderUI));
        DontDestroyOnLoad(uiObject);
        uiObject.AddComponent<CustomerOrderUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        MakePersistent();
        BuildUiIfNeeded();
        RestoreIfNeeded();
    }

    void MakePersistent()
    {
        if (gameObject.scene.name == "DontDestroyOnLoad")
            return;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void RestoreIfNeeded()
    {
        var orderService = CustomerOrderService.Instance;
        if (orderService != null && orderService.PlayerHasTaskList)
            ShowOrder(orderService.CurrentOrder);
        else
            Hide();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreIfNeeded();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    public void ShowOrder(IReadOnlyList<Item> order)
    {
        BuildUiIfNeeded();

        if (listRoot == null)
            return;

        ClearRows();

        if (order == null || order.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        var orderService = CustomerOrderService.Instance;
        if (orderService != null && orderService.UseCollapsedOrderDisplay)
        {
            foreach (var entry in orderService.GetOrderDisplayEntries())
                CreateIconSlot(entry.Item, rowInstances.Count, entry.Quantity);
        }
        else
        {
            foreach (var item in order)
                CreateIconSlot(item, rowInstances.Count);
        }

        RefreshAllSlotColors();

        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ClearAndHide()
    {
        ClearRows();
        Hide();
    }

    void BuildUiIfNeeded()
    {
        if (isBuilt)
            return;

        var canvasObject = new GameObject("CustomerOrderCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var listObject = new GameObject("OrderList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        listObject.transform.SetParent(canvasObject.transform, false);
        listRoot = listObject.GetComponent<RectTransform>();

        var listRect = listRoot;
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(0f, 1f);
        listRect.pivot = new Vector2(0f, 1f);
        listRect.anchoredPosition = new Vector2(Padding, -Padding);

        var listLayout = listObject.GetComponent<VerticalLayoutGroup>();
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.spacing = SlotSpacing;
        listLayout.padding = new RectOffset(0, 0, 0, 0);
        listLayout.childControlWidth = false;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = false;
        listLayout.childForceExpandHeight = false;

        isBuilt = true;
    }

    public void MarkItemGathered(int orderIndex)
    {
        var orderService = CustomerOrderService.Instance;
        if (orderService != null && orderService.UseCollapsedOrderDisplay)
            RefreshAllSlotColors();
        else
            RefreshSlotColor(orderIndex);
    }

    public void RefreshAllSlotColors()
    {
        var orderService = CustomerOrderService.Instance;
        if (orderService != null && orderService.UseCollapsedOrderDisplay)
        {
            var entries = orderService.GetOrderDisplayEntries();
            for (var i = 0; i < slotImages.Count && i < entries.Count; i++)
            {
                slotImages[i].color = entries[i].IsComplete ? GatheredSlotColor : SlotColor;
            }

            return;
        }

        for (var i = 0; i < slotImages.Count; i++)
            RefreshSlotColor(i);
    }

    void RefreshSlotColor(int orderIndex)
    {
        if (orderIndex < 0 || orderIndex >= slotImages.Count)
            return;

        var orderService = CustomerOrderService.Instance;
        var gathered = orderService != null && orderService.IsItemGathered(orderIndex);
        slotImages[orderIndex].color = gathered ? GatheredSlotColor : SlotColor;
    }

    void CreateIconSlot(Item item, int orderIndex, int quantity = 1)
    {
        var slot = new GameObject("OrderIcon", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(listRoot, false);

        var slotImage = slot.GetComponent<Image>();
        slotImage.color = SlotColor;

        var slotRect = slot.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(IconSize, IconSize);

        var slotLayout = slot.AddComponent<LayoutElement>();
        slotLayout.minWidth = IconSize;
        slotLayout.preferredWidth = IconSize;
        slotLayout.minHeight = IconSize;
        slotLayout.preferredHeight = IconSize;

        var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(slot.transform, false);

        var iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(IconInset, IconInset);
        iconRect.offsetMax = new Vector2(-IconInset, -IconInset);

        var iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = item?.icon;
        iconImage.preserveAspect = true;
        iconImage.enabled = item?.icon != null;
        iconImage.raycastTarget = false;

        if (quantity > 1)
            AddQuantityBadge(slot, quantity);

        rowInstances.Add(slot);
        slotImages.Add(slotImage);
        RefreshSlotColor(orderIndex);
    }

    static void AddQuantityBadge(GameObject slot, int quantity)
    {
        var badgeObject = new GameObject("QuantityBadge", typeof(RectTransform), typeof(Image));
        badgeObject.transform.SetParent(slot.transform, false);

        var badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 0f);
        badgeRect.anchorMax = new Vector2(1f, 0f);
        badgeRect.pivot = new Vector2(1f, 0f);
        badgeRect.anchoredPosition = new Vector2(-3f, 3f);
        badgeRect.sizeDelta = new Vector2(QuantityBadgeSize, QuantityBadgeSize);

        var badgeImage = badgeObject.GetComponent<Image>();
        badgeImage.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
        badgeImage.raycastTarget = false;

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(badgeObject.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.GetComponent<Text>();
        label.text = quantity.ToString();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 15;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    void ClearRows()
    {
        foreach (var row in rowInstances)
        {
            if (row != null)
                Destroy(row);
        }

        rowInstances.Clear();
        slotImages.Clear();
    }
}
