using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zarz¹dza pul¹ obiektów ³usek (casing).
/// </summary>
public class CasingPoolManager : MonoBehaviour
{
    public static CasingPoolManager Instance { get; private set; }
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
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

    public void ReturnCasing(GameObject casingInstance)
    {
        if (casingInstance == null) return;
        string key = casingInstance.name.Split('(')[0].Trim();

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("Zwrócona ³uska ma pust¹ nazwê! Niszczê.", casingInstance);
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
        }

        casingInstance.SetActive(true);
        return casingInstance;
    }
}