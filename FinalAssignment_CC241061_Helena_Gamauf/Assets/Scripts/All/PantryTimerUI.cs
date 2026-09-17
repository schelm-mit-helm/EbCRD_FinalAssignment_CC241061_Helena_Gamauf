using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PantryTimerUI : MonoBehaviour
{
    public static PantryTimerUI Instance { get; private set; }

    const float DurationSeconds = 120f;
    const float BlinkInterval = 0.6f;
    const float PaddingRight = 96f;
    const float PaddingTop = 20f;
    const float DotSize = 36f;
    const float DotTextSpacing = 10f;
    const float TimeTextWidth = 130f;

    static readonly Color RecordingRed = new(0.92f, 0.12f, 0.12f, 1f);

    [SerializeField] Text timeText;
    [SerializeField] Image recordingDot;

    float remainingSeconds;
    float pantryPenaltyTimer;
    bool isRunning;
    bool isBuilt;
    bool hasExpired;

    public static void EnsureAndStart()
    {
        if (!SceneNames.IsGatheringScene(SceneManager.GetActiveScene().name))
            return;

        var timer = Instance;
        if (timer == null)
        {
            var timerObject = new GameObject(nameof(PantryTimerUI));
            timer = timerObject.AddComponent<PantryTimerUI>();
        }

        timer.StartTimer();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SubscribeToSceneLoads()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        HandleScene(SceneManager.GetActiveScene().name);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HandleScene(scene.name);

    static void HandleScene(string sceneName)
    {
        if (SceneNames.IsGatheringScene(sceneName))
            return;

        if (Instance != null)
            Instance.Hide();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUiIfNeeded();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!isRunning)
            return;

        remainingSeconds -= Time.deltaTime;
        ApplyPantryTimePenalty();
        if (remainingSeconds <= 0f)
        {
            remainingSeconds = 0f;
            isRunning = false;
            if (recordingDot != null)
                recordingDot.enabled = false;

            if (!hasExpired)
            {
                hasExpired = true;
                PlayerDeath.KillPlayer();
            }
        }

        UpdateTimeDisplay();
        UpdateBlinkingDot();
    }

    public void StartTimer()
    {
        BuildUiIfNeeded();
        remainingSeconds = DurationSeconds;
        pantryPenaltyTimer = 0f;
        isRunning = true;
        hasExpired = false;

        if (recordingDot != null)
            recordingDot.enabled = true;

        UpdateTimeDisplay();
        gameObject.SetActive(true);
    }

    void Hide()
    {
        isRunning = false;
        pantryPenaltyTimer = 0f;
        gameObject.SetActive(false);
    }

    void ApplyPantryTimePenalty()
    {
        pantryPenaltyTimer += Time.deltaTime;

        while (pantryPenaltyTimer >= 1f)
        {
            pantryPenaltyTimer -= 1f;
            ScoreService.EnsurePersistentInstance()?.DeductPantrySecondScore();
        }
    }

    void UpdateTimeDisplay()
    {
        if (timeText == null)
            return;

        var totalSeconds = Mathf.CeilToInt(remainingSeconds);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        timeText.text = $"{minutes:00}:{seconds:00}";
    }

    void UpdateBlinkingDot()
    {
        if (recordingDot == null || !isRunning)
            return;

        var blinkOn = Mathf.FloorToInt(Time.unscaledTime / BlinkInterval) % 2 == 0;
        recordingDot.enabled = blinkOn;
    }

    void BuildUiIfNeeded()
    {
        if (isBuilt)
            return;

        var canvasObject = new GameObject("PantryTimerCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        var panelObject = new GameObject("TimerPanel", typeof(RectTransform));
        panelObject.transform.SetParent(canvasObject.transform, false);

        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-PaddingRight, -PaddingTop);

        var layout = panelObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.spacing = DotTextSpacing;
        layout.padding = new RectOffset(0, 4, 0, 0);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var dotObject = new GameObject("RecordingDot", typeof(RectTransform), typeof(Image));
        dotObject.transform.SetParent(panelObject.transform, false);

        var dotRect = dotObject.GetComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(DotSize, DotSize);

        recordingDot = dotObject.GetComponent<Image>();
        recordingDot.sprite = CreateCircleSprite(Mathf.RoundToInt(DotSize));
        recordingDot.color = RecordingRed;
        recordingDot.raycastTarget = false;

        var dotLayout = dotObject.AddComponent<LayoutElement>();
        dotLayout.minWidth = DotSize;
        dotLayout.preferredWidth = DotSize;
        dotLayout.minHeight = DotSize;
        dotLayout.preferredHeight = DotSize;

        var textObject = new GameObject("TimeText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panelObject.transform, false);

        timeText = textObject.GetComponent<Text>();
        timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timeText.fontSize = 41;
        timeText.fontStyle = FontStyle.Bold;
        timeText.alignment = TextAnchor.MiddleRight;
        timeText.color = Color.white;
        timeText.raycastTarget = false;

        var textLayout = textObject.AddComponent<LayoutElement>();
        textLayout.minWidth = TimeTextWidth;
        textLayout.preferredWidth = TimeTextWidth;

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(TimeTextWidth, 50f);

        isBuilt = true;
    }

    static Sprite CreateCircleSprite(int size = 32)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = size * 0.5f;
        var radius = size * 0.5f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f);
    }
}
