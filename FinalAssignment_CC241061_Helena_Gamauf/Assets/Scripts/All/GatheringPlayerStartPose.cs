using UnityEngine;

public static class GatheringPlayerStartPose
{
    public static bool HasPose { get; private set; }
    public static Vector3 Position { get; private set; }
    public static Quaternion Rotation { get; private set; }

    public static void Record(Transform player)
    {
        if (player == null)
            return;

        Position = player.position;
        Rotation = player.rotation;
        HasPose = true;
    }

    public static void Reset()
    {
        HasPose = false;
    }
}
