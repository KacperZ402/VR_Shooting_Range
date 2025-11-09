using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zarz¹dza pul¹ obiektów pocisków (Singleton).
/// </summary>
public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance { get; private set; }

    // S³ownik przechowuj¹cy pule (Klucz = nazwa prefabu, Wartoœæ = Kolejka pocisków)
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
            poolParent = new GameObject("BulletPool").transform;
            poolParent.SetParent(this.transform);
        }
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
            bulletInstance.SetActive(true);
        }
        else
        {
            bulletInstance = Instantiate(bulletPrefab);

            Projectile p = bulletInstance.GetComponent<Projectile>();
            if (p != null)
            {
                p.poolKey = key; // Zapisz klucz, aby wiedzia³ dok¹d wróciæ
            }
            else
            {
                Debug.LogError($"Prefab pocisku '{key}' nie posiada komponentu Projectile!");
            }
        }

        return bulletInstance;
    }

    /// <summary>
    /// Zwraca pocisk do puli.
    /// </summary>
    public void ReturnBullet(GameObject bulletInstance, string poolKey)
    {
        if (string.IsNullOrEmpty(poolKey))
        {
            Debug.LogError("Próbowano zwróciæ pocisk bez klucza puli!", bulletInstance);
            Destroy(bulletInstance);
            return;
        }

        if (!pools.ContainsKey(poolKey))
        {
            pools[poolKey] = new Queue<GameObject>();
        }

        bulletInstance.SetActive(false);
        bulletInstance.transform.SetParent(poolParent);
        pools[poolKey].Enqueue(bulletInstance);
    }
}