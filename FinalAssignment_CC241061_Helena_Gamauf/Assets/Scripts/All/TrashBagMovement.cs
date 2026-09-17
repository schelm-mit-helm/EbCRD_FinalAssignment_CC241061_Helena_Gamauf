using UnityEngine;

public class TrashBagMovement : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 2f;

    private bool isMoving;

    void Update()
    {
        if (isMoving)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
    }

    public void StartMoving()
    {
        isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;
    }
}