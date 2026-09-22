using UnityEngine;

public class PortalManager : MonoBehaviour
{
    [SerializeField] private GameObject spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelService.EnsurePersistentInstance();
            Anomaly.EnsurePersistentInstance();
            if (LevelService.Instance.Level > 0)
            {
                Anomaly.Instance.CheckAnomaly(other.tag);
            }
            other.transform.position = new Vector3(
                spawnPoint.transform.position.x + (other.transform.position.x - transform.position.x), 
                other.transform.position.y, 
                spawnPoint.transform.position.z + (other.transform.position.z - transform.position.z));
        }
    }
    
}
