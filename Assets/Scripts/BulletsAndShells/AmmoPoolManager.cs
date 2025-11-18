using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zarządza pulą obiektów nabojów (instancji, które żyją w magazynkach),
/// aby uniknąć kosztownego Instantiate/Destroy.
/// </summary>
public class AmmoPoolManager : MonoBehaviour
{
    public static AmmoPoolManager Instance { get; private set; }

    // 🔹 ZMIANA: Kluczem jest "czysta" nazwa prefabu
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
            poolParent = new GameObject("AmmoPool").transform;
            poolParent.SetParent(this.transform);
        }
    }
    public void ReturnRound(GameObject roundInstance)
    {
        Bullet bullet = roundInstance.GetComponent<Bullet>();
        if (bullet == null) { Destroy(roundInstance); return; }

        // Pobieramy pełną nazwę, np. "5.56_AP(1)" lub "5.56_AP(Clone)"
        string dirtyName = roundInstance.name;
        // Odcinamy wszystko, co zaczyna się od nawiasu '('
        string key = dirtyName.Split('(')[0];
        // Usuwamy ewentualne białe znaki na końcu
        key = key.Trim();

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("Zwrócony obiekt ma pustą nazwę po czyszczeniu! Niszczę.", roundInstance);
            Destroy(roundInstance);
            return;
        }

        if (!pools.ContainsKey(key))
        {
            pools[key] = new Queue<GameObject>();
        }

        // 🔽🔽🔽 NOWA SEKCJA: RESETOWANIE STANU NABOJU 🔽🔽🔽

        // 1. Włącz Collider (bo mógł zostać wyłączony w komorze)
        Collider col = roundInstance.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        // 2. Zresetuj fizykę (żeby nie zachował pędu lub stanu isKinematic z komory)
        Rigidbody rb = roundInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // Przywracamy fizykę
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 🔼🔼🔼 KONIEC NOWEJ SEKCJI 🔼🔼🔼

        roundInstance.SetActive(false);
        roundInstance.transform.SetParent(poolParent);
        pools[key].Enqueue(roundInstance);
    }

    /// <summary>
    /// Pobiera instancję naboju z puli.
    /// </summary>
    public GameObject GetRound(GameObject roundPrefab)
    {
        // 🔹 ZMIANA: Kluczem jest nazwa prefabu, a nie kaliber
        string key = roundPrefab.name;
        GameObject roundInstance;

        if (pools.ContainsKey(key) && pools[key].Count > 0)
        {
            roundInstance = pools[key].Dequeue();
            roundInstance.transform.SetParent(null);
        }
        else
        {
            roundInstance = Instantiate(roundPrefab);
        }

        roundInstance.SetActive(true);
        return roundInstance;
    }
}