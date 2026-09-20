using UnityEngine;

public class AnomalyDifficultyService : MonoBehaviour
{
    public static AnomalyDifficultyService Instance { get; private set; }

    [SerializeField] private int startingAnomalyCount = 3;
    [SerializeField] private int anomalyCountDecreasePerLevel = 1;

    private int _levelIndex;
    
    public int LevelIndex => _levelIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
