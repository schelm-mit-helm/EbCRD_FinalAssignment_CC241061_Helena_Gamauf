using UnityEngine;

public class AnomalyDifficultyService : MonoBehaviour
{
    public static AnomalyDifficultyService Instance { get; private set; }

    [Header("Anomaly count")]
    [SerializeField] private int startingAnomalyCount = 3;
    [SerializeField] private int minAnomalyCount = 1;
    [SerializeField] private int anomalyCountDecreasePerLevel = 1;
    [SerializeField] private int levelsPerAnomalyCountStep = 3; // every N levels, count drops by anomalyCountDecreasePerLevel

    [Header("Anomaly type bias")]
    [Tooltip("How many levels it takes to reach the strongest bias toward high type indices.")]
    [SerializeField] private int levelsToReachMaxBias = 20;
    [Tooltip("Lower = stronger pull toward the higher type indices at max difficulty. 1 = no bias.")]
    [SerializeField] private float minTypeExponent = 0.35f;

    private int _levelIndex;

    public int LevelIndex => _levelIndex;

    public static AnomalyDifficultyService EnsurePersistentInstance()
    {
        if (Instance != null)
            return Instance;

        AnomalyDifficultyService existing = FindFirstObjectByType<AnomalyDifficultyService>(FindObjectsInactive.Include);

        if (existing != null)
        {
            existing.MakePersistent();
            Instance = existing; // Awake() may not have run yet if it was inactive
            return existing;
        }

        GameObject serviceObject = new GameObject(nameof(AnomalyDifficultyService));
        DontDestroyOnLoad(serviceObject);
        return serviceObject.AddComponent<AnomalyDifficultyService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        MakePersistent();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void MakePersistent()
    {
        if (gameObject.scene.name == "DontDestroyOnLoad")
            return;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void AdvanceLevel()
    {
        _levelIndex++;
    }

    public void DecreaseLevel()
    {
        if (_levelIndex > 0)
        {
            _levelIndex--;
        }
    }

    public void ResetDifficulty()
    {
        _levelIndex = 0;
    }

    // How many anomalies should appear this cycle. Decreases as the level
    // goes on (fewer clues = harder to spot), floored at minAnomalyCount.
    public int GetAnomalyCount()
    {
        int steps = _levelIndex / levelsPerAnomalyCountStep;
        int count = startingAnomalyCount - steps * anomalyCountDecreasePerLevel;
        return Mathf.Max(minAnomalyCount, count);
    }

    // Picks an anomaly "type" index in [0, typeCount). At low levels this is
    // roughly uniform; as the level goes on it skews toward the higher
    // indices, so reserve those for the subtler/harder anomaly types
    // (e.g. material changes) once you add them.
    public int GetAnomalyType(int typeCount)
    {
        if (typeCount <= 1) return 0;

        float t = Mathf.Clamp01((float)_levelIndex / levelsToReachMaxBias);
        float exponent = Mathf.Lerp(1f, minTypeExponent, t);
        float sample = Mathf.Pow(Random.value, exponent); // exponent < 1 skews the sample toward 1.0
        int index = Mathf.FloorToInt(sample * typeCount);
        return Mathf.Clamp(index, 0, typeCount - 1);
    }
}