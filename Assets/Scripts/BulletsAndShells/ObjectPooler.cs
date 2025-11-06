using UnityEngine;
using System.Collections.Generic;

public class ObjectPooler : MonoBehaviour
{
    // Wewnêtrzna klasa do konfiguracji w Inspektorze
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public static ObjectPooler Instance;

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectQueue = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);

                // --- DODAJ TÊ LINIJKÊ ---
                // Upewnij siê, ¿e tag obiektu zgadza siê z tagiem puli
                obj.tag = pool.tag;
                // -------------------------

                objectQueue.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, objectQueue);
        }
    }

    /// <summary>
    /// Pobiera obiekt z puli.
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pula z tagiem '{tag}' nie istnieje.");
            return null;
        }
        if (poolDictionary[tag].Count == 0)
        {
            Debug.LogWarning($"Pula '{tag}' jest pusta. Zwiêksz jej rozmiar!");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        // --- DODAJ TÊ LINIÊ ---
        objectToSpawn.transform.SetParent(null); // Uwalnia obiekt z bycia dzieckiem
                                                 // ----------------------

        objectToSpawn.transform.SetPositionAndRotation(position, rotation);
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    /// <summary>
    /// Zwraca obiekt do puli.
    /// </summary>
    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pula z tagiem '{tag}' nie istnieje.");
            return;
        }

        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
    }
}