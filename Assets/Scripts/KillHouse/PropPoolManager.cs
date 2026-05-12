using System.Collections.Generic;
using UnityEngine;

public class PropPoolManager : MonoBehaviour
{
    [SerializeField] private int defaultPoolSizePerPrefab = 10;

    private readonly Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

    public void Warmup(IReadOnlyList<GameObject> prefabs, int? poolSizeOverride = null)
    {
        int size = poolSizeOverride ?? defaultPoolSizePerPrefab;

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null || pool.ContainsKey(prefab))
            {
                continue;
            }

            Queue<GameObject> queue = new Queue<GameObject>();
            for (int i = 0; i < size; i++)
            {
                GameObject instance = Instantiate(prefab, transform);
                instance.SetActive(false);
                queue.Enqueue(instance);
                instanceToPrefab[instance] = prefab;
            }

            pool.Add(prefab, queue);
        }
    }

    public GameObject Get(GameObject prefab)
    {
        if (!pool.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pool[prefab] = queue;
        }

        if (queue.Count == 0)
        {
            GameObject extra = Instantiate(prefab, transform);
            extra.SetActive(false);
            instanceToPrefab[extra] = prefab;
            return extra;
        }

        return queue.Dequeue();
    }

    public void Return(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.SetActive(false);

        if (instanceToPrefab.TryGetValue(instance, out GameObject prefab))
        {
            pool[prefab].Enqueue(instance);
        }
        else
        {
            Destroy(instance);
        }
    }
}