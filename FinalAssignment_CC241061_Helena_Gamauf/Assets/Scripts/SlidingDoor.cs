using System.Collections;
using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [SerializeField] Vector3 openOffset = new(1.2f, 0f, 0f);
    [SerializeField] float moveSpeed = 2.5f;
    [SerializeField] bool startOpen;

    Vector3 closedLocalPosition;
    Vector3 openLocalPosition;
    bool isOpen;
    Coroutine moveRoutine;

    public bool IsMoving => moveRoutine != null;

    void Awake()
    {
        CacheOpenClosedFromCurrentPosition();
        ApplyStartState();
    }

    public void Configure(Vector3 offset, bool shouldStartOpen)
    {
        openOffset = offset;
        CacheOpenClosedFromCurrentPosition();
        startOpen = shouldStartOpen;
        ApplyStartState();
    }

    public void ConfigureLocalZPositions(float openZ, float closedZ, bool shouldStartOpen)
    {
        var current = transform.localPosition;
        openLocalPosition = new Vector3(current.x, current.y, openZ);
        closedLocalPosition = new Vector3(current.x, current.y, closedZ);
        startOpen = shouldStartOpen;
        ApplyStartState();
    }

    void CacheOpenClosedFromCurrentPosition()
    {
        closedLocalPosition = transform.localPosition;
        openLocalPosition = closedLocalPosition + openOffset;
    }

    void ApplyStartState()
    {
        isOpen = startOpen;
        transform.localPosition = startOpen ? openLocalPosition : closedLocalPosition;
    }

    public void Open()
    {
        if (isOpen)
            return;
        //AkUnitySoundEngine.PostEvent("Play_elevator_door_close", gameObject);
        isOpen = true;
        StartMove(openLocalPosition);
    }

    public void Close()
    {
        if (!isOpen)
            return;
      
        //AkUnitySoundEngine.PostEvent("Play_elevator_door_close", gameObject);
        isOpen = false;
        StartMove(closedLocalPosition);
    }

    public void SnapClosed()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
        
        isOpen = false;
        transform.localPosition = closedLocalPosition;
        
    }

    public IEnumerator WaitUntilIdle()
    {
        while (moveRoutine != null)
            yield return null;
    }

    void StartMove(Vector3 targetLocalPosition)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveToLocal(targetLocalPosition));
    }

    IEnumerator MoveToLocal(Vector3 targetLocalPosition)
    {
        while (Vector3.Distance(transform.localPosition, targetLocalPosition) > 0.01f)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                targetLocalPosition,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.localPosition = targetLocalPosition;
        moveRoutine = null;
    }
}
