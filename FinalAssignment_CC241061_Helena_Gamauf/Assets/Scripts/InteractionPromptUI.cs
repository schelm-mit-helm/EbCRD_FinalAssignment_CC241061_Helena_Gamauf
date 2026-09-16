using UnityEngine;

using UnityEngine.UI;



public class InteractionPromptUI : MonoBehaviour

{

    const string DedicatedCanvasName = "InteractionPromptCanvas";

    const int CanvasSortingOrder = 5;



    [SerializeField]

    Text promptText;



    Canvas rootCanvas;



    public static InteractionPromptUI Instance { get; private set; }



    public static bool IsBlocked => Time.timeScale <= 0f;



    void Awake()

    {

        if (Instance != null && Instance != this)

        {

            Destroy(gameObject);

            return;

        }



        Instance = this;

        EnsureStructure();

        Hide();

    }



    void OnDestroy()

    {

        if (Instance == this)

            Instance = null;

    }



    void LateUpdate()

    {

        if (gameObject.activeSelf && IsBlocked)

            Hide();

    }



    public static InteractionPromptUI EnsureInstance()

    {

        if (Instance != null)

        {

            Instance.EnsureStructure();

            return Instance;

        }



        var existing = FindFirstObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);

        if (existing != null)

        {

            existing.EnsureStructure();

            return existing;

        }



        var canvasObject = CreateDedicatedCanvasObject();

        var promptObject = CreatePromptPanel(canvasObject.transform);

        var promptUi = promptObject.AddComponent<InteractionPromptUI>();

        promptUi.promptText = promptObject.GetComponentInChildren<Text>(true);

        promptUi.rootCanvas = canvasObject.GetComponent<Canvas>();

        promptObject.SetActive(false);



        return promptUi;

    }



    public void Show(string message)

    {

        if (IsBlocked)

        {

            Hide();

            return;

        }



        EnsureStructure();

        ResolvePromptText();



        if (promptText != null)

        {

            promptText.text = message;

            promptText.gameObject.SetActive(true);

        }



        if (rootCanvas != null)

            rootCanvas.gameObject.SetActive(true);



        gameObject.SetActive(true);

    }



    public void Hide()

    {

        gameObject.SetActive(false);

    }



    void EnsureStructure()

    {

        ResolvePromptText();

        EnsureDedicatedCanvas();

        EnsurePanelLayout();

    }



    void ResolvePromptText()

    {

        if (promptText == null)

            promptText = GetComponentInChildren<Text>(true);

    }



    void EnsureDedicatedCanvas()

    {

        rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas != null && rootCanvas.gameObject.name == DedicatedCanvasName)

            return;



        var canvasObject = CreateDedicatedCanvasObject();

        rootCanvas = canvasObject.GetComponent<Canvas>();

        transform.SetParent(canvasObject.transform, false);

    }



    void EnsurePanelLayout()

    {

        if (!TryGetComponent<RectTransform>(out var promptRect))

            return;



        promptRect.anchorMin = new Vector2(0.5f, 0f);

        promptRect.anchorMax = new Vector2(0.5f, 0f);

        promptRect.pivot = new Vector2(0.5f, 0.5f);

        promptRect.anchoredPosition = new Vector2(0f, 170f);

        promptRect.sizeDelta = new Vector2(600f, 60f);



        if (!TryGetComponent<Image>(out var promptImage))

            promptImage = gameObject.AddComponent<Image>();



        promptImage.color = new Color(0f, 0f, 0f, 0.45f);

        promptImage.raycastTarget = false;



        if (promptText == null)

        {

            var textObject = new GameObject("Prompt Text", typeof(RectTransform), typeof(Text));

            textObject.transform.SetParent(transform, false);



            var textRect = textObject.GetComponent<RectTransform>();

            textRect.anchorMin = Vector2.zero;

            textRect.anchorMax = Vector2.one;

            textRect.offsetMin = Vector2.zero;

            textRect.offsetMax = Vector2.zero;



            promptText = textObject.GetComponent<Text>();

            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            promptText.fontSize = 31;

            promptText.alignment = TextAnchor.MiddleCenter;

            promptText.color = Color.white;

            promptText.raycastTarget = false;

        }

    }



    static GameObject CreateDedicatedCanvasObject()

    {

        var existingCanvas = GameObject.Find(DedicatedCanvasName);

        if (existingCanvas != null)

            return existingCanvas;



        var canvasObject = new GameObject(DedicatedCanvasName, typeof(RectTransform));

        var canvas = canvasObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = CanvasSortingOrder;



        var scaler = canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920, 1080);

        scaler.matchWidthOrHeight = 0.5f;



        return canvasObject;

    }



    static GameObject CreatePromptPanel(Transform parent)

    {

        var promptObject = new GameObject("Interaction Prompt", typeof(RectTransform), typeof(Image));

        promptObject.transform.SetParent(parent, false);



        var promptRect = promptObject.GetComponent<RectTransform>();

        promptRect.anchorMin = new Vector2(0.5f, 0f);

        promptRect.anchorMax = new Vector2(0.5f, 0f);

        promptRect.pivot = new Vector2(0.5f, 0.5f);

        promptRect.anchoredPosition = new Vector2(0f, 170f);

        promptRect.sizeDelta = new Vector2(600f, 60f);



        var promptImage = promptObject.GetComponent<Image>();

        promptImage.color = new Color(0f, 0f, 0f, 0.45f);

        promptImage.raycastTarget = false;



        var textObject = new GameObject("Prompt Text", typeof(RectTransform), typeof(Text));

        textObject.transform.SetParent(promptObject.transform, false);



        var textRect = textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;

        textRect.anchorMax = Vector2.one;

        textRect.offsetMin = Vector2.zero;

        textRect.offsetMax = Vector2.zero;



        var text = textObject.GetComponent<Text>();

        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        text.fontSize = 31;

        text.alignment = TextAnchor.MiddleCenter;

        text.color = Color.white;

        text.raycastTarget = false;



        return promptObject;

    }

}


