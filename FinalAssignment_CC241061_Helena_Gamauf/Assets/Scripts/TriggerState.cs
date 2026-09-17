using System;
using UnityEngine;

public class TriggerState : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Portal opened!" + "just teleported?" + PortalManager.JustTeleported);
            if (PortalManager.JustTeleported == true) return;
            
            if (PortalManager.PassedCheckpoint1 == false)
            {
                PortalManager.PassedCheckpoint1 = true;
                Debug.Log("PassedCheckpoint1(should true):" + PortalManager.PassedCheckpoint1);
            }
            else
            {
                PortalManager.PassedCheckpoint1 = false;
                Debug.Log("PassedCheckpoint1(should false):" + PortalManager.PassedCheckpoint1);
            }
        } ;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PortalManager.JustTeleported = false;
        }
    }
}
