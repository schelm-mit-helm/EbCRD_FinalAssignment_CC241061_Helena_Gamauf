using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class MazeRenderer : MonoBehaviour
{
    public enum EntranceSide
    {
        None,
        Left,
        Right,
        Bottom,
        Top
    }

    enum WallDirection
    {
        Left,
        Right,
        Bottom,
        Top
    }

    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Theme")]
    [SerializeField] private MazeThemeData pantryDefaultTheme;

    [Header("Spawn Settings")]
    [SerializeField] private int enemyCount = 5;
    [SerializeField] private float enemySpawnYOffset = 0f;
    [SerializeField] private float itemSpawnYOffset = 0.3f;
    [SerializeField] private Vector3 itemBundleEulerRotation = Vector3.zero;
    [SerializeField] private float itemWallDistanceFromCenter = 1.14f;
    [SerializeField] private float minWalkwayClearance = 0.55f;

    [Header("Ceiling And Lighting")]
    [SerializeField] private bool generateCeiling = true;
    [SerializeField] private bool generateLights = true;
    [SerializeField] private float ceilingYOffset = 0.05f;
    [SerializeField] private float lightYOffset = -0.25f;
    [SerializeField] private int lightSpacingInCells = 2;

    [Header("Path Validation")]
    [SerializeField] private bool validatePathsAfterGeneration = true;
    [SerializeField] private int maxGenerationAttempts = 25;

    [Header("Theme Room")]
    [SerializeField] private bool spawnThemeRoom = true;
    [SerializeField] private int roomWidthInCells = 2;
    [SerializeField] private int roomHeightInCells = 2;
    [SerializeField] private float roomSpawnYOffset = 0f;

    [Header("Maze Entrance")]
    [SerializeField] private EntranceSide entranceSide = EntranceSide.Left;
    [SerializeField] private int entranceCellX = 0;
    [SerializeField] private int entranceCellY = 1;
    [SerializeField] private float entranceOpeningWidth = 1.5f;

    public float CellSize = 3f;
    public float WallHeight = 2f;
    public float WallThickness = 0.2f;

    private MazeThemeData theme;
    private MazeCell[,] maze;
    private readonly HashSet<Vector2Int> usedSpawnCells = new HashSet<Vector2Int>();

    //🏢
    struct ItemSpawnPlan
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;
        public Vector2Int cell;
    }

    readonly List<ItemSpawnPlan> plannedItemSpawns = new List<ItemSpawnPlan>();

    private int roomStartX = -1;
    private int roomStartY = -1;

    private struct RoomDoorOpening
    {
        public int x;
        public int y;
        public WallDirection side;
    }

    private void Start()
    {
        theme = MazeThemeManager.GetCurrentOrDefault(pantryDefaultTheme);

        if (theme == null)
        {
            Debug.LogError("MazeRenderer: No MazeThemeData found. Assign Pantry Default Theme on MazeRenderer.");
            return;
        }

        MazeDifficultyService difficultyService = MazeDifficultyService.EnsurePersistentInstance();

        mazeGenerator.mazeWidth = difficultyService.CurrentMazeSize;
        mazeGenerator.mazeHeight = difficultyService.CurrentMazeSize;
        enemyCount = difficultyService.CurrentEnemyCount;

        entranceCellX = Mathf.Clamp(entranceCellX, 0, mazeGenerator.mazeWidth - 1);
        entranceCellY = Mathf.Clamp(entranceCellY, 0, mazeGenerator.mazeHeight - 1);
        entranceOpeningWidth = Mathf.Clamp(entranceOpeningWidth, 0.5f, CellSize - 0.1f);

        bool mazeReady = false;

        for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
        {
            if (attempt > 0)
            {
                ClearGeneratedContent();
                MazeThemeManager.BumpMazeLayoutSeed();
            }

            ResetGenerationState();

            maze = mazeGenerator.GetMaze();
            PrepareThemeRoom();

            if (!IsEntireMazeAccessible())
                continue;

            BuildMazeGeometry();

            if (navMeshSurface != null)
            {
                ConfigureNavMeshSurface();
                navMeshSurface.BuildNavMesh();
            }

            if (validatePathsAfterGeneration && !ValidateMazePathsSilent())
                continue;

            TryPlanItemSpawns();

            mazeReady = true;
            break;
        }

        if (!mazeReady)
        {
            Debug.LogWarning(
                "MazeRenderer: Failed to generate an accessible maze after "
                + maxGenerationAttempts
                + " attempts. Using last layout."
            );
        }

        SpawnItems();
        SpawnEnemies();
    }
    private void ResetGenerationState()
    {
        usedSpawnCells.Clear();
        roomStartX = -1;
        roomStartY = -1;
        plannedItemSpawns.Clear();
    }

    private void ClearGeneratedContent()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    void ConfigureNavMeshSurface()
    {
        navMeshSurface.collectObjects = CollectObjects.Children;
    }

    private void BuildMazeGeometry()
    {
        GenerateFloor();
        GenerateHorizontalWalls();
        GenerateVerticalWalls();
        GenerateSmallEntranceWallPieces();
        SpawnThemeRoomPrefab();
        GenerateCeiling();
        GenerateLights();
    }

    private void PrepareThemeRoom()
    {
        if (!spawnThemeRoom || theme.roomPrefab == null)
            return;

        int width = mazeGenerator.mazeWidth;
        int height = mazeGenerator.mazeHeight;

        roomWidthInCells = Mathf.Clamp(roomWidthInCells, 1, width);
        roomHeightInCells = Mathf.Clamp(roomHeightInCells, 1, height);

        for (int attempt = 0; attempt < 100; attempt++)
        {
            int x = Random.Range(0, width - roomWidthInCells + 1);
            int y = Random.Range(0, height - roomHeightInCells + 1);

            if (RoomTouchesMazeEntrance(x, y))
                continue;

            roomStartX = x;
            roomStartY = y;

            List<RoomDoorOpening> existingDoors = CollectExistingRoomDoors();

            CloseRoomPerimeter();
            OpenRoomInterior();

            if (!OpenMinimumRoomDoors(existingDoors))
                continue;

            ReserveRoomCells();

            return;
        }

        roomStartX = -1;
        roomStartY = -1;
    }

    private bool RoomTouchesMazeEntrance(int x, int y)
    {
        for (int roomX = x; roomX < x + roomWidthInCells; roomX++)
        {
            for (int roomY = y; roomY < y + roomHeightInCells; roomY++)
            {
                if (IsEntranceCell(new Vector2Int(roomX, roomY)))
                    return true;
            }
        }

        return false;
    }

    private void CloseRoomPerimeter()
    {
        int width = mazeGenerator.mazeWidth;
        int height = mazeGenerator.mazeHeight;

        for (int y = roomStartY; y < roomStartY + roomHeightInCells; y++)
        {
            if (roomStartX > 0)
                maze[roomStartX, y].leftWall = true;

            int rightX = roomStartX + roomWidthInCells;

            if (rightX < width)
                maze[rightX, y].leftWall = true;
        }

        for (int x = roomStartX; x < roomStartX + roomWidthInCells; x++)
        {
            if (roomStartY > 0)
                maze[x, roomStartY - 1].topWall = true;

            int topY = roomStartY + roomHeightInCells - 1;

            if (topY >= 0 && topY < height)
                maze[x, topY].topWall = true;
        }
    }

    private void OpenRoomInterior()
    {
        for (int y = roomStartY; y < roomStartY + roomHeightInCells; y++)
        {
            for (int x = roomStartX + 1; x < roomStartX + roomWidthInCells; x++)
            {
                maze[x, y].leftWall = false;
            }
        }

        for (int y = roomStartY; y < roomStartY + roomHeightInCells - 1; y++)
        {
            for (int x = roomStartX; x < roomStartX + roomWidthInCells; x++)
            {
                maze[x, y].topWall = false;
            }
        }
    }

    private List<RoomDoorOpening> CollectExistingRoomDoors()
    {
        List<RoomDoorOpening> doors = new List<RoomDoorOpening>();

        if (roomStartX > 0)
        {
            for (int y = roomStartY; y < roomStartY + roomHeightInCells; y++)
            {
                if (!maze[roomStartX, y].leftWall)
                {
                    doors.Add(new RoomDoorOpening
                    {
                        x = roomStartX,
                        y = y,
                        side = WallDirection.Left
                    });
                }
            }
        }

        if (roomStartX + roomWidthInCells < mazeGenerator.mazeWidth)
        {
            int rightX = roomStartX + roomWidthInCells;

            for (int y = roomStartY; y < roomStartY + roomHeightInCells; y++)
            {
                if (!maze[rightX, y].leftWall)
                {
                    doors.Add(new RoomDoorOpening
                    {
                        x = rightX,
                        y = y,
                        side = WallDirection.Right
                    });
                }
            }
        }

        if (roomStartY > 0)
        {
            for (int x = roomStartX; x < roomStartX + roomWidthInCells; x++)
            {
                if (!maze[x, roomStartY - 1].topWall)
                {
                    doors.Add(new RoomDoorOpening
                    {
                        x = x,
                        y = roomStartY - 1,
                        side = WallDirection.Bottom
                    });
                }
            }
        }

        if (roomStartY + roomHeightInCells < mazeGenerator.mazeHeight)
        {
            int topY = roomStartY + roomHeightInCells - 1;

            for (int x = roomStartX; x < roomStartX + roomWidthInCells; x++)
            {
                if (!maze[x, topY].topWall)
                {
                    doors.Add(new RoomDoorOpening
                    {
                        x = x,
                        y = topY,
                        side = WallDirection.Top
                    });
                }
            }
        }

        return doors;
    }

    private List<RoomDoorOpening> CollectAllPossibleRoomDoors()
    {
        List<RoomDoorOpening> doors = new List<RoomDoorOpening>();

        if (roomStartX > 0)
        {
            for (int y = roomStartY; y < roomStartY + roomHeightInCells; y++)
            {
                doors.Add(new RoomDoorOpening
                {
                    x = roomStartX,
                    y = y,
                    side = WallDirection.Left
                });
            }
        }

        if (roomStartX + roomWidthInCells < mazeGenerator.mazeWidth)
        {
            int rightX = roomStartX + roomWidthInCells;

            for (int y = roomStartY; y < roomStartY + roomHeightInCells; y++)
            {
                doors.Add(new RoomDoorOpening
                {
                    x = rightX,
                    y = y,
                    side = WallDirection.Right
                });
            }
        }

        if (roomStartY > 0)
        {
            for (int x = roomStartX; x < roomStartX + roomWidthInCells; x++)
            {
                doors.Add(new RoomDoorOpening
                {
                    x = x,
                    y = roomStartY - 1,
                    side = WallDirection.Bottom
                });
            }
        }

        if (roomStartY + roomHeightInCells < mazeGenerator.mazeHeight)
        {
            int topY = roomStartY + roomHeightInCells - 1;

            for (int x = roomStartX; x < roomStartX + roomWidthInCells; x++)
            {
                doors.Add(new RoomDoorOpening
                {
                    x = x,
                    y = topY,
                    side = WallDirection.Top
                });
            }
        }

        return doors;
    }

    private List<RoomDoorOpening> OrderDoorCandidates(
        List<RoomDoorOpening> preferredDoors,
        List<RoomDoorOpening> allDoors)
    {
        HashSet<(int x, int y, WallDirection side)> preferred = new HashSet<(int x, int y, WallDirection side)>();

        for (int i = 0; i < preferredDoors.Count; i++)
        {
            RoomDoorOpening door = preferredDoors[i];
            preferred.Add((door.x, door.y, door.side));
        }

        List<RoomDoorOpening> ordered = new List<RoomDoorOpening>(allDoors.Count);

        for (int i = 0; i < allDoors.Count; i++)
        {
            RoomDoorOpening door = allDoors[i];

            if (preferred.Contains((door.x, door.y, door.side)))
                ordered.Add(door);
        }

        for (int i = 0; i < allDoors.Count; i++)
        {
            RoomDoorOpening door = allDoors[i];

            if (!preferred.Contains((door.x, door.y, door.side)))
                ordered.Add(door);
        }

        return ordered;
    }

    private bool OpenMinimumRoomDoors(List<RoomDoorOpening> existingDoors)
    {
        List<RoomDoorOpening> allCandidates = CollectAllPossibleRoomDoors();

        if (allCandidates.Count == 0)
            return false;

        List<RoomDoorOpening> orderedCandidates = OrderDoorCandidates(existingDoors, allCandidates);
        List<RoomDoorOpening> shuffledSameSize = new List<RoomDoorOpening>(orderedCandidates);

        for (int doorCount = 1; doorCount <= orderedCandidates.Count; doorCount++)
        {
            if (doorCount > 1)
                shuffledSameSize = new List<RoomDoorOpening>(orderedCandidates);

            ShuffleDoorCandidates(shuffledSameSize);

            if (TryDoorCombinationOfSize(shuffledSameSize, doorCount, out List<RoomDoorOpening> chosenDoors))
            {
                ApplyRoomDoors(chosenDoors);
                return true;
            }
        }

        return false;
    }

    private void ShuffleDoorCandidates(List<RoomDoorOpening> candidates)
    {
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            RoomDoorOpening temp = candidates[i];
            candidates[i] = candidates[swapIndex];
            candidates[swapIndex] = temp;
        }
    }

    private bool TryDoorCombinationOfSize(
        List<RoomDoorOpening> candidates,
        int doorCount,
        out List<RoomDoorOpening> chosenDoors)
    {
        chosenDoors = null;

        if (doorCount > candidates.Count)
            return false;

        int[] indices = new int[doorCount];

        for (int i = 0; i < doorCount; i++)
            indices[i] = i;

        while (true)
        {
            List<RoomDoorOpening> trialDoors = new List<RoomDoorOpening>(doorCount);

            for (int i = 0; i < doorCount; i++)
                trialDoors.Add(candidates[indices[i]]);

            ResetRoomPerimeterDoors();
            ApplyRoomDoors(trialDoors);

            if (IsEntireMazeAccessible())
            {
                chosenDoors = trialDoors;
                return true;
            }

            if (!TryAdvanceCombination(indices, candidates.Count))
                return false;
        }
    }

    private bool TryAdvanceCombination(int[] indices, int candidateCount)
    {
        int doorCount = indices.Length;

        for (int i = doorCount - 1; i >= 0; i--)
        {
            int maxIndex = candidateCount - doorCount + i;

            if (indices[i] < maxIndex)
            {
                indices[i]++;

                for (int j = i + 1; j < doorCount; j++)
                    indices[j] = indices[j - 1] + 1;

                return true;
            }
        }

        return false;
    }

    private void ResetRoomPerimeterDoors()
    {
        CloseRoomPerimeter();
        OpenRoomInterior();
    }

    private void ApplyRoomDoors(List<RoomDoorOpening> doors)
    {
        for (int i = 0; i < doors.Count; i++)
            OpenRoomDoor(doors[i]);
    }

    private void OpenRoomDoor(RoomDoorOpening door)
    {
        if (door.side == WallDirection.Left)
            maze[door.x, door.y].leftWall = false;
        else if (door.side == WallDirection.Right)
            maze[door.x, door.y].leftWall = false;
        else if (door.side == WallDirection.Bottom)
            maze[door.x, door.y].topWall = false;
        else if (door.side == WallDirection.Top)
            maze[door.x, door.y].topWall = false;
    }

    private void ReserveRoomCells()
    {
        for (int x = roomStartX; x < roomStartX + roomWidthInCells; x++)
        {
            for (int y = roomStartY; y < roomStartY + roomHeightInCells; y++)
            {
                usedSpawnCells.Add(new Vector2Int(x, y));
            }
        }
    }

    private void SpawnThemeRoomPrefab()
    {
        if (!spawnThemeRoom || theme.roomPrefab == null)
            return;

        if (roomStartX < 0 || roomStartY < 0)
            return;

        Vector3 roomCenter = new Vector3(
            (roomStartX + (roomWidthInCells - 1) / 2f) * CellSize,
            roomSpawnYOffset,
            (roomStartY + (roomHeightInCells - 1) / 2f) * CellSize
        );

        roomCenter += theme.roomPrefabPositionOffset;

        Instantiate(
            theme.roomPrefab,
            roomCenter,
            Quaternion.Euler(theme.roomPrefabEulerRotation),
            transform
        );
    }

    private void GenerateFloor()
    {
        if (theme.floorPrefab == null)
            return;

        GameObject floor = Instantiate(
            theme.floorPrefab,
            new Vector3(
                (mazeGenerator.mazeWidth - 1) * CellSize / 2f,
                -0.05f,
                (mazeGenerator.mazeHeight - 1) * CellSize / 2f
            ),
            Quaternion.identity,
            transform
        );

        floor.transform.localScale = new Vector3(
            mazeGenerator.mazeWidth * CellSize,
            0.1f,
            mazeGenerator.mazeHeight * CellSize
        );
    }

    private void GenerateCeiling()
    {
        if (!generateCeiling || theme.ceilingPrefab == null)
            return;

        GameObject ceiling = Instantiate(
            theme.ceilingPrefab,
            new Vector3(
                (mazeGenerator.mazeWidth - 1) * CellSize / 2f,
                WallHeight + ceilingYOffset,
                (mazeGenerator.mazeHeight - 1) * CellSize / 2f
            ),
            Quaternion.identity,
            transform
        );

        ceiling.transform.localScale = new Vector3(
            mazeGenerator.mazeWidth * CellSize,
            0.1f,
            mazeGenerator.mazeHeight * CellSize
        );
    }

    private void GenerateLights()
    {
        if (!generateLights || theme.lightPrefab == null)
            return;

        int spacing = Mathf.Max(1, lightSpacingInCells);

        for (int x = 0; x < mazeGenerator.mazeWidth; x += spacing)
        {
            for (int y = 0; y < mazeGenerator.mazeHeight; y += spacing)
            {
                Vector3 position = new Vector3(
                    x * CellSize,
                    WallHeight + lightYOffset,
                    y * CellSize
                );

                Instantiate(theme.lightPrefab, position, Quaternion.identity, transform);
            }
        }
    }

    private void GenerateHorizontalWalls()
    {
        int width = mazeGenerator.mazeWidth;
        int height = mazeGenerator.mazeHeight;

        for (int y = 0; y <= height; y++)
        {
            int runStart = -1;

            for (int x = 0; x < width; x++)
            {
                bool hasWall = HasHorizontalWall(x, y);

                if (hasWall && runStart == -1)
                    runStart = x;

                bool endRun = runStart != -1 && (!hasWall || x == width - 1);

                if (endRun)
                {
                    int runEnd = hasWall && x == width - 1 ? x : x - 1;
                    CreateHorizontalWall(runStart, runEnd, y);
                    runStart = -1;
                }
            }
        }
    }

    private void GenerateVerticalWalls()
    {
        int width = mazeGenerator.mazeWidth;
        int height = mazeGenerator.mazeHeight;

        for (int x = 0; x <= width; x++)
        {
            int runStart = -1;

            for (int y = 0; y < height; y++)
            {
                bool hasWall = HasVerticalWall(x, y);

                if (hasWall && runStart == -1)
                    runStart = y;

                bool endRun = runStart != -1 && (!hasWall || y == height - 1);

                if (endRun)
                {
                    int runEnd = hasWall && y == height - 1 ? y : y - 1;
                    CreateVerticalWall(x, runStart, runEnd);
                    runStart = -1;
                }
            }
        }
    }

    private bool HasHorizontalWall(int x, int y)
    {
        if (entranceSide == EntranceSide.Bottom && y == 0 && x == entranceCellX)
            return false;

        if (entranceSide == EntranceSide.Top && y == mazeGenerator.mazeHeight && x == entranceCellX)
            return false;

        if (y == 0)
            return true;

        if (y == mazeGenerator.mazeHeight)
            return maze[x, y - 1].topWall;

        return maze[x, y - 1].topWall;
    }

    private bool HasVerticalWall(int x, int y)
    {
        if (entranceSide == EntranceSide.Left && x == 0 && y == entranceCellY)
            return false;

        if (entranceSide == EntranceSide.Right && x == mazeGenerator.mazeWidth && y == entranceCellY)
            return false;

        if (x == 0)
            return true;

        if (x == mazeGenerator.mazeWidth)
            return true;

        return maze[x, y].leftWall;
    }

    private void CreateHorizontalWall(int startX, int endX, int y)
    {
        if (theme.wallPrefab == null)
            return;

        float length = (endX - startX + 1) * CellSize;

        Vector3 position = new Vector3(
            (startX + endX) * CellSize / 2f,
            WallHeight / 2f,
            y * CellSize - CellSize / 2f
        );

        GameObject wall = Instantiate(theme.wallPrefab, position, Quaternion.identity, transform);

        wall.transform.localScale = new Vector3(
            length + WallThickness,
            WallHeight,
            WallThickness
        );
    }

    private void CreateVerticalWall(int x, int startY, int endY)
    {
        if (theme.wallPrefab == null)
            return;

        float length = (endY - startY + 1) * CellSize;

        Vector3 position = new Vector3(
            x * CellSize - CellSize / 2f,
            WallHeight / 2f,
            (startY + endY) * CellSize / 2f
        );

        GameObject wall = Instantiate(theme.wallPrefab, position, Quaternion.identity, transform);

        wall.transform.localScale = new Vector3(
            WallThickness,
            WallHeight,
            length + WallThickness
        );
    }

    private void GenerateSmallEntranceWallPieces()
    {
        if (entranceSide == EntranceSide.None || theme.wallPrefab == null)
            return;

        float remainingWallLength = (CellSize - entranceOpeningWidth) / 2f;

        if (remainingWallLength <= 0f)
            return;

        if (entranceSide == EntranceSide.Left)
        {
            float x = -CellSize / 2f;
            float z = entranceCellY * CellSize;

            CreatePartialVerticalWall(new Vector3(x, WallHeight / 2f, z - entranceOpeningWidth / 2f - remainingWallLength / 2f), remainingWallLength);
            CreatePartialVerticalWall(new Vector3(x, WallHeight / 2f, z + entranceOpeningWidth / 2f + remainingWallLength / 2f), remainingWallLength);
        }
        else if (entranceSide == EntranceSide.Right)
        {
            float x = mazeGenerator.mazeWidth * CellSize - CellSize / 2f;
            float z = entranceCellY * CellSize;

            CreatePartialVerticalWall(new Vector3(x, WallHeight / 2f, z - entranceOpeningWidth / 2f - remainingWallLength / 2f), remainingWallLength);
            CreatePartialVerticalWall(new Vector3(x, WallHeight / 2f, z + entranceOpeningWidth / 2f + remainingWallLength / 2f), remainingWallLength);
        }
        else if (entranceSide == EntranceSide.Bottom)
        {
            float x = entranceCellX * CellSize;
            float z = -CellSize / 2f;

            CreatePartialHorizontalWall(new Vector3(x - entranceOpeningWidth / 2f - remainingWallLength / 2f, WallHeight / 2f, z), remainingWallLength);
            CreatePartialHorizontalWall(new Vector3(x + entranceOpeningWidth / 2f + remainingWallLength / 2f, WallHeight / 2f, z), remainingWallLength);
        }
        else if (entranceSide == EntranceSide.Top)
        {
            float x = entranceCellX * CellSize;
            float z = mazeGenerator.mazeHeight * CellSize - CellSize / 2f;

            CreatePartialHorizontalWall(new Vector3(x - entranceOpeningWidth / 2f - remainingWallLength / 2f, WallHeight / 2f, z), remainingWallLength);
            CreatePartialHorizontalWall(new Vector3(x + entranceOpeningWidth / 2f + remainingWallLength / 2f, WallHeight / 2f, z), remainingWallLength);
        }
    }

    private void CreatePartialVerticalWall(Vector3 position, float length)
    {
        GameObject wall = Instantiate(theme.wallPrefab, position, Quaternion.identity, transform);
        wall.transform.localScale = new Vector3(WallThickness, WallHeight, length + WallThickness);
    }

    private void CreatePartialHorizontalWall(Vector3 position, float length)
    {
        GameObject wall = Instantiate(theme.wallPrefab, position, Quaternion.identity, transform);
        wall.transform.localScale = new Vector3(length + WallThickness, WallHeight, WallThickness);
    }

    private bool IsEntireMazeAccessible()
    {
        Vector2Int startCell = GetEntranceCell();
        HashSet<Vector2Int> reachable = FloodFillReachableCells(startCell);

        for (int x = 0; x < mazeGenerator.mazeWidth; x++)
        {
            for (int y = 0; y < mazeGenerator.mazeHeight; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (!reachable.Contains(cell))
                    return false;
            }
        }

        return true;
    }

    private Vector2Int GetEntranceCell()
    {
        if (entranceSide == EntranceSide.Left)
            return new Vector2Int(0, entranceCellY);

        if (entranceSide == EntranceSide.Right)
            return new Vector2Int(mazeGenerator.mazeWidth - 1, entranceCellY);

        if (entranceSide == EntranceSide.Bottom)
            return new Vector2Int(entranceCellX, 0);

        if (entranceSide == EntranceSide.Top)
            return new Vector2Int(entranceCellX, mazeGenerator.mazeHeight - 1);

        return new Vector2Int(mazeGenerator.startX, mazeGenerator.startY);
    }

    private HashSet<Vector2Int> FloodFillReachableCells(Vector2Int startCell)
    {
        HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        reachable.Add(startCell);
        queue.Enqueue(startCell);

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            TryEnqueueNeighbor(cell, new Vector2Int(cell.x - 1, cell.y), WallDirection.Left, reachable, queue);
            TryEnqueueNeighbor(cell, new Vector2Int(cell.x + 1, cell.y), WallDirection.Right, reachable, queue);
            TryEnqueueNeighbor(cell, new Vector2Int(cell.x, cell.y - 1), WallDirection.Bottom, reachable, queue);
            TryEnqueueNeighbor(cell, new Vector2Int(cell.x, cell.y + 1), WallDirection.Top, reachable, queue);
        }

        return reachable;
    }

    private void TryEnqueueNeighbor(
        Vector2Int fromCell,
        Vector2Int neighborCell,
        WallDirection directionFromCurrent,
        HashSet<Vector2Int> reachable,
        Queue<Vector2Int> queue)
    {
        if (neighborCell.x < 0 || neighborCell.y < 0)
            return;

        if (neighborCell.x >= mazeGenerator.mazeWidth || neighborCell.y >= mazeGenerator.mazeHeight)
            return;

        if (reachable.Contains(neighborCell))
            return;

        if (HasWallBetweenCells(fromCell, directionFromCurrent))
            return;

        reachable.Add(neighborCell);
        queue.Enqueue(neighborCell);
    }

    private bool HasWallBetweenCells(Vector2Int fromCell, WallDirection direction)
    {
        if (direction == WallDirection.Left)
            return HasWallOnLeftOfCell(fromCell.x, fromCell.y);

        if (direction == WallDirection.Right)
            return HasWallOnRightOfCell(fromCell.x, fromCell.y);

        if (direction == WallDirection.Bottom)
            return HasWallOnBottomOfCell(fromCell.x, fromCell.y);

        return HasWallOnTopOfCell(fromCell.x, fromCell.y);
    }

    private void TryPlanItemSpawns()
    {
        plannedItemSpawns.Clear();

        if (theme.itemBundlePrefabs == null || theme.itemBundlePrefabs.Length == 0)
            return;

        HashSet<Vector2Int> reservedCells = new HashSet<Vector2Int>(usedSpawnCells);

        for (int i = 0; i < theme.itemBundlePrefabs.Length; i++)
        {
            GameObject itemPrefab = theme.itemBundlePrefabs[i];

            if (itemPrefab == null)
                continue;

            if (!TryFindItemSpawnPosition(
                    reservedCells,
                    itemPrefab,
                    out Vector3 spawnPosition,
                    out Vector2Int cell,
                    out Quaternion rotation))
                continue;

            reservedCells.Add(cell);
            plannedItemSpawns.Add(new ItemSpawnPlan
            {
                prefab = itemPrefab,
                position = spawnPosition,
                rotation = rotation,
                cell = cell
            });
        }

        if (plannedItemSpawns.Count == 0 && theme.itemBundlePrefabs.Length > 0)
        {
            Debug.LogWarning(
                "MazeRenderer: Could not plan any item batch spawns. "
                + "Try lowering Min Walkway Clearance on MazeRenderer."
            );
        }
    }

    private void ValidateMazePaths()
    {
        if (!ValidateMazePathsSilent())
            Debug.LogWarning("MazeRenderer: Blocked path found during validation.");
    }

    private bool ValidateMazePathsSilent()
    {
        if (navMeshSurface == null)
            return true;

        Vector3 startPosition = GetCellCenter(GetEntranceCell().x, GetEntranceCell().y);

        for (int x = 0; x < mazeGenerator.mazeWidth; x++)
        {
            for (int y = 0; y < mazeGenerator.mazeHeight; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (usedSpawnCells.Contains(cell))
                    continue;

                if (!IsPathAvailable(startPosition, GetCellCenter(x, y)))
                    return false;
            }
        }

        return true;
    }

    private bool IsPathAvailable(Vector3 from, Vector3 to)
    {
        if (!NavMesh.SamplePosition(from, out NavMeshHit fromHit, 2f, NavMesh.AllAreas))
            return false;

        if (!NavMesh.SamplePosition(to, out NavMeshHit toHit, 2f, NavMesh.AllAreas))
            return false;

        NavMeshPath path = new NavMeshPath();

        if (!NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, path))
            return false;

        return path.status == NavMeshPathStatus.PathComplete;
    }

    private void SpawnItems()
    {
        if (plannedItemSpawns.Count > 0)
        {
            for (int i = 0; i < plannedItemSpawns.Count; i++)
            {
                ItemSpawnPlan plan = plannedItemSpawns[i];
                usedSpawnCells.Add(plan.cell);
                Instantiate(plan.prefab, plan.position, plan.rotation, transform);
            }

            return;
        }

        if (theme.itemBundlePrefabs == null || theme.itemBundlePrefabs.Length == 0)
            return;

        int spawnedCount = 0;

        for (int i = 0; i < theme.itemBundlePrefabs.Length; i++)
        {
            GameObject itemPrefab = theme.itemBundlePrefabs[i];

            if (itemPrefab == null)
                continue;

            if (TryFindItemSpawnPosition(
                    usedSpawnCells,
                    itemPrefab,
                    out Vector3 spawnPosition,
                    out Vector2Int cell,
                    out Quaternion itemRotation))
            {
                usedSpawnCells.Add(cell);
                Instantiate(itemPrefab, spawnPosition, itemRotation, transform);
                spawnedCount++;
            }
        }

        if (spawnedCount == 0)
        {
            Debug.LogWarning(
                "MazeRenderer: Could not place any item batches in the maze. "
                + "Try lowering Min Walkway Clearance on MazeRenderer."
            );
        }
    }

    private void SpawnEnemies()
    {
        if (theme.enemyPrefabs == null || theme.enemyPrefabs.Length == 0)
            return;

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemyPrefab = theme.enemyPrefabs[Random.Range(0, theme.enemyPrefabs.Length)];

            if (enemyPrefab == null)
                continue;

            if (TryGetRandomMazePosition(out Vector3 spawnPosition))
            {
                spawnPosition.y += enemySpawnYOffset;
                Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);
            }
        }
    }

    private bool TryGetRandomMazePosition(out Vector3 position)
    {
        int width = mazeGenerator.mazeWidth;
        int height = mazeGenerator.mazeHeight;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            int randomX = Random.Range(0, width);
            int randomY = Random.Range(0, height);

            Vector2Int cell = new Vector2Int(randomX, randomY);

            if (usedSpawnCells.Contains(cell))
                continue;

            if (IsEntranceCell(cell))
                continue;

            Vector3 samplePosition = GetCellCenter(randomX, randomY);

            if (NavMesh.SamplePosition(samplePosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                usedSpawnCells.Add(cell);
                position = hit.position;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    private bool TryFindItemSpawnPosition(
        HashSet<Vector2Int> reservedCells,
        GameObject itemPrefab,
        out Vector3 position,
        out Vector2Int chosenCell,
        out Quaternion rotation)
    {
        int width = mazeGenerator.mazeWidth;
        int height = mazeGenerator.mazeHeight;
        List<Vector2Int> candidateCells = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (reservedCells.Contains(cell))
                    continue;

                if (IsEntranceCell(cell))
                    continue;

                if (GetAvailableWallDirections(x, y).Count == 0)
                    continue;

                candidateCells.Add(cell);
            }
        }

        ShuffleCells(candidateCells);

        for (int cellIndex = 0; cellIndex < candidateCells.Count; cellIndex++)
        {
            Vector2Int cell = candidateCells[cellIndex];
            List<WallDirection> wallDirections = GetAvailableWallDirections(cell.x, cell.y);
            PrioritizeInternalWalls(cell.x, cell.y, wallDirections);
            ShuffleWallDirections(wallDirections);

            for (int wallIndex = 0; wallIndex < wallDirections.Count; wallIndex++)
            {
                WallDirection wallDirection = wallDirections[wallIndex];

                if (!TryComputeItemBundleSpawnPose(
                        itemPrefab,
                        cell.x,
                        cell.y,
                        wallDirection,
                        out position,
                        out rotation,
                        out Bounds worldBounds))
                    continue;

                if (!IsItemBundleInsideMaze(worldBounds))
                    continue;

                if (!HasClearWalkwayAtCell(cell.x, cell.y, wallDirection, worldBounds))
                    continue;

                Vector3 startPosition = GetCellCenter(GetEntranceCell().x, GetEntranceCell().y);

                if (!IsPathAvailable(startPosition, GetCellCenter(cell.x, cell.y)))
                    continue;

                chosenCell = cell;
                return true;
            }
        }

        chosenCell = default;
        rotation = Quaternion.identity;
        position = Vector3.zero;
        return false;
    }

    private static void ShuffleCells(List<Vector2Int> cells)
    {
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            Vector2Int temp = cells[i];
            cells[i] = cells[swapIndex];
            cells[swapIndex] = temp;
        }
    }

    private bool TryGetRandomItemPosition(
        HashSet<Vector2Int> reservedCells,
        GameObject itemPrefab,
        out Vector3 position,
        out Vector2Int chosenCell,
        out Quaternion rotation)
    {
        return TryFindItemSpawnPosition(reservedCells, itemPrefab, out position, out chosenCell, out rotation);
    }

    private bool TryGetRandomItemPosition(out Vector3 position)
    {
        position = Vector3.zero;

        if (theme.itemBundlePrefabs == null || theme.itemBundlePrefabs.Length == 0)
            return false;

        for (int i = 0; i < theme.itemBundlePrefabs.Length; i++)
        {
            GameObject itemPrefab = theme.itemBundlePrefabs[i];

            if (itemPrefab == null)
                continue;

            if (TryGetRandomItemPosition(usedSpawnCells, itemPrefab, out position, out Vector2Int cell, out _))
            {
                usedSpawnCells.Add(cell);
                return true;
            }
        }

        return false;
    }

    private static void ShuffleWallDirections(List<WallDirection> wallDirections)
    {
        for (int i = wallDirections.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            WallDirection temp = wallDirections[i];
            wallDirections[i] = wallDirections[swapIndex];
            wallDirections[swapIndex] = temp;
        }
    }

    private void PrioritizeInternalWalls(int cellX, int cellY, List<WallDirection> wallDirections)
    {
        wallDirections.Sort((first, second) =>
        {
            bool firstInternal = IsInternalWall(cellX, cellY, first);
            bool secondInternal = IsInternalWall(cellX, cellY, second);

            if (firstInternal == secondInternal)
                return 0;

            return firstInternal ? -1 : 1;
        });
    }

    private bool IsInternalWall(int cellX, int cellY, WallDirection wallDirection)
    {
        if (wallDirection == WallDirection.Left)
            return cellX > 0;

        if (wallDirection == WallDirection.Right)
            return cellX < mazeGenerator.mazeWidth - 1;

        if (wallDirection == WallDirection.Bottom)
            return cellY > 0;

        return cellY < mazeGenerator.mazeHeight - 1;
    }

    private bool TryComputeItemBundleSpawnPose(
        GameObject prefab,
        int cellX,
        int cellY,
        WallDirection wallDirection,
        out Vector3 position,
        out Quaternion rotation,
        out Bounds worldBounds)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        worldBounds = default;

        if (prefab == null)
            return false;

        Vector3 cellCenter = GetCellCenter(cellX, cellY);

        if (!TryFindBestItemBundleRotation(
                prefab,
                cellX,
                cellY,
                wallDirection,
                cellCenter,
                out rotation))
            return false;

        GameObject tempInstance = Instantiate(prefab, cellCenter, rotation);
        tempInstance.hideFlags = HideFlags.HideAndDontSave;

        Bounds boundsAtCell = CalculateShelfPlacementBounds(tempInstance.transform);

        if (boundsAtCell.size.sqrMagnitude <= 0.0001f)
        {
            Destroy(tempInstance);
            return false;
        }

        Vector3 snapShift = ComputeSnapShiftToWall(boundsAtCell, cellX, cellY, wallDirection);
        tempInstance.transform.position = cellCenter + snapShift;

        boundsAtCell = CalculateShelfPlacementBounds(tempInstance.transform);
        snapShift += ComputeCellCenteringShift(boundsAtCell, cellX, cellY, wallDirection);
        tempInstance.transform.position = cellCenter + snapShift;

        boundsAtCell = CalculateShelfPlacementBounds(tempInstance.transform);
        snapShift.y = GetItemFloorHeight() - boundsAtCell.min.y;

        position = cellCenter + snapShift;
        tempInstance.transform.position = position;
        worldBounds = CalculateImmediateWorldBounds(tempInstance.transform);

        Destroy(tempInstance);

        return true;
    }

    private bool TryFindBestItemBundleRotation(
        GameObject prefab,
        int cellX,
        int cellY,
        WallDirection wallDirection,
        Vector3 cellCenter,
        out Quaternion bestRotation)
    {
        bestRotation = Quaternion.Euler(itemBundleEulerRotation);
        Quaternion baseRotation = Quaternion.Euler(itemBundleEulerRotation);
        Vector3 alongWall = GetWallTangentDirection(wallDirection);
        Vector3 intoRoom = GetDirectionIntoRoom(wallDirection);

        float bestAlongWall = float.MinValue;
        float bestIntoRoom = float.MaxValue;
        bool found = false;

        for (int yawStep = 0; yawStep < 4; yawStep++)
        {
            Quaternion candidate = Quaternion.AngleAxis(yawStep * 90f, Vector3.up) * baseRotation;

            GameObject tempInstance = Instantiate(prefab, cellCenter, candidate);
            tempInstance.hideFlags = HideFlags.HideAndDontSave;

            Bounds bounds = CalculateShelfPlacementBounds(tempInstance.transform);

            if (bounds.size.sqrMagnitude <= 0.0001f)
            {
                Destroy(tempInstance);
                continue;
            }

            Vector3 snapShift = ComputeSnapShiftToWall(bounds, cellX, cellY, wallDirection);
            tempInstance.transform.position = cellCenter + snapShift;

            bounds = CalculateShelfPlacementBounds(tempInstance.transform);
            snapShift += ComputeCellCenteringShift(bounds, cellX, cellY, wallDirection);
            tempInstance.transform.position = cellCenter + snapShift;

            bounds = CalculateShelfPlacementBounds(tempInstance.transform);

            float extentAlongWall = MeasureBoundsExtentAlongDirection(bounds, alongWall);
            float extentIntoRoom = MeasureBoundsExtentAlongDirection(bounds, intoRoom);

            Destroy(tempInstance);

            bool isBetter = extentAlongWall > bestAlongWall + 0.01f
                || (Mathf.Abs(extentAlongWall - bestAlongWall) <= 0.01f && extentIntoRoom < bestIntoRoom);

            if (isBetter)
            {
                bestAlongWall = extentAlongWall;
                bestIntoRoom = extentIntoRoom;
                bestRotation = candidate;
                found = true;
            }
        }

        return found;
    }

    private static float MeasureBoundsExtentAlongDirection(Bounds bounds, Vector3 direction)
    {
        Vector3 normalized = direction.normalized;
        float minProjection = float.MaxValue;
        float maxProjection = float.MinValue;

        foreach (Vector3 corner in GetWorldBoundsCorners(bounds))
        {
            float projection = Vector3.Dot(corner, normalized);
            minProjection = Mathf.Min(minProjection, projection);
            maxProjection = Mathf.Max(maxProjection, projection);
        }

        return maxProjection - minProjection;
    }

    private static void EncapsulateRendererWorldBounds(Renderer renderer, ref Bounds bounds, ref bool initialized)
    {
        Bounds localBounds = renderer.localBounds;
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;

        Vector3[] localCorners =
        {
            center + new Vector3(extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, -extents.y, -extents.z),
        };

        for (int cornerIndex = 0; cornerIndex < localCorners.Length; cornerIndex++)
        {
            Vector3 worldCorner = renderer.transform.TransformPoint(localCorners[cornerIndex]);

            if (!initialized)
            {
                bounds = new Bounds(worldCorner, Vector3.zero);
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(worldCorner);
            }
        }
    }

    private static Bounds CalculateImmediateWorldBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return default;

        bool initialized = false;
        Bounds bounds = default;

        for (int i = 0; i < renderers.Length; i++)
            EncapsulateRendererWorldBounds(renderers[i], ref bounds, ref initialized);

        return bounds;
    }

    private static Bounds CalculateShelfPlacementBounds(Transform instanceRoot)
    {
        Transform shelfTransform = FindShelfTransform(instanceRoot);
        Renderer[] renderers = shelfTransform.GetComponentsInChildren<Renderer>();
        bool initialized = false;
        Bounds bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (IsItemContentRenderer(renderers[i].transform, shelfTransform))
                continue;

            EncapsulateRendererWorldBounds(renderers[i], ref bounds, ref initialized);
        }

        if (!initialized)
            return CalculateImmediateWorldBounds(instanceRoot);

        return bounds;
    }

    private static Transform FindShelfTransform(Transform instanceRoot)
    {
        if (instanceRoot.name.IndexOf("shelf", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return instanceRoot;

        Transform[] children = instanceRoot.GetComponentsInChildren<Transform>();

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.IndexOf("shelf_small", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return children[i];
        }

        return instanceRoot;
    }

    private static bool IsItemContentRenderer(Transform rendererTransform, Transform shelfTransform)
    {
        Transform current = rendererTransform;

        while (current != null && current != shelfTransform)
        {
            string name = current.name;

            if (name.IndexOf("Batch", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Carton", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Single", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Box", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Banana", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Croissant", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Plate", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Cup", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Tart", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            current = current.parent;
        }

        return false;
    }

    private Vector3 ComputeSnapShiftToWall(Bounds bounds, int cellX, int cellY, WallDirection wallDirection)
    {
        Vector3 towardWall = GetDirectionTowardWall(wallDirection);
        Vector3 cellCenter = GetCellCenter(cellX, cellY);
        float insetFromCellEdge = CellSize * 0.5f - WallThickness * 0.5f;
        Vector3 wallFacePoint = cellCenter + towardWall * insetFromCellEdge;

        float extremeTowardWall = float.MinValue;

        foreach (Vector3 corner in GetWorldBoundsCorners(bounds))
        {
            float projection = Vector3.Dot(corner - cellCenter, towardWall);

            if (projection > extremeTowardWall)
                extremeTowardWall = projection;
        }

        float targetProjection = Vector3.Dot(wallFacePoint - cellCenter, towardWall);
        return towardWall * (targetProjection - extremeTowardWall);
    }

    private Vector3 ComputeCellCenteringShift(Bounds bounds, int cellX, int cellY, WallDirection wallDirection)
    {
        Vector3 cellCenter = GetCellCenter(cellX, cellY);
        Vector3 alongWall = GetWallTangentDirection(wallDirection);
        float alongOffset = Vector3.Dot(cellCenter - bounds.center, alongWall);
        return alongWall * alongOffset;
    }

    private float GetItemFloorHeight() => itemSpawnYOffset - 0.2f;

    private static Vector3[] GetWorldBoundsCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        return new[]
        {
            center + new Vector3(extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, -extents.y, -extents.z),
        };
    }

    private bool IsItemBundleInsideMaze(Bounds worldBounds)
    {
        if (worldBounds.size.sqrMagnitude <= 0.0001f)
            return false;

        const float margin = 0.08f;
        float minX = -CellSize * 0.5f + margin;
        float maxX = (mazeGenerator.mazeWidth - 1) * CellSize + CellSize * 0.5f - margin;
        float minZ = -CellSize * 0.5f + margin;
        float maxZ = (mazeGenerator.mazeHeight - 1) * CellSize + CellSize * 0.5f - margin;

        Vector3 center = worldBounds.center;
        return center.x >= minX && center.x <= maxX && center.z >= minZ && center.z <= maxZ;
    }

    private bool HasClearWalkwayAtCell(int cellX, int cellY, WallDirection wallDirection, Bounds worldBounds)
    {
        Vector3 cellCenter = GetCellCenter(cellX, cellY);
        Vector3 intoRoom = GetDirectionIntoRoom(wallDirection);
        float insetFromCellEdge = CellSize * 0.5f - WallThickness * 0.5f;
        Vector3 wallFaceCenter = cellCenter - GetDirectionTowardWall(wallDirection) * insetFromCellEdge;

        float maxDepthFromWall = float.MinValue;

        foreach (Vector3 corner in GetWorldBoundsCorners(worldBounds))
        {
            float depth = Vector3.Dot(corner - wallFaceCenter, intoRoom);

            if (depth > maxDepthFromWall)
                maxDepthFromWall = depth;
        }

        float maxAllowedDepth = CellSize - minWalkwayClearance - WallThickness;
        return maxDepthFromWall <= maxAllowedDepth;
    }

    private Vector3 GetCellCenter(int x, int y)
    {
        return new Vector3(x * CellSize, 0f, y * CellSize);
    }

    private static Vector3 GetDirectionTowardWall(WallDirection wallDirection)
    {
        if (wallDirection == WallDirection.Left)
            return Vector3.left;

        if (wallDirection == WallDirection.Right)
            return Vector3.right;

        if (wallDirection == WallDirection.Bottom)
            return Vector3.back;

        return Vector3.forward;
    }

    private static Vector3 GetDirectionIntoRoom(WallDirection wallDirection) =>
        -GetDirectionTowardWall(wallDirection);

    private static Vector3 GetWallTangentDirection(WallDirection wallDirection)
    {
        if (wallDirection == WallDirection.Left || wallDirection == WallDirection.Right)
            return Vector3.forward;

        return Vector3.right;
    }

    private List<WallDirection> GetAvailableWallDirections(int x, int y)
    {
        List<WallDirection> wallDirections = new List<WallDirection>();

        if (HasWallOnLeftOfCell(x, y))
            wallDirections.Add(WallDirection.Left);

        if (HasWallOnRightOfCell(x, y))
            wallDirections.Add(WallDirection.Right);

        if (HasWallOnBottomOfCell(x, y))
            wallDirections.Add(WallDirection.Bottom);

        if (HasWallOnTopOfCell(x, y))
            wallDirections.Add(WallDirection.Top);

        return wallDirections;
    }

    private bool HasWallOnLeftOfCell(int x, int y)
    {
        if (entranceSide == EntranceSide.Left && x == 0 && y == entranceCellY)
            return false;

        if (x == 0)
            return true;

        return maze[x, y].leftWall;
    }

    private bool HasWallOnRightOfCell(int x, int y)
    {
        if (entranceSide == EntranceSide.Right && x == mazeGenerator.mazeWidth - 1 && y == entranceCellY)
            return false;

        if (x == mazeGenerator.mazeWidth - 1)
            return true;

        return maze[x + 1, y].leftWall;
    }

    private bool HasWallOnBottomOfCell(int x, int y)
    {
        if (entranceSide == EntranceSide.Bottom && x == entranceCellX && y == 0)
            return false;

        if (y == 0)
            return true;

        return maze[x, y - 1].topWall;
    }

    private bool HasWallOnTopOfCell(int x, int y)
    {
        if (entranceSide == EntranceSide.Top && x == entranceCellX && y == mazeGenerator.mazeHeight - 1)
            return false;

        if (y == mazeGenerator.mazeHeight - 1)
            return true;

        return maze[x, y].topWall;
    }

    private bool IsEntranceCell(Vector2Int cell)
    {
        if (entranceSide == EntranceSide.Left && cell.x == 0 && cell.y == entranceCellY)
            return true;

        if (entranceSide == EntranceSide.Right && cell.x == mazeGenerator.mazeWidth - 1 && cell.y == entranceCellY)
            return true;

        if (entranceSide == EntranceSide.Bottom && cell.x == entranceCellX && cell.y == 0)
            return true;

        if (entranceSide == EntranceSide.Top && cell.x == entranceCellX && cell.y == mazeGenerator.mazeHeight - 1)
            return true;

        return false;
    }
}