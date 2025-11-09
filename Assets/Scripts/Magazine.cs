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

    void Awake()
    {
        if (bulletPrefabForFilling != null && rounds.Count == 0)
        {
            RefillWithPrefab();
        }
    }

    private void RefillWithPrefab()
    {
        rounds.Clear();
        for (int i = 0; i < capacity; i++)
        {
            GameObject newRound = Instantiate(bulletPrefabForFilling);
            newRound.SetActive(false);
            // TODO: Ustawić parent na transform magazynka dla porządku
            // newRound.transform.SetParent(this.transform);
            rounds.Push(newRound);
        }
        Debug.Log($"[Magazine] Napełniono magazynek {capacity} nabojami (Debug).");
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