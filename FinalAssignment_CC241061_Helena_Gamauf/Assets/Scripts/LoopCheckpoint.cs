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
        AnomalyDifficultyService.EnsurePersistentInstance();
        
        if (pair.pendingEntry == null)
        {
            pair.pendingEntry = this;
            pair.anomalyAtEntry = Anomaly.Instance.AnomalyHappening;
            //Variable that tells anomaly that the triggers were passed through and that the anomaly should be reset.
            pair.hasTriggeredCheckpoints = true;
            return;
        }
        
        if (LevelService.Instance.Level == 0)
        {
            LevelService.Instance.AddLevel();
            pair.checkpointA.gameObject.GetComponent<Collider>().enabled = false; 
            pair.checkpointB.gameObject.GetComponent<Collider>().enabled = false;
            pair.pendingEntry = null;
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
            pair.checkpointA.gameObject.GetComponent<Collider>().enabled = false; 
            pair.checkpointB.gameObject.GetComponent<Collider>().enabled = false;
        }

        // Reset so the pair is ready for the next crossing.
        pair.pendingEntry = null;
    }
    
}