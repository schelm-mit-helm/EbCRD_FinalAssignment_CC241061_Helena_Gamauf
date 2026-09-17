using UnityEngine;

public class Rotator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    bool randomInit;
    [SerializeField]
    float speed;

    void Start()
    {
        if (randomInit)
        {
            var eulers = transform.eulerAngles;
            eulers.y = Random.Range(0.0f, 360.0f);
            transform.eulerAngles = eulers;
        }
    }
    void Update()
    {
        transform.Rotate(speed * Time.deltaTime * Vector3.up);
    }
}
