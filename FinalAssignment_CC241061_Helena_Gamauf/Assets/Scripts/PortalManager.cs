using UnityEngine;

public class PortalManager : MonoBehaviour
{
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private CheckpointPair pair;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        LevelService.EnsurePersistentInstance();
        Anomaly.EnsurePersistentInstance();

        if (pair.hasTriggeredCheckpoints)
        {
            Anomaly.Instance.NewLevelAnomalyGenerator(other.tag);
        }
        other.transform.position = new Vector3(
            spawnPoint.transform.position.x +
            (other.transform.position.x - transform.position.x),
            other.transform.position.y,
            spawnPoint.transform.position.z + 
            (other.transform.position.z - transform.position.z));
    }
}