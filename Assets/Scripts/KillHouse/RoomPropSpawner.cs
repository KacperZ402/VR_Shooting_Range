using System.Collections.Generic;
using UnityEngine;

public class RoomPropSpawner : MonoBehaviour
{
    [System.Serializable]
    private class PropDefinition
    {
        public GameObject prefab;
        public Vector2Int size = Vector2Int.one;
        public bool allowRotate90Z = false;
    }

    [Header("Rooms")]
    [SerializeField] private List<BoxCollider> roomColliders = new List<BoxCollider>();

    [Header("Grid")]
    [SerializeField] private Vector2 cellSize = new Vector2(1f, 1f);

    [Header("Props (per room)")]
    [SerializeField] private List<PropDefinition> roomProps = new List<PropDefinition>();

    [Header("Pooling")]
    [SerializeField] private PropPoolManager poolManager;
    [SerializeField] private int poolSizePerPrefab = 10;

    [Header("Spawn")]
    [SerializeField] private bool spawnOnStart = true;
    public Vector2Int propsCountRange = new Vector2Int(3, 10);

    private readonly List<GameObject> active = new List<GameObject>();

    private void Awake()
    {
        if (poolManager == null)
        {
            poolManager = FindObjectOfType<PropPoolManager>();
        }

        if (poolManager != null)
        {
            poolManager.Warmup(GetUniquePrefabs(), poolSizePerPrefab);
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnProps();
        }
    }

    public void SpawnProps()
    {
        ClearActive();

        if (roomColliders.Count == 0 || roomProps.Count == 0 || poolManager == null)
        {
            return;
        }

        int minCount = Mathf.Max(0, Mathf.Min(propsCountRange.x, propsCountRange.y));
        int maxCount = Mathf.Max(minCount, Mathf.Max(propsCountRange.x, propsCountRange.y));
        int targetCount = Random.Range(minCount, maxCount + 1);
        int placedCount = 0;

        List<RoomGrid> grids = BuildRoomGrids();

        int colliderIndex = 0;
        int safety = targetCount * grids.Count + 100;

        while (placedCount < targetCount && safety-- > 0)
        {
            RoomGrid grid = grids[colliderIndex];

            if (TryPlaceInGrid(grid))
            {
                placedCount++;
            }

            colliderIndex = (colliderIndex + 1) % grids.Count;
        }
    }

    private List<RoomGrid> BuildRoomGrids()
    {
        List<RoomGrid> grids = new List<RoomGrid>();

        foreach (BoxCollider roomCollider in roomColliders)
        {
            if (roomCollider == null)
            {
                continue;
            }

            Bounds bounds = roomCollider.bounds;

            int cellsX = Mathf.Max(1, Mathf.FloorToInt(bounds.size.x / cellSize.x));
            int cellsZ = Mathf.Max(1, Mathf.FloorToInt(bounds.size.z / cellSize.y));

            float offsetX = (bounds.size.x - cellsX * cellSize.x) * 0.5f;
            float offsetZ = (bounds.size.z - cellsZ * cellSize.y) * 0.5f;

            grids.Add(new RoomGrid
            {
                Bounds = bounds,
                CellsX = cellsX,
                CellsZ = cellsZ,
                OffsetX = offsetX,
                OffsetZ = offsetZ,
                Occupied = new bool[cellsX, cellsZ]
            });
        }

        return grids;
    }

    private bool TryPlaceInGrid(RoomGrid grid)
    {
        int maxTries = grid.CellsX * grid.CellsZ;

        for (int attempt = 0; attempt < maxTries; attempt++)
        {
            int x = Random.Range(0, grid.CellsX);
            int z = Random.Range(0, grid.CellsZ);

            if (grid.Occupied[x, z])
            {
                continue;
            }

            if (TryPlaceAtCell(x, z, grid, grid.Occupied))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryPlaceAtCell(int x, int z, RoomGrid grid, bool[,] occupied)
    {
        int attempts = roomProps.Count;

        for (int i = 0; i < attempts; i++)
        {
            PropDefinition def = roomProps[Random.Range(0, roomProps.Count)];
            if (def == null || def.prefab == null)
            {
                continue;
            }

            Vector2Int baseSize = new Vector2Int(Mathf.Max(1, def.size.x), Mathf.Max(1, def.size.y));
            bool rotate90Z = def.allowRotate90Z && Random.value < 0.5f;

            Vector2Int size = rotate90Z ? new Vector2Int(baseSize.y, baseSize.x) : baseSize;

            if (!CanFit(x, z, size, grid.CellsX, grid.CellsZ, occupied))
            {
                continue;
            }

            Vector3 position = GetCellWorldCenter(grid.Bounds, grid.OffsetX, grid.OffsetZ, x, z, size);
            Quaternion rotation = def.prefab.transform.rotation;

            if (rotate90Z)
            {
                rotation *= Quaternion.Euler(0f, 0f, 90f);
            }

            GameObject instance = poolManager.Get(def.prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            active.Add(instance);

            MarkOccupied(x, z, size, occupied);
            return true;
        }

        return false;
    }

    private static bool CanFit(int startX, int startZ, Vector2Int size, int cellsX, int cellsZ, bool[,] occupied)
    {
        if (startX + size.x > cellsX || startZ + size.y > cellsZ)
        {
            return false;
        }

        for (int x = startX; x < startX + size.x; x++)
        {
            for (int z = startZ; z < startZ + size.y; z++)
            {
                if (occupied[x, z])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void MarkOccupied(int startX, int startZ, Vector2Int size, bool[,] occupied)
    {
        for (int x = startX; x < startX + size.x; x++)
        {
            for (int z = startZ; z < startZ + size.y; z++)
            {
                occupied[x, z] = true;
            }
        }
    }

    private Vector3 GetCellWorldCenter(Bounds bounds, float offsetX, float offsetZ, int x, int z, Vector2Int size)
    {
        return new Vector3(
            bounds.min.x + offsetX + (x + size.x * 0.5f) * cellSize.x,
            bounds.center.y,
            bounds.min.z + offsetZ + (z + size.y * 0.5f) * cellSize.y
        );
    }

    public void ClearActive()
    {
        if (poolManager == null)
        {
            active.Clear();
            return;
        }

        for (int i = active.Count - 1; i >= 0; i--)
        {
            poolManager.Return(active[i]);
        }
        active.Clear();
    }

    private List<GameObject> GetUniquePrefabs()
    {
        HashSet<GameObject> unique = new HashSet<GameObject>();
        foreach (PropDefinition def in roomProps)
        {
            if (def != null && def.prefab != null)
            {
                unique.Add(def.prefab);
            }
        }
        return new List<GameObject>(unique);
    }

    private void OnDrawGizmos()
    {
        if (roomColliders == null || roomColliders.Count == 0)
        {
            return;
        }

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);

        foreach (BoxCollider roomCollider in roomColliders)
        {
            if (roomCollider == null)
            {
                continue;
            }

            Bounds bounds = roomCollider.bounds;
            int cellsX = Mathf.Max(1, Mathf.FloorToInt(bounds.size.x / cellSize.x));
            int cellsZ = Mathf.Max(1, Mathf.FloorToInt(bounds.size.z / cellSize.y));

            float offsetX = (bounds.size.x - cellsX * cellSize.x) * 0.5f;
            float offsetZ = (bounds.size.z - cellsZ * cellSize.y) * 0.5f;

            for (int x = 0; x < cellsX; x++)
            {
                for (int z = 0; z < cellsZ; z++)
                {
                    Vector3 cellCenter = new Vector3(
                        bounds.min.x + offsetX + (x + 0.5f) * cellSize.x,
                        bounds.center.y,
                        bounds.min.z + offsetZ + (z + 0.5f) * cellSize.y
                    );

                    Vector3 size = new Vector3(cellSize.x, 0.02f, cellSize.y);
                    Gizmos.DrawWireCube(cellCenter, size);
                }
            }
        }
    }

    private struct RoomGrid
    {
        public Bounds Bounds;
        public int CellsX;
        public int CellsZ;
        public float OffsetX;
        public float OffsetZ;
        public bool[,] Occupied;
    }
}
