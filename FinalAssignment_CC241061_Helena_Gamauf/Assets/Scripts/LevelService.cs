using UnityEngine;

public class LevelService : MonoBehaviour
{
    public static LevelService Instance { get; private set; }
    public const int WinLevel = 20;
    int level = 0;
    public int Level => level;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddLevel()
    {
        level += 1;
        CheckWin();
        AnomalyDifficultyService.Instance.AdvanceLevel();
    }

    public void DeductLevels()
    {
        level = Mathf.Max(0, level - 3);
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
        level = 0;
        AnomalyDifficultyService.Instance.ResetDifficulty();
    }
}