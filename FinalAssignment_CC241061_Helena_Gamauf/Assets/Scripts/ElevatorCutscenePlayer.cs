using System;
using System.Collections;
using UnityEngine;

public static class ElevatorCutscenePlayer
{
    public static Transform ResolveCameraTransform(Transform player, PlayerMovement playerMovement)
    {
        if (playerMovement != null && playerMovement.CameraTransform != null)
            return playerMovement.CameraTransform;

        var childCamera = player.GetComponentInChildren<Camera>(true);
        if (childCamera != null)
            return childCamera.transform;

        return Camera.main != null ? Camera.main.transform : null;
    }

    public static Transform ResolveElevatorCameraPoint(Transform elevatorCameraPointOverride)
    {
        if (elevatorCameraPointOverride != null)
            return elevatorCameraPointOverride;

        foreach (var objectName in new[] { "elevator middle", "Elevator Middle", "ElevatorMiddle", "ElevatorCameraPoint" })
        {
            var found = SceneTransformFinder.FindRecursiveIgnoreCase(objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    public static void PreservePlayerHeight(Transform player, float y)
    {
        var position = player.position;
        if (Mathf.Approximately(position.y, y))
            return;

        player.position = new Vector3(position.x, y, position.z);
    }

    public static IEnumerator RotateYawToLookDirection(
        Transform target,
        Vector3 direction,
        float rotateSpeed)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            yield break;

        var startRotation = target.rotation;
        var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        var angle = Quaternion.Angle(startRotation, targetRotation);
        var duration = angle / Mathf.Max(rotateSpeed, 1f);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            target.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        target.rotation = targetRotation;
    }

    public static IEnumerator RotateCameraToLookDirection(
        Transform cameraTransform,
        Vector3 direction,
        float rotateSpeed,
        float pitchDown = 0f)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            yield break;

        var startRotation = cameraTransform.rotation;
        var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up)
            * Quaternion.Euler(pitchDown, 0f, 0f);
        var angle = Quaternion.Angle(startRotation, targetRotation);
        var duration = angle / Mathf.Max(rotateSpeed, 1f);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        cameraTransform.rotation = targetRotation;
    }

    public static IEnumerator MoveAlongXAxis(
        Transform player,
        Transform cameraTransform,
        float targetX,
        float preservedPlayerY,
        float preservedPlayerZ,
        float preservedCameraY,
        float preservedCameraZ,
        float moveSpeed,
        Action<float, float, float> onPositionUpdated = null)
    {
        var startX = cameraTransform.position.x;
        var targetPosition = new Vector3(targetX, preservedCameraY, preservedCameraZ);

        while (Mathf.Abs(cameraTransform.position.x - targetX) > 0.02f)
        {
            var next = Vector3.MoveTowards(
                cameraTransform.position,
                targetPosition,
                moveSpeed * Time.deltaTime);
            cameraTransform.position = new Vector3(next.x, preservedCameraY, preservedCameraZ);
            player.position = new Vector3(next.x, preservedPlayerY, preservedPlayerZ);
            onPositionUpdated?.Invoke(next.x, startX, targetX);
            yield return null;
        }

        cameraTransform.position = targetPosition;
        player.position = new Vector3(targetX, preservedPlayerY, preservedPlayerZ);
        onPositionUpdated?.Invoke(targetX, startX, targetX);
    }

    public static bool HasPassedTriggerX(float currentX, float startX, float targetX, float triggerX)
    {
        if (Mathf.Approximately(startX, targetX))
            return Mathf.Abs(currentX - triggerX) <= 0.02f;

        var minX = Mathf.Min(startX, targetX);
        var maxX = Mathf.Max(startX, targetX);

        if (triggerX >= minX && triggerX <= maxX)
            return targetX > startX ? currentX >= triggerX : currentX <= triggerX;

        if (targetX > startX)
            return triggerX > startX && currentX >= triggerX;

        return triggerX < startX && currentX <= triggerX;
    }
}
