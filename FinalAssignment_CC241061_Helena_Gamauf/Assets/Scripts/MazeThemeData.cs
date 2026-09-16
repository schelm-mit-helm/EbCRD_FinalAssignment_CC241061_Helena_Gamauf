using UnityEngine;

public enum MazeThemeEnvironmentEffect
{
    None = 0,
    SlipperyFloor = 1,
    Fog = 2,
}

[CreateAssetMenu(fileName = "Maze Theme", menuName = "Maze/Maze Theme")]
public class MazeThemeData : ScriptableObject
{
    public string themeName;

    [Header("Environment")]
    public MazeThemeEnvironmentEffect environmentEffect = MazeThemeEnvironmentEffect.None;

    [Header("Maze")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject ceilingPrefab;
    public GameObject lightPrefab;

    [Header("Items")]
    public GameObject[] itemBundlePrefabs;
    public Item[] customerOrderItems;

    [Header("Enemies")]
    public GameObject[] enemyPrefabs;

    [Header("Room")]
    public GameObject roomPrefab;
    public Vector3 roomPrefabEulerRotation;
    public Vector3 roomPrefabPositionOffset;
}