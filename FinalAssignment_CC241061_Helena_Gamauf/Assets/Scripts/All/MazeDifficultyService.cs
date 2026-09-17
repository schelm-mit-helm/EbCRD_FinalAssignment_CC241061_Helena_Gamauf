using UnityEngine;

public class MazeDifficultyService : MonoBehaviour
{
    public static MazeDifficultyService Instance { get; private set; }

    [SerializeField] private int startingMazeSize = 5;
    [SerializeField] private int mazeSizeIncreasePerLoop = 2;
    [SerializeField] private int startingEnemyCount = 1;
    [SerializeField] private int enemyIncreasePerLoop = 1;

    private int loopIndex;

    public int LoopIndex => loopIndex;
    public int CurrentMazeSize => startingMazeSize + loopIndex * mazeSizeIncreasePerLoop;
    public int CurrentEnemyCount => startingEnemyCount + (loopIndex / 4) * enemyIncreasePerLoop;

    public static MazeDifficultyService EnsurePersistentInstance()
    {
        if (Instance != null)
            return Instance;

        MazeDifficultyService existing = FindFirstObjectByType<MazeDifficultyService>(FindObjectsInactive.Include);

        if (existing != null)
        {
            existing.MakePersistent();
            return Instance;
        }

        GameObject serviceObject = new GameObject(nameof(MazeDifficultyService));
        DontDestroyOnLoad(serviceObject);
        return serviceObject.AddComponent<MazeDifficultyService>();
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

    public void AdvanceLoop()
    {
        loopIndex++;
    }

    public void ResetDifficulty()
    {
        loopIndex = 0;
    }
}