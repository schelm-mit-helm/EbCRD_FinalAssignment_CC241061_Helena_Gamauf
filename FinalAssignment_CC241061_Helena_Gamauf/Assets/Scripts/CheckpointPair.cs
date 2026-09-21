using System;
using UnityEngine;
public class CheckpointPair : MonoBehaviour
{
    public LoopCheckpoint checkpointA;
    public LoopCheckpoint checkpointB;

    // Which checkpoint the player entered through, and what the anomaly
    // state was at that moment. Null = no crossing currently in progress.
    [HideInInspector] public LoopCheckpoint pendingEntry;
    [HideInInspector] public bool anomalyAtEntry;

    // True once a correct crossing has already scored this cycle. Blocks
    // further AddLevel() calls until a new cycle starts, so the player
    // can't just loop back and forth for infinite levels.
    [HideInInspector] public bool hasScoredThisCycle;

    // Raised whenever the environment/anomaly regenerates elsewhere in
    // the level. Every CheckpointPair resets in response.
    public static event Action OnNewCycle;

    public static void NotifyNewCycle() => OnNewCycle?.Invoke();

    private void OnEnable() => OnNewCycle += ResetCycle;
    private void OnDisable() => OnNewCycle -= ResetCycle;

    private void ResetCycle()
    {
        hasScoredThisCycle = false;
    }
}