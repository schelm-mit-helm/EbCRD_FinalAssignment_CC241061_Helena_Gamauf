using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class IpadScoreAnchor : MonoBehaviour
{
    const string CanvasChildName = "ScoreCanvas";

    [Header("Canvas On Screen (used when creating a new canvas)")]
    [SerializeField] Vector3 canvasLocalPosition = new(0f, 0f, -0.5f);
    [SerializeField] Vector3 canvasLocalEulerAngles = Vector3.zero;
    [SerializeField] Vector3 canvasLocalScale = new(0.001f, 0.001f, 0.001f);
    [SerializeField] Vector2 canvasSize = new(400f, 220f);
    [SerializeField] bool showBackground;

    [Header("Text (edit here, or on the Text objects in the scene)")]
    [SerializeField] int titleFontSize = 50;
    [SerializeField] int scoreFontSize = 60;
    [SerializeField] Color titleColor = Color.white;
    [SerializeField] Color scoreColor = new(1f, 0.656f, 0.988f, 1f);

    Text scoreText;
    Transform canvasTransform;

    public void SetScore(int totalScore)
    {
        EnsureCanvas();
        if (scoreText != null)
            scoreText.text = totalScore.ToString();
    }

    void OnEnable()
    {
        EnsureCanvas();
        var score = ScoreService.Instance != null ? ScoreService.Instance.TotalScore : 0;
        SetScore(score);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        EnsureCanvas();
        ApplyBackgroundVisibility();
        UpdateTextStyles();
    }
#endif

    void EnsureCanvas()
    {
        canvasTransform = transform.Find(CanvasChildName);
        if (canvasTransform == null)
        {
            BuildCanvas();
            return;
        }

        BindExistingCanvas();
        ApplyBackgroundVisibility();

        if (!Application.isPlaying)
            UpdateTextStyles();
    }

    void BuildCanvas()
    {
        var canvasObject = new GameObject(CanvasChildName, typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        canvasTransform = canvasObject.transform;

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(canvasObject.transform, false);

        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.85f);
        background.GetComponent<Image>().raycastTarget = false;

        var titleObject = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(canvasObject.transform, false);

        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.55f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(8f, 0f);
        titleRect.offsetMax = new Vector2(-8f, -8f);

        var titleText = titleObject.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = titleFontSize;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = titleColor;
        titleText.text = "Score";
        titleText.raycastTarget = false;

        var scoreObject = new GameObject("ScoreValue", typeof(RectTransform), typeof(Text));
        scoreObject.transform.SetParent(canvasObject.transform, false);

        var scoreRect = scoreObject.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 0f);
        scoreRect.anchorMax = new Vector2(1f, 0.55f);
        scoreRect.offsetMin = new Vector2(8f, 8f);
        scoreRect.offsetMax = new Vector2(-8f, 0f);

        scoreText = scoreObject.GetComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = scoreFontSize;
        scoreText.fontStyle = FontStyle.Bold;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.color = scoreColor;
        scoreText.text = "0";
        scoreText.raycastTarget = false;

        ApplyCanvasTransform();
        ApplyBackgroundVisibility();
    }

    void UpdateTextStyles()
    {
        if (canvasTransform == null)
            return;

        var title = canvasTransform.Find("Title")?.GetComponent<Text>();
        if (title != null)
        {
            title.fontSize = titleFontSize;
            title.color = titleColor;
        }

        if (scoreText != null)
        {
            scoreText.fontSize = scoreFontSize;
            scoreText.color = scoreColor;
        }

        var canvasRect = canvasTransform.GetComponent<RectTransform>();
        if (canvasRect != null)
            canvasRect.sizeDelta = canvasSize;
    }

    void BindExistingCanvas()
    {
        var scoreTransform = canvasTransform.Find("ScoreValue");
        scoreText = scoreTransform != null
            ? scoreTransform.GetComponent<Text>()
            : canvasTransform.GetComponentInChildren<Text>(true);
    }

    void ApplyCanvasTransform()
    {
        if (canvasTransform == null)
            return;

        canvasTransform.localPosition = canvasLocalPosition;
        canvasTransform.localRotation = Quaternion.Euler(canvasLocalEulerAngles);
        canvasTransform.localScale = canvasLocalScale;
    }

    void ApplyBackgroundVisibility()
    {
        if (canvasTransform == null)
            return;

        var background = canvasTransform.Find("Background")?.GetComponent<Image>();
        if (background == null)
            return;

        background.enabled = showBackground;
    }
}
