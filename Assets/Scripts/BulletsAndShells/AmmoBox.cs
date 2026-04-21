using UnityEngine;

/// <summary>
/// Dzia³a jak pude³ko z amunicj¹. Posiada publiczn¹ metodê
/// 'OpenBox', która po wywo³aniu wyrzuca zawartoœæ z puli
/// w zorganizowanej siatce i niszczy pude³ko.
/// </summary>
public class AmmoBox : MonoBehaviour
{
    [Header("Box Conf")]
    [Tooltip("Prefab naboju (z komponentem Bullet), który ma zostaæ pobrany z puli.")]
    public GameObject ammoPrefab;

    [Header("Grid settings")]
    [Tooltip("Liczba kolumn w siatce (Oœ X)")]
    public int gridColumns = 5;

    [Tooltip("Liczba rzêdów w siatce (Oœ Y/Z)")]
    public int gridRows = 2;

    [Tooltip("Odstêp miêdzy nabojami w siatce (w metrach).")]
    public float gridSpacing = 0.05f; // 5 cm

    private AmmoPoolManager ammoPool;
    private AmmoBoxPoolManager boxPoolManager;

    // Pobieramy referencjê do puli przy starcie
    void Awake()
    {
        ammoPool = AmmoPoolManager.Instance;

        if (ammoPool == null)
        {
            Debug.LogError("[AmmoBox] Nie znaleziono AmmoPoolManager na scenie! To pude³ko nie bêdzie dzia³aæ.", this);
        }
    }

    public void SetPoolManager(AmmoBoxPoolManager manager)
    {
        boxPoolManager = manager;
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
            ReturnToPool();
            return;
        }

        if (ammoPrefab == null)
        {
            Debug.LogError("[AmmoBox] Nie przypisano 'ammoPrefab'! Niszczê...", this);
            ReturnToPool();
            return;
        }

        SpawnRoundsInGrid();
        ReturnToPool();
    }

    /// <summary>
    /// Wewnêtrzna logika pobierania i uk³adania nabojów w siatce.
    /// </summary>
    private void SpawnRoundsInGrid()
    {
        // --- Obliczanie centrowania siatki ---
        // Obliczamy ca³kowit¹ szerokoœæ i g³êbokoœæ siatki
        float gridWidth = (gridColumns - 1) * gridSpacing;
        float gridDepth = (gridRows - 1) * gridSpacing;

        Vector3 startOffset = new Vector3(-gridWidth / 2.0f, 0.01f, -gridDepth / 2.0f);

        for (int y = 0; y < gridRows; y++)
        {
            for (int x = 0; x < gridColumns; x++)
            {
                GameObject round = ammoPool.GetRound(ammoPrefab);
                if (round == null)
                {
                    return; // Przerwij, jeœli pula jest pusta
                }
                Vector3 localPos = startOffset + new Vector3(x * gridSpacing, 0, y * gridSpacing);
                Vector3 spawnPosition = transform.position + (transform.rotation * localPos);
                round.transform.position = spawnPosition;
                round.transform.rotation = transform.rotation;
            }
        }
    }
    private void ReturnToPool()
    {
        if (boxPoolManager != null)
        {
            boxPoolManager.ReturnAmmoBox(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}