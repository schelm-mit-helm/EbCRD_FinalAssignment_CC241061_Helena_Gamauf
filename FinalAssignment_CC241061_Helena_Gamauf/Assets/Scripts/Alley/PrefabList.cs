using UnityEngine;

public class PrefabList : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Transform[] locations;
    
    
    private int _locationCount;
    void Start()
    {
        _locationCount = locations.Length;
        SpawnRandomPrefabs();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public GameObject[] SpawnRandomPrefabs()
    {
        GameObject[] chosenPrefabs = GetRandomPrefabs();
        GameObject[] spawnedPrefabs = new GameObject[locations.Length];

        for (int i = 0; i < locations.Length; i++)
        {
            spawnedPrefabs[i] = SpawnPrefab(
                chosenPrefabs[i],
                locations[i]
            );
        }

        return spawnedPrefabs;
    } 
    public GameObject[] GetRandomPrefabs()
    {
        GameObject[] chosenPrefabs = new GameObject[locations.Length];
        
        for (int i = 0; i < locations.Length; i++)
        {

            //locations[i].tag = ;
            int randomIndex = Random.Range(0, prefabs.Length);
            chosenPrefabs[i] = prefabs[randomIndex];
            if(chosenPrefabs[i].tag == locations[i].tag){}
        }
        return chosenPrefabs;
    }
    
    public GameObject SpawnPrefab(GameObject prefab, Transform location)
    {
        return Instantiate(prefab, location);
    } 
        
        

   
}
