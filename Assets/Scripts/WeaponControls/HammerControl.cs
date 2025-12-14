using UnityEngine;

public class HammerController : MonoBehaviour
{
    [Header("Powi¹zania")]
    [Tooltip("Obiekt kurka")]
    public Transform hammerTransform;

    [Tooltip("Skrypt zamka")]
    public ChargingHandleLocking chargingHandle;

    [Header("Ustawienia Rotacji")]
    public Vector3 restRotationEuler;
    public Vector3 cockedRotationEuler;

    [Header("Ustawienia Logiki")]
    [Range(0.5f, 1f)]
    public float cockThreshold = 0.95f;
    public bool isCocked = false;

    // Prywatna zmienna, która przechowa collider zamka
    private Collider slideCollider;

    void Start()
    {
        // AUTOMATYCZNIE pobieramy collider z obiektu, na którym jest skrypt ChargingHandleLocking
        if (chargingHandle != null)
        {
            slideCollider = chargingHandle.GetComponent<Collider>();
        }
        else
        {
            Debug.LogError("Nie przypisano ChargingHandle w skrypcie HammerController!");
        }
    }

    void Update()
    {
        // 1. SAFETY CHECK + OPTYMALIZACJA ZGODNA Z TWOIM SYSTEMEM
        // Jeœli nie mamy zamka LUB jego collider jest wy³¹czony (broñ upuszczona) -> koñczymy.
        if (!slideCollider.enabled)
        {
            return;
        }

        if (hammerTransform == null) return;

        // --- DALSZA LOGIKA BEZ ZMIAN ---
        // 2. Obliczamy postêp
        float currentY = chargingHandle.transform.localPosition.y;
        float maxY = chargingHandle.maxLocalY;
        float currentSlideProgress = 0f;

        if (Mathf.Abs(maxY) > 0.001f)
        {
            currentSlideProgress = Mathf.InverseLerp(0, maxY, currentY);
        }

        // 3. Zatrzask
        if (currentSlideProgress >= cockThreshold)
        {
            isCocked = true;
        }

        // 4. Ruch
        Quaternion targetRot;
        if (isCocked)
        {
            targetRot = Quaternion.Euler(cockedRotationEuler);
        }
        else
        {
            targetRot = Quaternion.Slerp(
                Quaternion.Euler(restRotationEuler),
                Quaternion.Euler(cockedRotationEuler),
                currentSlideProgress
            );
        }

        hammerTransform.localRotation = targetRot;
    }

    public void Fire()
    {
        isCocked = false;
        hammerTransform.localRotation = Quaternion.Euler(restRotationEuler);
    }

    // Kontekstowe menu do zapisu rotacji (jak wczeœniej)
    [ContextMenu("Zapisz SPOCZYNEK")]
    void SaveRestRot() { if (hammerTransform) restRotationEuler = hammerTransform.localEulerAngles; }

    [ContextMenu("Zapisz NAPIÊTY")]
    void SaveCockedRot() { if (hammerTransform) cockedRotationEuler = hammerTransform.localEulerAngles; }
}