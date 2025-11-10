using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zarz¹dza pul¹ obiektów POCISKÓW (tych, które lataj¹),
/// aby unikn¹æ kosztownego Instantiate/Destroy przy strzale.
/// </summary>
public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance { get; private set; }

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
            poolParent = new GameObject("BulletPool").transform; // Inna nazwa dla porz¹dku
            poolParent.SetParent(this.transform);
        }
    }

    /// <summary>
    /// Zwraca pocisk do puli.
    /// </summary>
    public void ReturnBullet(GameObject bulletInstance)
    {
        // "Czyœcimy" nazwê, aby znaleŸæ klucz prefabu
        string dirtyName = bulletInstance.name;
        string key = dirtyName.Split('(')[0].Trim();

        if (string.IsNullOrEmpty(key))
        {
            Destroy(bulletInstance);
            return;
        }

        if (!pools.ContainsKey(key))
        {
            pools[key] = new Queue<GameObject>();
        }

        bulletInstance.SetActive(false);
        bulletInstance.transform.SetParent(poolParent);
        pools[key].Enqueue(bulletInstance);
    }

    /// <summary>
    /// Pobiera instancjê pocisku z puli.
    /// </summary>
    public GameObject GetBullet(GameObject bulletPrefab)
    {
        string key = bulletPrefab.name;
        GameObject bulletInstance;

        if (pools.ContainsKey(key) && pools[key].Count > 0)
        {
            bulletInstance = pools[key].Dequeue();
            bulletInstance.transform.SetParent(null);
        }
        else
        {
            bulletInstance = Instantiate(bulletPrefab);
            // Nie musimy siê martwiæ nazw¹ (1), bo ReturnBullet j¹ czyœci
        }

        bulletInstance.SetActive(true);
        return bulletInstance;
    }
}