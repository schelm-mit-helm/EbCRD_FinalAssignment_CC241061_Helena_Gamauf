using System.Collections.Generic;
using UnityEngine;

public class ScenePoppulator : MonoBehaviour
{
    [SerializeField] private List<GameObject> prefabs;
    [SerializeField] private List<Transform> locations;
    
    private bool _isAnomalyInstantiated = false;
    private int _lastInstantiatedAnomaly = 0;
    private List<GameObject> _initialObjectsInScene = new List<GameObject>();
    private List<GameObject> _currentObjectsInScene = new List<GameObject>();
    private List<int> _availableIndices = new List<int>();
    void Start()
    {
        SpawnInitialPrefabs();
        
    }
    void Update()
    {
        if (Anomaly.Instance.AnomalyVersion != _lastInstantiatedAnomaly)
        {
            //_isAnomalyInstantiated = true;
            Debug.Log("In ScenePopulator: Anomaly happening was detected");
            
            _currentObjectsInScene.Clear();
            _currentObjectsInScene = new List<GameObject>(_initialObjectsInScene);
            
            _availableIndices.Clear();
            for (int i = 0; i < _currentObjectsInScene.Count; i++)
            {
                _availableIndices.Add(i);
            }
            
            GetAnomalyGameObjects();
            _lastInstantiatedAnomaly = Anomaly.Instance.AnomalyVersion;
        } 
        
    }
    
    private GameObject[] SpawnInitialPrefabs()
    {
        GameObject[] chosenPrefabs = GetRandomPrefabs();
        _initialObjectsInScene.Clear();

        for (int i = 0; i < locations.Count; i++)   
        {
            _initialObjectsInScene.Add(SpawnPrefab(
                chosenPrefabs[i],
                locations[i]
            ));
        }
        
        return _initialObjectsInScene.ToArray();
    }

    private List<GameObject> GetAnomalyGameObjects()
    {
        for (int i = 0; i < Anomaly.Instance.AnomalyTypePerCount.Length; i++)
        {
            int randomListPosition = Random.Range(0, _availableIndices.Count);
            int objectIndex = _availableIndices[randomListPosition];

            _availableIndices.RemoveAt(randomListPosition);
            
            switch (Anomaly.Instance.AnomalyTypePerCount[i])
            {
                case 0:
                    GameObject oldObject = _currentObjectsInScene[objectIndex];

                    GameObject newObject = SpawnPrefab(
                        GetRandomPrefab(locations[objectIndex]),
                        locations[objectIndex]
                    );

                    Destroy(oldObject);

                    _currentObjectsInScene[objectIndex] = newObject;
                    break;

                case 1:
                    // Anomaly Type 2: Deactivate object
                    _currentObjectsInScene[objectIndex].SetActive(false);
                    break;
                default:
                    Debug.Log("No anomaly occurred.");
                    break;
            }

        }
        return _currentObjectsInScene;
    }
    public GameObject[] GetRandomPrefabs()
    {
        GameObject[] randomPrefabs = new GameObject[locations.Count];
        
        for (int i = 0; i < locations.Count; i++)
        {
            GameObject randomPrefab = GetRandomPrefab(locations[i]);
            randomPrefabs[i] = randomPrefab;
        }
        return randomPrefabs;
    }
    
    public GameObject GetRandomPrefab(Transform location)
    {
        List<GameObject> filteredPrefabs = new List<GameObject>();

        foreach (GameObject prefab in prefabs)
        {
            if (prefab.CompareTag(location.tag))
            {
                filteredPrefabs.Add(prefab);
            }
        }

        if (filteredPrefabs.Count > 0)
        {
            int randomIndex = Random.Range(0, filteredPrefabs.Count);
            return filteredPrefabs[randomIndex];
        }

        Debug.LogError("No Prefab Found for location: " + location.name
                                                        + " with tag: " + location.tag);

        return null;
    }
    
    public GameObject SpawnPrefab(GameObject prefab, Transform location)
    {
        return Instantiate(prefab, location);
    } 
      
    public void ResetScene()
    {
     
    }
}
