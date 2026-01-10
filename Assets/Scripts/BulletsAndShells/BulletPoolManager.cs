using UnityEngine;
using System.Collections.Generic;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance { get; private set; }

    // S³ownik: ID Prefabu -> Kolejka gotowych pocisków
    private Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();

    // Mapa: ID Instancji (klona na scenie) -> ID Prefabu (z którego powsta³)
    // Dziêki temu wiemy, na któr¹ pó³kê od³o¿yæ zu¿yty pocisk
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
            poolParent = new GameObject("BulletPool").transform;
            poolParent.SetParent(this.transform);
        }
    }

    public GameObject GetBullet(GameObject bulletPrefab)
    {
        if (bulletPrefab == null) return null;

        int prefabID = bulletPrefab.GetInstanceID();
        GameObject bulletInstance = null;

        // 1. Sprawdzamy czy mamy coœ w puli
        if (pools.ContainsKey(prefabID) && pools[prefabID].Count > 0)
        {
            bulletInstance = pools[prefabID].Dequeue();
        }
        else
        {
            // 2. Jak nie, tworzymy nowy
            bulletInstance = Instantiate(bulletPrefab);
        }

        // 3. Rejestrujemy powi¹zanie (Instancja -> Prefab)
        int instanceID = bulletInstance.GetInstanceID();
        if (!activeObjectsMap.ContainsKey(instanceID))
        {
            activeObjectsMap.Add(instanceID, prefabID);
        }

        // 4. Resetowanie stanu pocisku (Bardzo wa¿ne!)
        bulletInstance.transform.SetParent(null);
        bulletInstance.SetActive(true);
        ResetPhysics(bulletInstance);

        return bulletInstance;
    }

    public void ReturnBullet(GameObject bulletInstance)
    {
        if (bulletInstance == null) return;

        int instanceID = bulletInstance.GetInstanceID();

        // Sprawdzamy, z jakiego prefaba pochodzi ten pocisk
        if (activeObjectsMap.TryGetValue(instanceID, out int prefabID))
        {
            bulletInstance.SetActive(false);
            bulletInstance.transform.SetParent(poolParent);

            if (!pools.ContainsKey(prefabID))
            {
                pools[prefabID] = new Queue<GameObject>();
            }

            pools[prefabID].Enqueue(bulletInstance);
        }
        else
        {
            Debug.LogWarning($"Pocisk {bulletInstance.name} nie nale¿y do Poola (brak w rejestrze). Niszczê go.");
            Destroy(bulletInstance);
        }
    }

    // Pomocnicza funkcja czyszcz¹ca fizykê
    // Bez tego wyjêty z puli pocisk móg³by mieæ star¹ prêdkoœæ i polecieæ w bok!
    private void ResetPhysics(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Reset prêdkoœci liniowej
            rb.angularVelocity = Vector3.zero; // Reset prêdkoœci obrotowej
            rb.Sleep(); // Opcjonalnie: uœpienie fizyki na moment startu
        }
    }
}