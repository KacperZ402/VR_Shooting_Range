using UnityEngine;
using System.Collections.Generic;

public class CasingPoolManager : MonoBehaviour
{
    public static CasingPoolManager Instance { get; private set; }


    private Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();
    private Dictionary<int, int> activeObjectsMap = new Dictionary<int, int>();

    private Transform poolParent;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else
        {
            Instance = this;
            poolParent = new GameObject("CasingPool").transform;
            poolParent.SetParent(this.transform);
        }
    }

    public GameObject GetCasing(GameObject casingPrefab)
    {
        if (casingPrefab == null) return null;

        int prefabID = casingPrefab.GetInstanceID();
        GameObject casingInstance = null;

        // 1. Sprawdzamy czy mamy coœ w kolejce dla tego ID
        if (pools.ContainsKey(prefabID) && pools[prefabID].Count > 0)
        {
            casingInstance = pools[prefabID].Dequeue();
        }
        else
        {
            // 2. Jak nie, tworzymy nowy
            casingInstance = Instantiate(casingPrefab);
        }

        // 3. Rejestrujemy powi¹zanie (Instancja -> Prefab)
        int instanceID = casingInstance.GetInstanceID();
        if (!activeObjectsMap.ContainsKey(instanceID))
        {
            activeObjectsMap.Add(instanceID, prefabID);
        }

        casingInstance.transform.SetParent(null);
        casingInstance.SetActive(true);

        // Reset fizyki (wa¿ne przy ³uskach!)
        Rigidbody rb = casingInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        return casingInstance;
    }

    public void ReturnCasing(GameObject casingInstance)
    {
        if (casingInstance == null) return;

        int instanceID = casingInstance.GetInstanceID();

        // Sprawdzamy w mapie, z jakiego prefaba pochodzi ten obiekt
        if (activeObjectsMap.TryGetValue(instanceID, out int prefabID))
        {
            casingInstance.SetActive(false);
            casingInstance.transform.SetParent(poolParent);

            if (!pools.ContainsKey(prefabID))
            {
                pools[prefabID] = new Queue<GameObject>();
            }

            pools[prefabID].Enqueue(casingInstance);
        }
        else
        {
            Debug.LogWarning("Zwrócono obiekt spoza Poola. Niszczê go.");
            Destroy(casingInstance);
        }
    }
}