using UnityEngine;
using System.Collections.Generic;

public class ImpactPoolManager : MonoBehaviour
{
    public static ImpactPoolManager Instance { get; private set; }

    // S³ownik: Klucz to InstanceID prefaba -> Wartoœæ to Kolejka gotowych obiektów
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

    // S³ownik pomocniczy, ¿eby wiedzieæ, do której kolejki oddaæ obiekt
    private Dictionary<int, int> activeObjectsMap = new Dictionary<int, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Pobiera obiekt z poola lub tworzy nowy.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float autoReturnTime = 0f)
    {
        if (prefab == null) return null;

        int prefabID = prefab.GetInstanceID();
        GameObject objToSpawn = null;

        // 1. SprawdŸ czy mamy kolejkê dla tego prefaba
        if (poolDictionary.ContainsKey(prefabID) && poolDictionary[prefabID].Count > 0)
        {
            // Mamy wolny obiekt - wyci¹gamy go
            objToSpawn = poolDictionary[prefabID].Dequeue();
        }
        else
        {
            // Nie mamy wolnego - tworzymy nowy
            objToSpawn = Instantiate(prefab);
        }

        // 2. Konfiguracja obiektu
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        // Resetujemy parenta (na wypadek gdyby by³ przyklejony do œciany)
        objToSpawn.transform.SetParent(null);

        // Reset dla Particle System (wa¿ne!)
        var ps = objToSpawn.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        // Zapisujemy ID prefaba, ¿eby wiedzieæ gdzie go oddaæ
        int instanceID = objToSpawn.GetInstanceID();
        if (!activeObjectsMap.ContainsKey(instanceID))
        {
            activeObjectsMap.Add(instanceID, prefabID);
        }

        // 3. Auto-zwrot po czasie (jeœli podano czas)
        if (autoReturnTime > 0f)
        {
            StartCoroutine(ReturnDelayed(objToSpawn, autoReturnTime));
        }

        return objToSpawn;
    }

    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        int instanceID = obj.GetInstanceID();

        // Sprawdzamy, czy wiemy z jakiego prefaba pochodzi ten obiekt
        if (activeObjectsMap.TryGetValue(instanceID, out int prefabID))
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform); // Chowamy go pod Managera, ¿eby nie œmieci³ na scenie

            // Jeœli nie ma kolejki dla tego ID (dziwne, ale mo¿liwe), tworzymy j¹
            if (!poolDictionary.ContainsKey(prefabID))
            {
                poolDictionary[prefabID] = new Queue<GameObject>();
            }

            poolDictionary[prefabID].Enqueue(obj);
        }
        else
        {
            // Jeœli nie znamy tego obiektu (nie by³ spawnowany przez Managera), po prostu niszczymy
            Debug.LogWarning($"Obiekt {obj.name} nie nale¿y do Poola. Niszczê go.");
            Destroy(obj);
        }
    }

    private System.Collections.IEnumerator ReturnDelayed(GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);
        // Sprawdzamy czy obiekt nadal jest aktywny (móg³ zostaæ zwrócony rêcznie wczeœniej)
        if (obj.activeSelf)
        {
            ReturnToPool(obj);
        }
    }
}