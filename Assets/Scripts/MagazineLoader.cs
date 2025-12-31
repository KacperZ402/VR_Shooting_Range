using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MagazineLoader : MonoBehaviour
{
    [Header("Konfiguracja")]
    [Tooltip("Podepnij tutaj skrypt Magazine z obiektu rodzica")]
    public Magazine parentMagazine;

    [Tooltip("Tag naboju")]
    public string bulletTag = "Bullet";

    private void Awake()
    {
        // Automatyczne znalezienie rodzica, jeœli zapomnisz przypisaæ w inspektorze
        if (parentMagazine == null)
        {
            parentMagazine = GetComponentInParent<Magazine>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Szybkie odrzucenie po Tagu
        if (!other.CompareTag(bulletTag)) return;

        // 2. Sprawdzenie czy magazynek istnieje i nie jest pe³ny
        if (parentMagazine == null || parentMagazine.IsFull) return;

        // 3. Próba dodania naboju
        // Przekazujemy obiekt do rodzica -> Rodzic decyduje czy kaliber pasuje
        bool success = parentMagazine.TryInsertRound(other.gameObject);

        if (success)
        {
            // Opcjonalnie: Tutaj mo¿esz dodaæ efekt wizualny na samym wlocie (iskra/b³ysk)
        }
    }
}