using UnityEngine;
using UnityEngine.Events;

public class Magazine : MonoBehaviour
{
    [Header("Magazynek")]
    public int maxRounds = 30;
    public int currentRounds = 30;

    [Tooltip("Kaliber magazynka, np. 9mm, 5.56, .45ACP")]
    public string caliber = "5.56x45";

    [Header("Eventy")]
    public UnityEvent OnInsertedToSocket;
    public UnityEvent OnRemovedFromSocket;
    public UnityEvent OnRoundRemoved;

    void Reset()
    {
        currentRounds = maxRounds;
    }

    public bool ExtractRound()
    {
        if (currentRounds <= 0) return false;
        currentRounds--;
        OnRoundRemoved?.Invoke();
        return true;
    }

    public void Refill()
    {
        currentRounds = maxRounds;
    }
    public bool OnInsertedBullet()
    {
        if(currentRounds < maxRounds) 
        {
            currentRounds++;
            return true;
        }
        return false;
    }

    public void NotifyInserted() => OnInsertedToSocket?.Invoke();
    public void NotifyRemoved() => OnRemovedFromSocket?.Invoke();
    [Header("Trigger nabojów")]
    [Tooltip("Tag obiektów nabojów akceptowanych przez magazyn")]
    public string bulletTag = "Bullet";

    private void OnTriggerEnter(Collider other)
    {
        // Sprawdzenie tagu
        if (!other.CompareTag(bulletTag)) return;

        // Pobranie komponentu Bullet (opcjonalnie, jeœli chcesz filtrowaæ kaliber)
        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet != null && bullet.caliber != caliber) return;

        // Dodanie naboju do magazynka
        if (OnInsertedBullet())
        {
            Debug.Log("[Magazine] Nabój dodany do magazynka.");
            Destroy(other.gameObject); // usuniêcie naboju ze sceny
        }
    }
}