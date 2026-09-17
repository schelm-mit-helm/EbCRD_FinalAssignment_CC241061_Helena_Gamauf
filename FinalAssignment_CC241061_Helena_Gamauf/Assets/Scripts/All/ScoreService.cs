using System;
using UnityEngine;

public class ScoreService : MonoBehaviour
{
    public static ScoreService Instance { get; private set; }

    public const int PointsPerDeliveredItem = 75;
    public const int PointsPerPantrySecond = 1;

    int totalScore;

    public int TotalScore => totalScore;

    public event Action<int> ScoreChanged;

    public static ScoreService EnsurePersistentInstance()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<ScoreService>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.MakePersistent();
            return Instance;
        }

        var serviceObject = new GameObject(nameof(ScoreService));
        DontDestroyOnLoad(serviceObject);
        return serviceObject.AddComponent<ScoreService>();
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

    public void AddDeliveryScore()
    {
        totalScore += PointsPerDeliveredItem;
        ScoreChanged?.Invoke(totalScore);

        MazeDifficultyService.EnsurePersistentInstance().AdvanceLoop();
    }

    public void DeductPantrySecondScore()
    {
        totalScore -= PointsPerPantrySecond;
        ScoreChanged?.Invoke(totalScore);
    }

    public void ResetScore()
    {
        totalScore = 0;
        ScoreChanged?.Invoke(totalScore);

        MazeDifficultyService.EnsurePersistentInstance().ResetDifficulty();
    }
}