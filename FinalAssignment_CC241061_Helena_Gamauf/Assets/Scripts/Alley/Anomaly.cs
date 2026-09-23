using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Anomaly : MonoBehaviour
{
    public static Anomaly Instance { get; private set; }

    private bool _anomalyHappening = false;
    private int _anomalyAmount = 0;
    private int[] _anomalyTypePerAnomaly = Array.Empty<int>();


    public bool AnomalyHappening => _anomalyHappening;
    public int AnomalyAmount => _anomalyAmount;
    public int[] AnomalyTypePerAnomaly => _anomalyTypePerAnomaly;

    public event Action AnomalyDetermined;

    public static Anomaly EnsurePersistentInstance()
    {
        if (Instance != null)
            return Instance;

        Anomaly existing = FindFirstObjectByType<Anomaly>(FindObjectsInactive.Include);

        if (existing != null)
        {
            existing.MakePersistent();
            Instance = existing; // Awake() may not have run yet if it was inactive
            return existing;
        }

        GameObject serviceObject = new GameObject(nameof(Anomaly));
        DontDestroyOnLoad(serviceObject);
        return serviceObject.AddComponent<Anomaly>();
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

    public void NewLevelAnomalyGenerator(string other)
    {
        if (other != "Player")
            return;
        AnomalyDifficultyService.EnsurePersistentInstance();
        CheckpointPair.NotifyNewCycle();
        //_anomalyDeterminded = false;
        _anomalyHappening = NextBoolean();
        Debug.Log("Anomaly happening: " + _anomalyHappening);
        if (_anomalyHappening)
        {
            Debug.Log("Anomaly happening");

            DetermineAnomaly();
        }
        else
        {
            ResetAnomaly();
        }
        AnomalyDetermined?.Invoke();
    }

    public void DetermineAnomaly()
    {
        const int anomalyTypeCount = 3; // 0-3, adjust once the actual anomaly types are defined
        _anomalyAmount = AnomalyDifficultyService.Instance.GetAnomalyCount();
        _anomalyTypePerAnomaly = new int[_anomalyAmount];
        for (int i = 0; i < _anomalyTypePerAnomaly.Length; i++)
        {
            _anomalyTypePerAnomaly[i] = AnomalyDifficultyService.Instance.GetAnomalyType(anomalyTypeCount);
        }
        Debug.Log("Anomaly type per count: " + string.Join(", ", _anomalyTypePerAnomaly));
        //_anomalyDeterminded = true;
    }

    public static bool NextBoolean()
    {
        return Random.Range(0, 2) == 0;
    }

    private void ResetAnomaly()
    {
        _anomalyAmount = 0;
        _anomalyTypePerAnomaly = Array.Empty<int>();
    }

}