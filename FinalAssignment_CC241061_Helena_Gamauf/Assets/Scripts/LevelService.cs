using System;
using UnityEngine;

public class LevelService : MonoBehaviour
{
    public static LevelService Instance { get; private set; }
    public const int WinLevel = 20;
    int level = 0;
    public int Level => level;

    public event Action<int> OnLevelChanged;

    public static LevelService EnsurePersistentInstance()
    {
        if (Instance != null)
            return Instance;

        LevelService existing = FindFirstObjectByType<LevelService>(FindObjectsInactive.Include);

        if (existing != null)
        {
            existing.MakePersistent();
            Instance = existing; // Awake() may not have run yet if it was inactive
            return existing;
        }

        GameObject serviceObject = new GameObject(nameof(LevelService));
        DontDestroyOnLoad(serviceObject);
        return serviceObject.AddComponent<LevelService>();
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

    public void AddLevel()
    {
        SetLevel(level + 1);
        CheckWin();
        AnomalyDifficultyService.Instance.AdvanceLevel();
    }

    public void DeductLevels()
    {
        SetLevel(Mathf.Max(0, level - 3));
        AnomalyDifficultyService.Instance.DecreaseLevel();
        CheckWin();
    }

    void CheckWin()
    {
        if (level >= WinLevel)
        {
            Debug.Log("You won!");
        }
    }

    public void ResetLevel()
    {
        SetLevel(0);
        AnomalyDifficultyService.Instance.ResetDifficulty();
    }

    void SetLevel(int newLevel)
    {
        if (level == newLevel) return;
        level = newLevel;
        OnLevelChanged?.Invoke(level);
    }
}