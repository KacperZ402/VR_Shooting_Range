using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; // Potrzebne dla Stosu (Stack)

public class Magazine : MonoBehaviour
{
    [Header("Magazynek")]
    [Tooltip("Maksymalna pojemność magazynka")]
    public int capacity = 30;

    [Tooltip("Kaliber magazynka, np. 9mm, 5.56, .45ACP")]
    public string caliber = "5.56x45";

    private Stack<GameObject> rounds = new Stack<GameObject>();

    public int currentRounds
    {
        get { return rounds.Count; }
    }

    [Header("Eventy")]
    public UnityEvent OnInsertedToSocket;
    public UnityEvent OnRemovedFromSocket;
    public UnityEvent OnRoundRemoved;

    [Header("Debug")]
    [Tooltip("Prefab pocisku (z komponentem Bullet) do napełnienia magazynka w trybie testowym.")]
    public GameObject bulletPrefabForFilling;

    private AmmoPoolManager ammoPool; // Referencja do puli

    void Awake()
    {
        ammoPool = AmmoPoolManager.Instance; // Pobierz Singleton

        if (bulletPrefabForFilling != null && rounds.Count == 0)
        {
            RefillWithPrefab();
        }
    }

    /// <summary>
    /// Napełnia magazynek instancjami z puli (do testów).
    /// </summary>
    private void RefillWithPrefab()
    {
        if (ammoPool == null)
        {
            Debug.LogError("AmmoPoolManager nie znaleziony! Nie można napełnić magazynka.", this);
            return;
        }

        rounds.Clear();
        for (int i = 0; i < capacity; i++)
        {
            // 🔹 ZMIANA: Zamiast Instantiate(), pobieramy z puli
            GameObject newRound = ammoPool.GetRound(bulletPrefabForFilling);

            if (newRound != null)
            {
                newRound.SetActive(false); // Dezaktywuj instancję
                // TODO: Ustawić parent na magazynek
                rounds.Push(newRound); // Dodaj na stos
            }
        }
        Debug.Log($"[Magazine] Napełniono magazynek {capacity} nabojami (z puli).");
    }

    public GameObject ExtractRound()
    {
        if (rounds.Count <= 0)
        {
            return null;
        }

        OnRoundRemoved?.Invoke();
        GameObject round = rounds.Pop(); // Zdejmij instancję ze stosu

        // (Opcjonalnie) usuń parenta, aby obiekt nie był już dzieckiem magazynka
        // if(round != null) round.transform.SetParent(null); 

        return round; // Zwróć ją
    }

    // 🔹 POPRAWIONA METODA
    public bool OnInsertedBullet(GameObject bulletInstance)
    {
        if (rounds.Count < capacity)
        {
            // TODO: Ustawić parent na transform magazynka dla porządku
            // bulletInstance.transform.SetParent(this.transform);

            // 🔹 POPRAWKA: Chowamy obiekt, zamiast go niszczyć
            bulletInstance.SetActive(false);

            rounds.Push(bulletInstance); // Włóż na stos
            return true;
        }
        return false; // Magazynek pełny
    }

    public void Refill()
    {
        RefillWithPrefab();
    }

    public void NotifyInserted() => OnInsertedToSocket?.Invoke();
    public void NotifyRemoved() => OnRemovedFromSocket?.Invoke();

    [Header("Trigger nabojów")]
    [Tooltip("Tag obiektów nabojów akceptowanych przez magazyn")]
    public string bulletTag = "Bullet";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(bulletTag)) return;

        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet == null || bullet.caliber != caliber) return;

        // Przekazujemy 'other.gameObject', OnInsertedBullet zajmie się resztą
        if (OnInsertedBullet(other.gameObject))
        {
            Debug.Log("[Magazine] Nabój dodany do magazynka.");
        }
        else
        {
            Debug.Log("[Magazine] Magazynek pełny.");
        }
    }
}