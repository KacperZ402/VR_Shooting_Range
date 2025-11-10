using UnityEngine;

/// <summary>
/// Dzia³a jak pude³ko z amunicj¹. Posiada publiczn¹ metodê
/// 'OpenBox', która po wywo³aniu wyrzuca zawartoœæ z puli
/// w zorganizowanej siatce i niszczy pude³ko.
/// </summary>
public class AmmoBox : MonoBehaviour
{
    [Header("Konfiguracja Pude³ka")]
    [Tooltip("Prefab naboju (z komponentem Bullet), który ma zostaæ pobrany z puli.")]
    public GameObject ammoPrefab;

    [Header("Ustawienia Siatki (Grid)")]
    [Tooltip("Liczba kolumn w siatce (Oœ X)")]
    public int gridColumns = 5;

    [Tooltip("Liczba rzêdów w siatce (Oœ Y/Z)")]
    public int gridRows = 2;

    [Tooltip("Odstêp miêdzy nabojami w siatce (w metrach).")]
    public float gridSpacing = 0.05f; // 5 cm

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
            Destroy(gameObject);
            return;
        }

        if (ammoPrefab == null)
        {
            Debug.LogError("[AmmoBox] Nie przypisano 'ammoPrefab'! Niszczê...", this);
            Destroy(gameObject);
            return;
        }

        // 1. Wyrzuæ zawartoœæ
        SpawnRoundsInGrid();

        // 2. Zniszcz pude³ko
        Destroy(gameObject);
    }

    /// <summary>
    /// Wewnêtrzna logika pobierania i uk³adania nabojów w siatce.
    /// </summary>
    private void SpawnRoundsInGrid()
    {
        int totalSpawned = 0;
        int totalToSpawn = gridColumns * gridRows;

        if (totalToSpawn <= 0) return;

        // --- Obliczanie centrowania siatki ---
        // Obliczamy ca³kowit¹ szerokoœæ i g³êbokoœæ siatki
        float gridWidth = (gridColumns - 1) * gridSpacing;
        float gridDepth = (gridRows - 1) * gridSpacing;

        // Znajdujemy punkt startowy (lewy dolny róg), aby siatka by³a wyœrodkowana
        // na obiekcie AmmoBox. Dodajemy ma³y offset Y, aby naboje nie kolidowa³y z pod³og¹.
        Vector3 startOffset = new Vector3(-gridWidth / 2.0f, 0.01f, -gridDepth / 2.0f);

        for (int y = 0; y < gridRows; y++)
        {
            for (int x = 0; x < gridColumns; x++)
            {
                // A. Pobierz nabój z puli
                GameObject round = ammoPool.GetRound(ammoPrefab);
                if (round == null)
                {
                    Debug.LogWarning($"[AmmoBox] Pula zwróci³a 'null'. Spawniono {totalSpawned} z {totalToSpawn} nabojów.", this);
                    return; // Przerwij, jeœli pula jest pusta
                }

                // B. Oblicz pozycjê lokaln¹ dla tego naboju
                Vector3 localPos = startOffset + new Vector3(x * gridSpacing, 0, y * gridSpacing);

                // C. Przekszta³æ pozycjê lokaln¹ na œwiatow¹, uwzglêdniaj¹c rotacjê pude³ka
                Vector3 spawnPosition = transform.position + (transform.rotation * localPos);

                // D. Ustaw pozycjê i rotacjê naboju (taka sama jak pude³ka)
                round.transform.position = spawnPosition;
                round.transform.rotation = transform.rotation;

                // Usunêliœmy "wyrzut" fizyczny - naboje po prostu pojawi¹ siê u³o¿one
                totalSpawned++;
            }
        }

        Debug.Log($"[AmmoBox] Otwarto i wyrzucono {totalSpawned} nabojów typu {ammoPrefab.name} w siatce.", this);
    }
}