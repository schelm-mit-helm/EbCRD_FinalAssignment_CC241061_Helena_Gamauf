using System;
using UnityEngine;
public class CheckpointPair : MonoBehaviour
{
    public LoopCheckpoint checkpointA;
    public LoopCheckpoint checkpointB;
    
    [HideInInspector] public LoopCheckpoint pendingEntry;
    [HideInInspector] public bool anomalyAtEntry;

  
    [HideInInspector] public bool hasScoredThisCycle;
    [HideInInspector] public bool hasTriggeredCheckpoints;

   
    public static event Action OnNewCycle;

    public static void NotifyNewCycle() => OnNewCycle?.Invoke();

    private void OnEnable() => OnNewCycle += ResetCycle;
    private void OnDisable() => OnNewCycle -= ResetCycle;

    private void ResetCycle()
    {
        hasScoredThisCycle = false;
        hasTriggeredCheckpoints = false;
        if(!checkpointA.gameObject.GetComponent<Collider>().enabled || !checkpointB.gameObject.GetComponent<Collider>().enabled)
        {
            checkpointA.gameObject.GetComponent<Collider>().enabled = true; 
            checkpointB.gameObject.GetComponent<Collider>().enabled = true;
        }
    }
}