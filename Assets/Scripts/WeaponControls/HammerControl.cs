using UnityEngine;

public class HammerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Obiekt kurka")]
    public Transform hammerTransform;

    [Tooltip("Twój g³ówny WeaponController (MÓZG)")]
    public WeaponControllerBase weaponController;

    [Tooltip("Skrypt zamka (do pobierania pozycji suwad³a)")]
    public ChargingHandleLocking chargingHandle;

    [Header("Rottation Settings")]
    public Vector3 restRotationEuler;
    public Vector3 cockedRotationEuler;

    // Prywatna zmienna, która przechowa collider zamka
    private Collider slideCollider;

    void Start()
    {
        // 1. Automatyczne pobranie collidera zamka
        if (chargingHandle != null)
        {
            slideCollider = chargingHandle.GetComponent<Collider>();
        }
        else
        {
            Debug.LogError("Nie przypisano ChargingHandle w skrypcie HammerController!");
        }

        // 2. Próba automatycznego znalezienia WeaponControllera, jeœli nie przypisa³eœ rêcznie
        if (weaponController == null)
        {
            weaponController = GetComponentInParent<WeaponControllerBase>();
        }
    }

    void Update()
    {
        if (!weaponController.weaponGrab.IsGripHeld) {
            return;
        }
        // --- OPTYMALIZACJA (Twoja zasada) ---
        // Jeœli collider zamka jest wy³¹czony (broñ w kaburze/rêce puszczone), nie liczymy animacji.
        if (slideCollider != null && !slideCollider.enabled)
        {
            return;
        }

        if (hammerTransform == null || weaponController == null || chargingHandle == null) return;

        // --- LOGIKA WIZUALNA ---

        // 1. Obliczamy, jak bardzo zamek jest cofniêty (0 = przód, 1 = ty³)
        float currentY = chargingHandle.transform.localPosition.y;
        float maxY = chargingHandle.maxLocalY;
        float slideProgress = 0f;

        if (Mathf.Abs(maxY) > 0.001f)
        {
            slideProgress = Mathf.InverseLerp(0, maxY, currentY);
        }

        Quaternion targetRot;

        // 2. Decyzja o rotacji
        // WARUNEK A: Jeœli MÓZG mówi, ¿e kurek jest napiêty -> Ustawiamy pozycjê napiêt¹.
        if (weaponController.isHammerCocked)
        {
            // Mo¿emy ewentualnie sprawdziæ, czy slideProgress > 1 (overtravel), 
            // ale dla prostoty przyjmijmy, ¿e "napiêty" to pozycja cockedRotation.
            targetRot = Quaternion.Euler(cockedRotationEuler);
        }
        // WARUNEK B: Kurek ZWOLNIONY (np. po strzale), ale zamek go fizycznie popycha.
        else
        {
            // Kurek pod¹¿a za zamkiem (Lerp od spoczynku do napiêcia)
            targetRot = Quaternion.Slerp(
                Quaternion.Euler(restRotationEuler),
                Quaternion.Euler(cockedRotationEuler),
                slideProgress
            );
        }

        // 3. Aplikujemy rotacjê
        hammerTransform.localRotation = targetRot;
    }

    // Menu kontekstowe do ustawiania rotacji (bez zmian)
    [ContextMenu("Zapisz SPOCZYNEK")]
    void SaveRestRot() { if (hammerTransform) restRotationEuler = hammerTransform.localEulerAngles; }

    [ContextMenu("Zapisz NAPIÊTY")]
    void SaveCockedRot() { if (hammerTransform) cockedRotationEuler = hammerTransform.localEulerAngles; }
}