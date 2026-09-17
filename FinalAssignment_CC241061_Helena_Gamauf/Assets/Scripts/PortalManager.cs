using UnityEngine;

public class PortalManager : MonoBehaviour
{
    [SerializeField] private GameObject spawnPoint;
    
    public static bool PassedCheckpoint1 = false;
    public static bool JustTeleported = false;
    public bool fromoutside = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            /*if(fromoutside)
            {
                other.transform.position = spawnPoint.transform.position;
                fromoutside = false;
            }*/
            
            if (PassedCheckpoint1)
            {
                Debug.Log("Portal teleporting! from " + other.transform.position.z + " to " + spawnPoint.transform.position.z + "");
                Debug.Log("Portal teleporting! from " + other.transform.position.x + " to " + spawnPoint.transform.position.x + "");
                other.transform.position = new Vector3(spawnPoint.transform.position.x, other.transform.position.y, spawnPoint.transform.position.z);
                Debug.Log("Portal teleported to " + other.transform.position.x + " and " + other.transform.position.z + "");
                Debug.Log("Portal teleporting! from " + this.name + " to " + spawnPoint.name + "");
                
                PassedCheckpoint1 = false;
                JustTeleported = true;
                Debug.Log("PassedCheckpoint1(should false):" + PassedCheckpoint1 + " at " + this.name);
                Debug.Log("JustTeleported(should true):" + JustTeleported);
            }
            else
            {
                Debug.Log("Portal blocked!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fromoutside = true;
        }
    }
}
