using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zarz¹dza pul¹ obiektów £USEK, aby unikn¹æ kosztownego
/// Instantiate/Destroy przy ka¿dym cyklu strza³u.
/// </summary>
public class CasingPoolManager : MonoBehaviour
{
    public static CasingPoolManager Instance { get; private set; }

    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
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
            poolParent = new GameObject("CasingPool").transform;
            poolParent.SetParent(this.transform);
        }
    }

    /// <summary>
    /// Zwraca ³uskê do puli.
    /// </summary>
    public void ReturnCasing(GameObject casingInstance)
    {
        // "Czyœcimy" nazwê, aby znaleŸæ klucz prefabu
        string dirtyName = casingInstance.name;
        string key = dirtyName.Split('(')[0].Trim();

        if (string.IsNullOrEmpty(key))
        {
            Destroy(casingInstance);
            return;
        }

        if (!pools.ContainsKey(key))
        {
            pools[key] = new Queue<GameObject>();
        }

        casingInstance.SetActive(false);
        casingInstance.transform.SetParent(poolParent);
        pools[key].Enqueue(casingInstance);
    }

    /// <summary>
    /// Pobiera instancjê ³uski z puli.
    /// </summary>
    public GameObject GetCasing(GameObject casingPrefab)
    {
        string key = casingPrefab.name;
        GameObject casingInstance;

        if (pools.ContainsKey(key) && pools[key].Count > 0)
        {
            casingInstance = pools[key].Dequeue();
            casingInstance.transform.SetParent(null);
        }
        else
        {
            casingInstance = Instantiate(casingPrefab);
            // Nadajemy nazwê, aby ReturnCasing wiedzia³, dok¹d wróciæ
            casingInstance.name = key;
        }

        casingInstance.SetActive(true);
        return casingInstance;
    }
}