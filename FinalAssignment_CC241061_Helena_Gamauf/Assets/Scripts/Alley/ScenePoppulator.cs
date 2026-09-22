using System.Collections.Generic;
using UnityEngine;

public class ScenePoppulator : MonoBehaviour
{
    [SerializeField] private List<GameObject> prefabs;
    [SerializeField] private List<Transform> locations;

    // The prefab originally assigned to each location.
    private readonly List<GameObject> _initialPrefabs = new();

    // The prefab currently being used at each location.
    private readonly List<GameObject> _currentPrefabs = new();

    // The actual instantiated GameObject currently at each location.
    private readonly List<GameObject> _currentObjectsInScene = new();

    // Locations that can still receive an anomaly during this anomaly event.
    private readonly List<int> _availableIndices = new();
    
    // Locations that received an anomaly during the PREVIOUS cycle.
    private readonly HashSet<int> _previousAnomalyIndices = new();

    private void OnEnable()
    {
        if (Anomaly.Instance != null)
        {
            Anomaly.EnsurePersistentInstance().AnomalyDetermined += HandleAnomalyDetermined;
        }
    }

    private void OnDisable()
    {
        if (Anomaly.Instance != null)
        {
            Anomaly.EnsurePersistentInstance().AnomalyDetermined -= HandleAnomalyDetermined;
        }
    }

    private void Start()
    {
        SpawnInitialPrefabs();
    }

    private void HandleAnomalyDetermined()
    {
        Debug.Log("New anomaly state received!");

        // Always restore the scene to its original state first.
        ResetScene();

        if (Anomaly.Instance.AnomalyHappening)
        {
            Debug.Log("About to get anomaly game objects!");
            GetAnomalyGameObjects();
        }
    }

    private void SpawnInitialPrefabs()
    {
        _initialPrefabs.Clear();
        _currentPrefabs.Clear();
        _currentObjectsInScene.Clear();
        _availableIndices.Clear();

        foreach (Transform location in locations)
        {
            GameObject randomPrefab = GetRandomPrefab(location);

            if (randomPrefab == null)
            {
                Debug.LogError(
                    $"Could not spawn initial prefab at location {location.name}."
                );

                _initialPrefabs.Add(null);
                _currentPrefabs.Add(null);
                _currentObjectsInScene.Add(null);

                continue;
            }

            GameObject spawnedObject = SpawnPrefab(
                randomPrefab,
                location
            );

            // Store BOTH the prefab and the actual spawned object.
            _initialPrefabs.Add(randomPrefab);
            _currentPrefabs.Add(randomPrefab);
            _currentObjectsInScene.Add(spawnedObject);
        }

        // Every location is initially available for an anomaly.
        for (int i = 0; i < _currentObjectsInScene.Count; i++)
        {
            if (_currentObjectsInScene[i] != null)
            {
                _availableIndices.Add(i);
            }
        }
    }

    private void GetAnomalyGameObjects()
    {   
        _availableIndices.Clear();

        for (int i = 0; i < _currentObjectsInScene.Count; i++)
        {
            if (_currentObjectsInScene[i] != null &&
                !_previousAnomalyIndices.Contains(i))
            {
                _availableIndices.Add(i);
            }
        }

        // Keep track of THIS cycle separately.
        HashSet<int> currentAnomalyIndices = new();
        
        foreach (int anomalyType in Anomaly.Instance.AnomalyTypePerCount)
        {
         
            if (_availableIndices.Count == 0)
            {
                Debug.LogWarning("No more available locations for anomalies.");
                break;
            }

            int randomListPosition = Random.Range(
                0,
                _availableIndices.Count
            );

            int objectIndex = _availableIndices[randomListPosition];
            currentAnomalyIndices.Add(objectIndex);

            
            _availableIndices.RemoveAt(randomListPosition);

            switch (anomalyType)
            {
                case 0:
                {
                    // Anomaly Type 1: Replace object.
                    
                    ReplaceObject(objectIndex);
                    break;
                }

                case 1:
                {
                    // Anomaly Type 2: Deactivate object.
                    DeactivateObject(objectIndex);

                    break;
                }

                default:
                    Debug.LogWarning(
                        $"Unknown anomaly type: {anomalyType}"
                    );
                    break;
            }
        }
        _previousAnomalyIndices.Clear();

        foreach (int index in currentAnomalyIndices)
        {
            _previousAnomalyIndices.Add(index);
        }
    }

    private void ReplaceObject(int objectIndex)
    {
        GameObject oldObject = _currentObjectsInScene[objectIndex];
        GameObject oldPrefab = _currentPrefabs[objectIndex];

        GameObject newPrefab = GetRandomPrefab(
            locations[objectIndex],
            oldPrefab
        );

        if (newPrefab == null)
        {
            Debug.LogWarning(
                $"Could not replace object at index {objectIndex}. " +
                "There is no alternative prefab. Object will be deactivated."
            );

            if (oldObject != null)
            {
                oldObject.SetActive(false);
            }

            return;
        }

        // IMPORTANT:
        // Spawn the exact prefab we selected above.
        GameObject newObject = SpawnPrefab(
            newPrefab,
            locations[objectIndex]
        );

        // Destroy the old object.
        if (oldObject != null)
        {
            Destroy(oldObject);
        }

        // Update both lists to point at the replacement.
        _currentObjectsInScene[objectIndex] = newObject;
        _currentPrefabs[objectIndex] = newPrefab;

        Debug.Log(
            $"Anomaly Type 1: Replaced object at index {objectIndex}. " +
            $"Old: {oldPrefab.name}, New: {newPrefab.name}"
        );
    }

    private void DeactivateObject(int objectIndex)
    {
        GameObject objectToDeactivate =
            _currentObjectsInScene[objectIndex];

        if (objectToDeactivate != null)
        {
            objectToDeactivate.SetActive(false);

            Debug.Log(
                $"Anomaly Type 2: Deactivated object at index " +
                $"{objectIndex}. Object: {objectToDeactivate.name}"
            );
        }
    }

    private GameObject GetRandomPrefab(
        Transform location,
        GameObject prefabToExclude = null)
    {
        List<GameObject> filteredPrefabs = new();

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            if (prefab.CompareTag(location.tag) &&
                prefab != prefabToExclude)
            {
                filteredPrefabs.Add(prefab);
            }
        }

        if (filteredPrefabs.Count > 0)
        {
            int randomIndex = Random.Range(
                0,
                filteredPrefabs.Count
            );

            return filteredPrefabs[randomIndex];
        }

        Debug.LogWarning(
            $"No alternative prefab found for location: " +
            $"{location.name} with tag: {location.tag}"
        );

        return null;
    }

    private GameObject SpawnPrefab(
        GameObject prefab,
        Transform location)
    {
        if (prefab == null)
        {
            return null;
        }

        return Instantiate(prefab, location);
    }

    private void ResetScene()
    {
        // Destroy every currently spawned object.
        foreach (GameObject obj in _currentObjectsInScene)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        _currentObjectsInScene.Clear();
        _currentPrefabs.Clear();
        _availableIndices.Clear();

        // Spawn exactly one original object at each location.
        for (int i = 0; i < _initialPrefabs.Count; i++)
        {
            GameObject initialPrefab = _initialPrefabs[i];

            if (initialPrefab == null)
            {
                _currentObjectsInScene.Add(null);
                _currentPrefabs.Add(null);
                continue;
            }

            GameObject newObject = SpawnPrefab(
                initialPrefab,
                locations[i]
            );

            _currentObjectsInScene.Add(newObject);
            _currentPrefabs.Add(initialPrefab);

            // Make sure the reset object is active.
            newObject.SetActive(true);

            _availableIndices.Add(i);
        }
    }
}

