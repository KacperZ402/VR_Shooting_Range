using UnityEngine;
using System.Collections.Generic;

public class AmmoPoolManager : MonoBehaviour
{
    public static AmmoPoolManager Instance { get; private set; }

    // Słownik: ID Prefabu -> Kolejka gotowych nabojów
    private Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();

    // Mapa: ID Instancji -> ID Prefabu (żeby wiedzieć, gdzie oddać)
    private Dictionary<int, int> activeObjectsMap = new Dictionary<int, int>();

    private Transform poolParent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            poolParent = new GameObject("AmmoPool").transform;
            poolParent.SetParent(this.transform);
        }
    }

    /// <summary>
    /// Pobiera instancję naboju z puli.
    /// </summary>
    public GameObject GetRound(GameObject roundPrefab)
    {
        if (roundPrefab == null) return null;

        int prefabID = roundPrefab.GetInstanceID();
        GameObject roundInstance = null;

        // 1. Sprawdzamy czy mamy coś w kolejce
        if (pools.ContainsKey(prefabID) && pools[prefabID].Count > 0)
        {
            roundInstance = pools[prefabID].Dequeue();
        }
        else
        {
            // 2. Jak nie, tworzymy nowy
            roundInstance = Instantiate(roundPrefab);
        }

        // 3. Rejestrujemy powiązanie
        int instanceID = roundInstance.GetInstanceID();
        if (!activeObjectsMap.ContainsKey(instanceID))
        {
            activeObjectsMap.Add(instanceID, prefabID);
        }

        // 4. Resetowanie stanu (Ustawiamy pozycję startową)
        roundInstance.transform.SetParent(null);
        roundInstance.SetActive(true);

        // 🔥 Tu czyścimy fizykę PRZED użyciem
        ResetRoundState(roundInstance);

        return roundInstance;
    }

    /// <summary>
    /// Zwraca nabój do puli.
    /// </summary>
    public void ReturnRound(GameObject roundInstance)
    {
        if (roundInstance == null) return;

        int instanceID = roundInstance.GetInstanceID();

        // Sprawdzamy po ID, z jakiego prefaba pochodzi
        if (activeObjectsMap.TryGetValue(instanceID, out int prefabID))
        {
            roundInstance.SetActive(false);
            roundInstance.transform.SetParent(poolParent);

            if (!pools.ContainsKey(prefabID))
            {
                pools[prefabID] = new Queue<GameObject>();
            }

            pools[prefabID].Enqueue(roundInstance);
        }
        else
        {
            Debug.LogWarning($"Obiekt {roundInstance.name} nie należy do AmmoPoola. Niszczę go.");
            Destroy(roundInstance);
        }
    }

    // Pomocnicza metoda do resetowania fizyki
    private void ResetRoundState(GameObject roundInstance)
    {
        // 1. Włącz Collider
        Collider col = roundInstance.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        // 2. Zresetuj Rigidbody
        Rigidbody rb = roundInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep(); // Dla pewności
        }
    }
}