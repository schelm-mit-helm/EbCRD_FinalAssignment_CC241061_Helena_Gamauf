using System.Collections;
using UnityEngine;

public class AutomaticDoor1 : MonoBehaviour
{
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 120f;
    [SerializeField] private float closeDelay = 4f;
    [SerializeField] private bool autoClose = true;

    private float closedEulerX;
    private float closedEulerY;
    private float closedEulerZ;
    private float openEulerY;
    private float oppositeOpenEulerY;
    private bool isOpen;
    private Coroutine moveRoutine;

    public bool IsMoving => moveRoutine != null;

    private void Awake()
    {
        CacheAngles();
    }

    private void CacheAngles()
    {
        Vector3 euler = transform.eulerAngles;
        closedEulerX = euler.x;
        closedEulerY = euler.y;
        closedEulerZ = euler.z;
        openEulerY = closedEulerY + openAngle;
        oppositeOpenEulerY = closedEulerY - openAngle;
    }

    public void Configure(float angle, bool shouldAutoClose)
    {
        openAngle = angle;
        autoClose = shouldAutoClose;
        CacheAngles();
    }

    public float GetEulerYAtOpenAmount(float amount)
    {
        amount = Mathf.Clamp01(amount);
        return Mathf.LerpAngle(closedEulerY, openEulerY, amount);
    }

    public IEnumerator RotateToOpenAmount(float amount)
    {
        var targetY = GetEulerYAtOpenAmount(amount);
        isOpen = amount >= 0.99f;
        yield return RotateToY(targetY);
    }

    public IEnumerator RotateFullyOpen() => RotateToOpenAmount(1f);

    public IEnumerator RotateFullyClosed() => RotateToOpenAmount(0f);

    public void BeginRotateFullyClosed()
    {
        isOpen = false;
        BeginRotateToY(closedEulerY);
    }

    public void Open()
    {
        Open(false);
    }

    public void OpenOpposite()
    {
        Open(true);
    }

    void Open(bool reverseDirection)
    {
        if (isOpen)
            return;

        isOpen = true;
        StartMove(reverseDirection ? oppositeOpenEulerY : openEulerY);

        if (autoClose)
            Invoke(nameof(Close), closeDelay);
    }

    public void Close()
    {
        CancelInvoke(nameof(Close));

        if (!isOpen)
            return;

        isOpen = false;
        StartMove(closedEulerY);
    }

    public void SnapClosed()
    {
        CancelInvoke(nameof(Close));

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isOpen = false;
        transform.eulerAngles = new Vector3(closedEulerX, closedEulerY, closedEulerZ);
    }

    public IEnumerator RotateToY(float targetY)
    {
        isOpen = IsAtOpenPosition(targetY);
        StartMove(targetY);
        yield return WaitUntilIdle();
    }

    public void BeginRotateToY(float targetY)
    {
        isOpen = IsAtOpenPosition(targetY);
        StartMove(targetY);
    }

    bool IsAtOpenPosition(float y) =>
        Mathf.Abs(Mathf.DeltaAngle(y, openEulerY)) < 0.5f
        || Mathf.Abs(Mathf.DeltaAngle(y, oppositeOpenEulerY)) < 0.5f;

    private void StartMove(float targetY)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(RotateOnYAxis(targetY));
    }

    private IEnumerator RotateOnYAxis(float targetY)
    {
        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetY)) > 0.5f)
        {
            float newY = Mathf.MoveTowardsAngle(
                transform.eulerAngles.y,
                targetY,
                openSpeed * Time.deltaTime
            );
            transform.eulerAngles = new Vector3(closedEulerX, newY, closedEulerZ);
            yield return null;
        }

        transform.eulerAngles = new Vector3(closedEulerX, targetY, closedEulerZ);
        moveRoutine = null;
    }

    public IEnumerator WaitUntilIdle()
    {
        while (moveRoutine != null)
            yield return null;
    }
}
