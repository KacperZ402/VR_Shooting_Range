using System.Collections.Generic;
using UnityEngine;

public class KillHouseRoomSpawning : MonoBehaviour
{
    [Header("Room Setup")]
    public GameObject roomStart;
    public List<GameObject> roomPrefabs = new List<GameObject>();
    public int roomsToSpawn = 10;

    [Header("End Palette")]
    public GameObject endPalettePrefab;

    private const string EndPointName = "EndPoint";
    private const float FixedGlobalX = -89.99f;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pool = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();

    void Start()
    {
        SpawnRooms();
    }

    private void SpawnRooms()
    {
        if (roomStart == null || roomPrefabs.Count == 0)
        {
            Debug.LogError("[KillHouse] Brak przypisanych prefabów pokojów.");
            return;
        }

        Transform lastEndPoint = null;
        float accumulatedZ = transform.rotation.eulerAngles.z;

        int lastPrefabIndex = -1;

        // Start room
        Vector3 startPos = transform.position;
        Quaternion startRot = Quaternion.Euler(FixedGlobalX, 0f, accumulatedZ);

        GameObject startRoomInstance = GetFromPool(roomStart, startPos, startRot);
        lastEndPoint = GetEndPoint(startRoomInstance.transform);

        if (lastEndPoint == null)
        {
            Debug.LogError("[KillHouse] Brak EndPoint w roomStart.");
            return;
        }

        for (int i = 0; i < roomsToSpawn; i++)
        {
            int prefabIndex = GetRandomPrefabIndex(lastPrefabIndex);
            GameObject prefab = roomPrefabs[prefabIndex];
            lastPrefabIndex = prefabIndex;

            accumulatedZ += lastEndPoint.localEulerAngles.z;

            Quaternion roomRot = Quaternion.Euler(FixedGlobalX, 0f, accumulatedZ);
            GameObject roomInstance = GetFromPool(prefab, lastEndPoint.position, roomRot);

            lastEndPoint = GetEndPoint(roomInstance.transform);

            if (lastEndPoint == null)
            {
                Debug.LogError($"[KillHouse] Brak EndPoint w pokoju: {roomInstance.name}");
                return;
            }
        }

        if (endPalettePrefab != null)
        {
            Quaternion paletteRot = Quaternion.Euler(FixedGlobalX, 0f, accumulatedZ);
            GetFromPool(endPalettePrefab, lastEndPoint.position, paletteRot);
        }
    }

    private GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!_pool.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            _pool[prefab] = queue;
        }

        GameObject instance;
        if (queue.Count > 0)
        {
            instance = queue.Dequeue();
        }
        else
        {
            instance = Instantiate(prefab);
            _instanceToPrefab[instance] = prefab;
        }

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        return instance;
    }

    public void ReturnToPool(GameObject instance)
    {
        if (instance == null) return;

        if (_instanceToPrefab.TryGetValue(instance, out var prefab))
        {
            instance.SetActive(false);
            _pool[prefab].Enqueue(instance);
        }
        else
        {
            Destroy(instance);
        }
    }

    private int GetRandomPrefabIndex(int lastIndex)
    {
        if (roomPrefabs.Count <= 1)
            return 0;

        int index;
        do
        {
            index = Random.Range(0, roomPrefabs.Count);
        }
        while (index == lastIndex);

        return index;
    }

    private Transform GetEndPoint(Transform roomRoot)
    {
        return roomRoot.Find(EndPointName);
    }
}