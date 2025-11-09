using UnityEngine;

/// <summary>
/// Dzia³a jak pude³ko z amunicj¹. Posiada publiczn¹ metodê
/// 'OpenBox', która po wywo³aniu wyrzuca zawartoœæ z puli
/// i niszczy pude³ko.
/// </summary>
public class AmmoBox : MonoBehaviour
{
    [Header("Konfiguracja Pude³ka")]
    [Tooltip("Prefab naboju (z komponentem Bullet), który ma zostaæ pobrany z puli.")]
    public GameObject ammoPrefab;

    [Tooltip("Liczba nabojów do pobrania/wyrzucenia.")]
    public int count = 30;

    [Tooltip("Jak daleko naboje maj¹ zostaæ rozrzucone (promieñ).")]
    public float spawnRadius = 0.15f;

    private AmmoPoolManager ammoPool;

    // Pobieramy referencjê do puli przy starcie
    void Awake()
    {
        ammoPool = AmmoPoolManager.Instance;

        if (ammoPool == null)
        {
            Debug.LogError("[AmmoBox] Nie znaleziono AmmoPoolManager na scenie! To pude³ko nie bêdzie dzia³aæ.", this);
        }
    }

    /// <summary>
    /// Publiczna funkcja, któr¹ mo¿esz wywo³aæ, aby "otworzyæ" pude³ko.
    /// Wyrzuca naboje i niszczy ten obiekt.
    /// </summary>
    public void OpenBox()
    {
        if (ammoPool == null)
        {
            Debug.LogError("[AmmoBox] Próba otwarcia pude³ka, ale nie znaleziono AmmoPoolManager!", this);
            Destroy(gameObject); // Zniszcz siebie, bo i tak nie zadzia³a
            return;
        }

        if (ammoPrefab == null)
        {
            Debug.LogError("[AmmoBox] Nie przypisano 'ammoPrefab'! Niszczê...", this);
            Destroy(gameObject);
            return;
        }

        // 1. Wyrzuæ zawartoœæ
        SpawnRounds();

        // 2. Zniszcz pude³ko
        Destroy(gameObject);
    }

    /// <summary>
    /// Wewnêtrzna logika pobierania i rozrzucania nabojów.
    /// </summary>
    private void SpawnRounds()
    {
        for (int i = 0; i < count; i++)
        {
            // A. Pobierz nabój z puli
            GameObject round = ammoPool.GetRound(ammoPrefab);
            if (round == null)
            {
                Debug.LogWarning($"[AmmoBox] Pula zwróci³a 'null' dla prefabu: {ammoPrefab.name}. Przerywam.", this);
                return;
            }

            // B. Ustaw pozycjê (rozrzucone w ma³ym promieniu)
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            round.transform.position = transform.position + randomOffset;
            round.transform.rotation = Random.rotation;

            // C. (Opcjonalnie) "Kopnij" naboje, jeœli maj¹ Rigidbody
            Rigidbody rb = round.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.up * 0.3f + randomOffset * 0.5f, ForceMode.Impulse);
            }
        }
    }
}