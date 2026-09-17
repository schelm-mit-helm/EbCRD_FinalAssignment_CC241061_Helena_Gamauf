using UnityEngine;
using UnityEngine.SceneManagement;

public class IpadScoreDisplay : MonoBehaviour
{
    public static IpadScoreDisplay Instance { get; private set; }

    const string BakerySceneName = SceneNames.Bakery;

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        var existing = FindFirstObjectByType<IpadScoreDisplay>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.MakePersistent();
            return;
        }

        var displayObject = new GameObject(nameof(IpadScoreDisplay));
        DontDestroyOnLoad(displayObject);
        displayObject.AddComponent<IpadScoreDisplay>();
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
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SubscribeToScore();
        RefreshAllAnchors();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromScore();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void MakePersistent()
    {
        if (gameObject.scene.name == "DontDestroyOnLoad")
            return;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == BakerySceneName)
            RefreshAllAnchors();
    }

    void SubscribeToScore()
    {
        var scoreService = ScoreService.Instance ?? ScoreService.EnsurePersistentInstance();
        if (scoreService == null)
            return;

        scoreService.ScoreChanged -= OnScoreChanged;
        scoreService.ScoreChanged += OnScoreChanged;
    }

    void UnsubscribeFromScore()
    {
        if (ScoreService.Instance == null)
            return;

        ScoreService.Instance.ScoreChanged -= OnScoreChanged;
    }

    void OnScoreChanged(int totalScore)
    {
        RefreshAllAnchors(totalScore);
    }

    void RefreshAllAnchors()
    {
        var score = ScoreService.Instance != null ? ScoreService.Instance.TotalScore : 0;
        RefreshAllAnchors(score);
    }

    static void RefreshAllAnchors(int totalScore)
    {
        if (SceneManager.GetActiveScene().name != BakerySceneName)
            return;

        var anchors = FindObjectsByType<IpadScoreAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (anchors.Length == 0)
        {
            Debug.LogWarning(
                $"IpadScoreDisplay: Add {nameof(IpadScoreAnchor)} to your screen placement object (e.g. Ipadsceern) in {BakerySceneName}.");
            return;
        }

        foreach (var anchor in anchors)
            anchor.SetScore(totalScore);
    }
}
