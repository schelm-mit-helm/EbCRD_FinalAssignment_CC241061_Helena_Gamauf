using UnityEngine;

public class PlacePrefab : MonoBehaviour
{
    [SerializeField] private GameObject objectPrefab;
    [SerializeField] private GameObject positionInScene;
    void Start()
    {
        PlaceObject();
    }

    void PlaceObject()
    {
        Instantiate(objectPrefab, positionInScene.transform.position, positionInScene.transform.rotation);
    }
}
