using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WeaponRecoilSystem : MonoBehaviour
{
    [Header("Konfiguracja")]
    [Tooltip("G³ówny kontroler broni (do podpiêcia eventów).")]
    public WeaponControllerBase weaponController;

    [Tooltip("Obiekt, którym bêdziemy ruszaæ (musi to byæ DZIECKO g³ównego obiektu, które zawiera wszystkie meshe broni).")]
    public Transform recoilTransform;

    [Header("Parametry Odrzutu")]
    [Tooltip("Si³a kopniêcia w ty³ (oœ Z).")]
    public float recoilPositionKick = -0.05f;
    [Tooltip("Si³a podbicia lufy (oœ X).")]
    public float recoilRotationKick = 10f;
    [Tooltip("Losowe odchylenie na boki przy strzale (oœ Y).")]
    public float sideRecoilRandomness = 2f;

    [Header("Dynamika")]
    [Tooltip("Jak szybko broñ odskakuje (im wiêcej, tym ostrzej).")]
    public float snappiness = 6f;
    [Tooltip("Jak szybko broñ wraca do pozycji zerowej.")]
    public float returnSpeed = 2f;

    [Header("Haptyka VR")]
    [Tooltip("Si³a wibracji (0-1).")]
    public float hapticAmplitude = 0.8f;
    [Tooltip("Czas trwania wibracji w sekundach.")]
    public float hapticDuration = 0.1f;

    // Zmienne wewnêtrzne do obliczeñ
    private Vector3 currentRecoilPos;
    private Vector3 targetRecoilPos;
    private Vector3 currentRecoilRot;
    private Vector3 targetRecoilRot;

    private void Start()
    {
        // Automatyczne podpiêcie pod event OnFire z WeaponControllerBase
        if (weaponController != null)
        {
            weaponController.OnFire.AddListener(AddRecoil);
        }
        else
        {
            Debug.LogWarning("Nie przypisano WeaponControllerBase w WeaponRecoilSystem!", this);
        }
    }

    private void Update()
    {
        if (weaponController.weaponGrab.IsGripHeld == false) return;
        // 1. Interpolacja celu (Target) w stronê zera (powrót broni na miejsce)
        targetRecoilPos = Vector3.Lerp(targetRecoilPos, Vector3.zero, returnSpeed * Time.deltaTime);
        targetRecoilRot = Vector3.Lerp(targetRecoilRot, Vector3.zero, returnSpeed * Time.deltaTime);

        // 2. Interpolacja obecnej pozycji (Current) w stronê celu (Target) - efekt sprê¿ystoœci
        currentRecoilPos = Vector3.Lerp(currentRecoilPos, targetRecoilPos, snappiness * Time.deltaTime);
        currentRecoilRot = Vector3.Lerp(currentRecoilRot, targetRecoilRot, snappiness * Time.deltaTime);

        // 3. Aplikowanie transformacji do wizualnego modelu broni
        if (recoilTransform != null)
        {
            recoilTransform.localPosition = currentRecoilPos;
            recoilTransform.localRotation = Quaternion.Euler(currentRecoilRot);
        }
    }

    /// <summary>
    /// Funkcja wywo³ywana przez Event OnFire
    /// </summary>
    public void AddRecoil()
    {
        // Dodajemy "kopniêcie" do targetu
        targetRecoilPos += Vector3.forward * recoilPositionKick;

        // Obliczamy losowy odrzut na boki
        float randomY = Random.Range(-sideRecoilRandomness, sideRecoilRandomness);

        // Dodajemy rotacjê (podbicie lufy -X + losowe Y)
        targetRecoilRot += new Vector3(-recoilRotationKick, randomY, 0f);

        // Wywo³anie wibracji kontrolera
        TriggerHaptics();
    }

    private void TriggerHaptics()
    {
        if (weaponController == null || weaponController.weaponGrab == null) return;

        // Pobieramy interaktor trzymaj¹cy broñ
        var interactable = weaponController.weaponGrab;

        // XR Interaction Toolkit 3.x / 2.x logic
        if (interactable.isSelected)
        {
            // Pobieramy pierwszy interaktor (np. praw¹ d³oñ)
            var interactor = interactable.interactorsSelecting[0];

            // Wysy³amy impuls haptyczny
            if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputInteractor)
            {
                inputInteractor.SendHapticImpulse(hapticAmplitude, hapticDuration);
            }
        }
    }
}