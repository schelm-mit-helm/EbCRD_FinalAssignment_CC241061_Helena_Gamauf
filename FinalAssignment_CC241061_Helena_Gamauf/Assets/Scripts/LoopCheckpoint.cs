using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LoopCheckpoint : MonoBehaviour
{
    
    [SerializeField] private CheckpointPair pair;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (pair == null)
        {
            Debug.Log($"{name}: no CheckpointPair assigned.", this);
            return;
        }
        LevelService.EnsurePersistentInstance();
        Anomaly.EnsurePersistentInstance();
        
        if (pair.pendingEntry == null)
        {
            pair.pendingEntry = this;
            pair.anomalyAtEntry = Anomaly.Instance.AnomalyHappening;
            return;
        }

        bool sameCheckpointAsEntry = pair.pendingEntry == this; // Did the player exit through the same checkpoint they entered? this is the "turn around" case.
        bool anomaly = pair.anomalyAtEntry;
        bool correct = sameCheckpointAsEntry ? anomaly : !anomaly;

        if (correct)
        {
            if (!pair.hasScoredThisCycle)
            {
                LevelService.Instance.AddLevel();
                pair.hasScoredThisCycle = true;
            }
        }
        else
        {
            LevelService.Instance.DeductLevels();
        }

        // Reset so the pair is ready for the next crossing.
        pair.pendingEntry = null;
    }
    
}