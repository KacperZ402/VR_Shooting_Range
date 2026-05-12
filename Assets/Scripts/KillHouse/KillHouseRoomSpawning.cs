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

        float accumulatedY = transform.rotation.eulerAngles.y;

        // 1) Start room
        Vector3 startPos = transform.position;
        Quaternion startRot = Quaternion.Euler(FixedGlobalX, accumulatedY, 0f);

        GameObject startRoomInstance = Instantiate(roomStart, startPos, startRot);
        lastEndPoint = GetEndPoint(startRoomInstance.transform);

        if (lastEndPoint == null)
        {
            Debug.LogError("[KillHouse] Brak EndPoint w roomStart.");
            return;
        }

        // 2) Kolejne pokoje
        for (int i = 0; i < roomsToSpawn; i++)
        {
            GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];

            float endPointLocalY = lastEndPoint.localEulerAngles.y;
            accumulatedY += endPointLocalY;

            Quaternion roomRot = Quaternion.Euler(FixedGlobalX, accumulatedY, 0f);
            GameObject roomInstance = Instantiate(prefab, lastEndPoint.position, roomRot);

            lastEndPoint = GetEndPoint(roomInstance.transform);

            if (lastEndPoint == null)
            {
                Debug.LogError($"[KillHouse] Brak EndPoint w pokoju: {roomInstance.name}");
                return;
            }
        }

        // 3) Końcowa paleta
        if (endPalettePrefab != null)
        {
            Quaternion paletteRot = Quaternion.Euler(FixedGlobalX, accumulatedY, 0f);
            Instantiate(endPalettePrefab, lastEndPoint.position, paletteRot);
        }
    }

    private Transform GetEndPoint(Transform roomRoot)
    {
        return roomRoot.Find(EndPointName);
    }
}
