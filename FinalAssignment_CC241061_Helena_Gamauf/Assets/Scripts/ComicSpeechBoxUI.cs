using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ComicSpeechBoxUI : MonoBehaviour
{
    public static ComicSpeechBoxUI Instance { get; private set; }

    [SerializeField] float defaultDisplayDuration = 4f;
    [SerializeField] Vector2 panelSize = new(560f, 108f);
    [SerializeField] Vector2 screenOffset = new(0f, 52f);
    [SerializeField] int fontSize = 22;
    [SerializeField] float cornerRoundness = 0.7f;
    [SerializeField] float borderPadding = 6f;
    [SerializeField] Color panelColor = new(1f, 1f, 1f, 0.88f);
    [SerializeField] Color borderColor = new(0.08f, 0.08f, 0.08f, 0.92f);
    [SerializeField] Color textColor = new(0.1f, 0.1f, 0.1f, 1f);

    Text messageText;
    Coroutine hideRoutine;
    static Sprite roundedSprite;
    static float roundedSpriteRoundness = -1f;

    public static void Show(string message, float duration = -1f)
    {
        EnsureInstance();
        Instance.Display(message, duration < 0f ? Instance.defaultDisplayDuration : duration);
    }

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        var existing = FindFirstObjectByType<ComicSpeechBoxUI>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        var uiObject = new GameObject(nameof(ComicSpeechBoxUI));
        DontDestroyOnLoad(uiObject);
        uiObject.AddComponent<ComicSpeechBoxUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUi();
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (roundedSprite != null)
        {
            Destroy(roundedSprite.texture);
            Destroy(roundedSprite);
            roundedSprite = null;
            roundedSpriteRoundness = -1f;
        }
    }

    void BuildUi()
    {
        var canvasObject = new GameObject("ComicSpeechCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var root = new GameObject("SpeechRoot", typeof(RectTransform));
        root.transform.SetParent(canvasObject.transform, false);

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = screenOffset;
        rootRect.sizeDelta = panelSize;

        var border = CreateRoundedImage(
            "Border",
            root.transform,
            borderColor,
            panelSize + new Vector2(borderPadding, borderPadding),
            cornerRoundness);
        border.transform.SetAsFirstSibling();

        var panel = CreateRoundedImage("Panel", root.transform, panelColor, panelSize, cornerRoundness);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 12f);
        textRect.offsetMax = new Vector2(-18f, -12f);

        messageText = textObject.GetComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = fontSize;
        messageText.fontStyle = FontStyle.Bold;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = textColor;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Overflow;
        messageText.raycastTarget = false;
    }

    static GameObject CreateRoundedImage(
        string objectName,
        Transform parent,
        Color color,
        Vector2 size,
        float roundness)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        var image = imageObject.GetComponent<Image>();
        image.sprite = GetRoundedSprite(roundness);
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;

        return imageObject;
    }

    static Sprite GetRoundedSprite(float roundness)
    {
        roundness = Mathf.Clamp01(roundness);
        if (roundedSprite != null && Mathf.Approximately(roundedSpriteRoundness, roundness))
            return roundedSprite;

        if (roundedSprite != null)
        {
            Destroy(roundedSprite.texture);
            Destroy(roundedSprite);
        }

        const int textureSize = 128;
        float cornerRadius = (textureSize * 0.5f - 1f) * roundness;
        roundedSprite = CreateRoundedRectSprite(textureSize, textureSize, cornerRadius);
        roundedSpriteRoundness = roundness;
        return roundedSprite;
    }

    static Sprite CreateRoundedRectSprite(int width, int height, float cornerRadius)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };

        cornerRadius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(width, height) * 0.5f - 1f);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                float alpha = RoundedRectAlpha(x + 0.5f, y + 0.5f, width, height, cornerRadius);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        int border = Mathf.Max(1, Mathf.RoundToInt(cornerRadius));
        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    static float RoundedRectAlpha(float x, float y, float width, float height, float radius)
    {
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float cx = x - halfW;
        float cy = y - halfH;

        float qx = Mathf.Abs(cx) - (halfW - radius);
        float qy = Mathf.Abs(cy) - (halfH - radius);
        float ox = Mathf.Max(qx, 0f);
        float oy = Mathf.Max(qy, 0f);
        float outside = Mathf.Sqrt(ox * ox + oy * oy);
        float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
        float distance = outside + inside - radius;

        return Mathf.Clamp01(0.75f - distance);
    }

    void Display(string message, float duration)
    {
        if (messageText != null)
            messageText.text = message;

        gameObject.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay(duration));
    }

    IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);
        hideRoutine = null;
    }
}
