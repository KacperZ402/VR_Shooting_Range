using UnityEngine;
using UnityEngine.Events;
using System.Collections; // Potrzebne do Coroutine
using System.Collections.Generic;

public class Magazine : MonoBehaviour
{
    [Header("Magazynek")]
    public int capacity = 30;
    public string caliber = "5.56x45";

    [Header("Wizualizacja Naboi")]
    public bool showVisualAmmo = false;
    public List<Transform> ammoSlots = new();

    private Stack<GameObject> rounds = new();

    public int currentRounds => rounds.Count;
    public bool IsFull => rounds.Count >= capacity;

    [Header("Eventy")]
    public UnityEvent OnInsertedToSocket;
    public UnityEvent OnRemovedFromSocket;
    public UnityEvent OnRoundRemoved;
    public UnityEvent OnRoundInserted;

    [Header("Debug")]
    public GameObject bulletPrefabForFilling;
    private AmmoPoolManager ammoPool;

    void Awake()
    {
        if (AmmoPoolManager.Instance != null)
            ammoPool = AmmoPoolManager.Instance;
    }

    // --- LOGIKA WSTAWIANIA ---
    public bool TryInsertRound(GameObject bulletInstance)
    {
        if (IsFull || bulletInstance == null) return false;

        Bullet bulletScript = bulletInstance.GetComponent<Bullet>();
        if (bulletScript != null && bulletScript.caliber != this.caliber) return false;

        // 1. Ustawienie rodzica
        bulletInstance.transform.SetParent(this.transform);

        // 2. Dodajemy na stos
        rounds.Push(bulletInstance);

        // 3. 🔥 ZMIANA: Uruchamiamy opóźnione wyłączenie fizyki
        // Dzięki temu XR Toolkit zdąży "puścić" obiekt, a my go zaraz potem zamrozimy
        StartCoroutine(DisablePhysicsDelayed(bulletInstance));

        // 4. Aktualizacja wizualna
        UpdateVisuals();

        OnRoundInserted?.Invoke();
        return true;
    }

    // 🔥 KORUTYNA OPÓŹNIAJĄCA (To naprawia problem z ręką)
    private IEnumerator DisablePhysicsDelayed(GameObject bullet)
    {
        // Czekamy jedną klatkę fizyczną. W tym czasie XR Toolkit "puszcza" obiekt i włącza mu grawitację.
        yield return new WaitForFixedUpdate();

        if (bullet != null)
        {
            var rb = bullet.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;          // TERAZ zamrażamy
                //rb.linearVelocity = Vector3.zero; // Reset pędu (Unity 6)
                //rb.angularVelocity = Vector3.zero;
                // Dla starszego Unity użyj: rb.velocity = Vector3.zero;
            }

            var col = bullet.GetComponent<Collider>();
            if (col) col.enabled = false;

            // Dla pewności wymuszamy pozycję jeszcze raz po wyłączeniu fizyki
            UpdateVisuals();
        }
    }

    // --- LOGIKA WYJMOWANIA ---
    public GameObject ExtractRound()
    {
        if (rounds.Count <= 0) return null;

        OnRoundRemoved?.Invoke();
        GameObject round = rounds.Pop();

        if (round != null)
        {
            round.transform.SetParent(null);
            // Tutaj NIE włączamy fizyki ręcznie, bo zrobi to broń/socket
            // Ale przerywamy korutynę zamrażania, jeśli akurat trwa (rzadki przypadek)
            StopAllCoroutines();
        }

        UpdateVisuals();
        return round;
    }
    private void UpdateVisuals()
    {
        if (!showVisualAmmo || ammoSlots.Count == 0)
        {
            foreach (var r in rounds) r.SetActive(false);
            return;
        }

        int slotIndex = 0;
        foreach (GameObject round in rounds)
        {
            if (slotIndex < ammoSlots.Count)
            {
                Transform slot = ammoSlots[slotIndex];
                round.SetActive(true);
                round.transform.position = slot.position;
                round.transform.rotation = slot.rotation;
            }
            else
            {
                round.SetActive(false);
            }
            slotIndex++;
        }
    }
    public void NotifyInserted() => OnInsertedToSocket?.Invoke();
    public void NotifyRemoved() => OnRemovedFromSocket?.Invoke();
    public void Refill()
    {
        if (ammoPool == null) return;
        while (rounds.Count > 0) ammoPool.ReturnRound(rounds.Pop());

        for (int i = 0; i < capacity; i++)
        {
            GameObject newRound = ammoPool.GetRound(bulletPrefabForFilling);
            if (newRound != null) TryInsertRound(newRound);
        }
    }
}