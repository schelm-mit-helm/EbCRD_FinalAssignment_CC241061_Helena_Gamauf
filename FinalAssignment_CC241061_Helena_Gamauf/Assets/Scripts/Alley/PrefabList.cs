using System.Collections.Generic;
using UnityEngine;

public class PrefabList : MonoBehaviour
{
    [SerializeField] private List<GameObject> prefabs;
    [SerializeField] private List<Transform> locations;
    
    private List<GameObject> spawnedPrefabs = new List<GameObject>();
    void Start()
    {
        SpawnRandomPrefabs();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public GameObject[] SpawnRandomPrefabs()
    {
        GameObject[] chosenPrefabs = GetRandomPrefabs();
        spawnedPrefabs.Clear();

        for (int i = 0; i < locations.Count; i++)   
        {
            spawnedPrefabs.Add(SpawnPrefab(
                chosenPrefabs[i],
                locations[i]
            ));
        }

        return spawnedPrefabs.ToArray();
    } 
    public GameObject[] GetRandomPrefabs()
    {
        GameObject[] randomPrefabs = new GameObject[locations.Count];
        
        for (int i = 0; i < locations.Count; i++)
        {
            List<GameObject> filteredPrefabs = new List<GameObject>();
            
            foreach (GameObject prefab in prefabs)
            {
                if (prefab.CompareTag(locations[i].tag))
                {
                    filteredPrefabs.Add(prefab);
                }
            }

            if (filteredPrefabs.Count > 0)
            {
                int randomIndex = Random.Range(0, filteredPrefabs.Count);
                randomPrefabs[i] = filteredPrefabs[randomIndex];
            }
            else
            {
                Debug.LogError("No Prefab Found for location: " + locations[i].name 
                                + " with tag: " + locations[i].tag);
            }
        }
        return randomPrefabs;
    }
    
    public GameObject SpawnPrefab(GameObject prefab, Transform location)
    {
        return Instantiate(prefab, location);
    } 
        
        

   
}
