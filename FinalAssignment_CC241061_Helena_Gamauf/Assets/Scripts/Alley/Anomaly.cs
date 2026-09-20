using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Anomaly : MonoBehaviour
{
    public static Anomaly Instance { get; private set; } //TODO Temporary
    
    private bool _anomalyHappening = false;
    //private bool _anomalyDeterminded = false;
    private int _anomalyVersion;
    private int _anomalyCount = 0;
    private int[] _anomalyTypePerCount;
    public int AnomalyCount => _anomalyCount; 
    
    //public bool AnomalyDeterminded => _anomalyDeterminded;
    public int AnomalyVersion => _anomalyVersion;
    public int[] AnomalyTypePerCount => _anomalyTypePerCount;
    
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        //_anomalyDeterminded = false;
        _anomalyHappening = NextBoolean();
        Debug.Log("Anomaly happening: " + _anomalyHappening);
        if (_anomalyHappening)
        {
            Debug.Log("Anomaly happening");
            _anomalyVersion++;
            DetermineAnomaly();
        }
        else
        {
            ResetAnomaly();
        }
        
    }
    
    public void DetermineAnomaly()
    {
        _anomalyCount = Random.Range(1, 3);
        _anomalyTypePerCount = new int[_anomalyCount];// TODO  Temporary the range will be determined by the difficulty
        for (int i = 0; i < _anomalyTypePerCount.Length; i++)
        {
            _anomalyTypePerCount[i] = Random.Range(0, 2); // Randomly select an anomaly type (0, 1, or 2) TODO what possible anomalies are there
        }
        Debug.Log("Anomaly type per count: " + string.Join(", ", _anomalyTypePerCount));
        //_anomalyDeterminded = true;
    }

    public static bool NextBoolean()
    {
        return Random.Range(0, 2) == 0;
    }
    
    private void ResetAnomaly()
    {
        _anomalyCount = 0;
        _anomalyTypePerCount = Array.Empty<int>();
    }
    
}
