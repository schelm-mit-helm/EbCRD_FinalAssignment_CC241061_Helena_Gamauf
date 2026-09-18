using UnityEngine;

[CreateAssetMenu(fileName = "Alley", menuName = "Alleys/Alley")]
public class Alley : ScriptableObject
{
    [Header("Items")]
    public GameObject[] itemBundlePrefabs;
}
