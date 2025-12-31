using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class Magazine : MonoBehaviour
{
    [Header("Magazynek")]
    [Tooltip("Maksymalna pojemność magazynka")]
    public int capacity = 30;

    [Tooltip("Kaliber magazynka, np. 9mm, 5.56, .45ACP")]
    public string caliber = "5.56x45";

    private Stack<GameObject> rounds = new Stack<GameObject>();

    public int currentRounds => rounds.Count;

    // Helper dla skryptu dziecka
    public bool IsFull => rounds.Count >= capacity;

    [Header("Eventy")]
    public UnityEvent OnInsertedToSocket;
    public UnityEvent OnRemovedFromSocket;
    public UnityEvent OnRoundRemoved;
    public UnityEvent OnRoundInserted; // Dodatkowy event dla feedbacku

    [Header("Debug")]
    public GameObject bulletPrefabForFilling;
    private AmmoPoolManager ammoPool;

    void Awake()
    {
        // Jeśli masz Singletona, to jest ok
        if (AmmoPoolManager.Instance != null)
            ammoPool = AmmoPoolManager.Instance;
    }

    // --- LOGIKA PRZYJMOWANIA NABOJU (wywoływana przez Dziecko/Loader) ---
    public bool TryInsertRound(GameObject bulletInstance)
    {
        if (IsFull) return false;

        // Sprawdzamy kaliber (logika przeniesiona tutaj, żeby dziecko było tylko detektorem)
        Bullet bulletScript = bulletInstance.GetComponent<Bullet>();
        if (bulletScript != null && bulletScript.caliber != this.caliber)
        {
            Debug.Log("Zły kaliber!");
            return false;
        }

        // Deaktywacja naboju i dodanie na stos
        bulletInstance.SetActive(false);

        // Ustawienie parenta na magazynek (dla porządku w hierarchii)
        bulletInstance.transform.SetParent(this.transform);

        rounds.Push(bulletInstance);

        OnRoundInserted?.Invoke(); // Np. odtwórz dźwięk ładowania
        return true;
    }

    // --- LOGIKA WYJMOWANIA NABOJU (dla Broni) ---
    public GameObject ExtractRound()
    {
        if (rounds.Count <= 0) return null;

        OnRoundRemoved?.Invoke();
        GameObject round = rounds.Pop();

        // Ważne: Odpinamy parenta przy wyjmowaniu, żeby nabój nie "ciągnął" magazynka
        if (round != null) round.transform.SetParent(null);

        return round;
    }

    // --- Obsługa Socketów ---
    public void NotifyInserted() => OnInsertedToSocket?.Invoke();
    public void NotifyRemoved() => OnRemovedFromSocket?.Invoke();

    // --- Debug / Refill ---
    public void Refill()
    {
        if (ammoPool == null) return;
        rounds.Clear();
        for (int i = 0; i < capacity; i++)
        {
            GameObject newRound = ammoPool.GetRound(bulletPrefabForFilling);
            if (newRound != null)
            {
                newRound.SetActive(false);
                newRound.transform.SetParent(this.transform);
                rounds.Push(newRound);
            }
        }
    }
}