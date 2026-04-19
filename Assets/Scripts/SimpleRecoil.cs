using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
// Odkomentuj dla Unity 6 / XR Toolkit 3.0+:
// using UnityEngine.XR.Interaction.Toolkit.Interactables; 
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class WeaponRecoilSystem : MonoBehaviour
{
    [Header("configuration")]
    public WeaponControllerBase weaponController;
    public Transform recoilTransform;

    [Header("Two-handed conf ")]
    [Tooltip("Mnożnik odrzutu, gdy broń trzyma tylko jedna ręka.")]
    public float oneHandMultiplier = 2.0f;

    [Header("Recoil posistion")]
    public float recoilKickY = -0.05f;
    public float positionRandomness = 0.002f;

    [Header("Recoil rotation")]
    public float rotationKickX = 10f;
    public float rotationRandomY = 2f;

    [Header("Dynamics")]
    public float snappiness = 20f;
    public float returnSpeed = 10f;

    [Header("Haptics")]
    public float hapticAmplitude = 0.8f;
    public float hapticDuration = 0.1f;

    // Zmienne wewnętrzne
    private Vector3 currentRecoilPos;
    private Vector3 targetRecoilPos;
    private Vector3 currentRecoilRot;
    private Vector3 targetRecoilRot;
    private Vector3 initialPos;
    private Quaternion initialRot;

    private void Start()
    {
        if (weaponController != null)
        {
            weaponController.OnFire.AddListener(AddRecoil);
        }

        if (recoilTransform != null)
        {
            initialPos = recoilTransform.localPosition;
            initialRot = recoilTransform.localRotation;
        }
    }

    private void Update()
    {
        // Optymalizacja (Sleep)
        if (targetRecoilPos == Vector3.zero && currentRecoilPos.sqrMagnitude < 0.000001f &&
            targetRecoilRot == Vector3.zero && currentRecoilRot.sqrMagnitude < 0.000001f)
        {
            return;
        }

        // Interpolacja
        targetRecoilPos = Vector3.Lerp(targetRecoilPos, Vector3.zero, returnSpeed * Time.deltaTime);
        targetRecoilRot = Vector3.Lerp(targetRecoilRot, Vector3.zero, returnSpeed * Time.deltaTime);

        currentRecoilPos = Vector3.Lerp(currentRecoilPos, targetRecoilPos, snappiness * Time.deltaTime);
        currentRecoilRot = Vector3.Lerp(currentRecoilRot, targetRecoilRot, snappiness * Time.deltaTime);

        if (recoilTransform != null)
        {
            recoilTransform.localPosition = initialPos + currentRecoilPos;
            recoilTransform.localRotation = initialRot * Quaternion.Euler(currentRecoilRot);
        }
    }

    public void AddRecoil()
    {
        // 1. SPRAWDZENIE: Ile rąk trzyma GŁÓWNY chwyt?
        // Pobieramy to bezpośrednio z WeaponGrabInteractable
        int handCount = 0;

        if (weaponController != null && weaponController.weaponGrab != null)
        {
            // To jest ta lista, o którą Ci chodziło
            handCount = weaponController.weaponGrab.interactorsSelecting.Count;
        }

        // 2. LOGIKA: Jeśli 2 lub więcej rąk -> Mnożnik 1.0. Jeśli mniej -> Mnożnik 2.0
        float multiplier = (handCount >= 2) ? 1.0f : oneHandMultiplier;


        // 3. APLIKACJA SIŁY
        Vector3 kickVector = new Vector3(
            Random.Range(-positionRandomness, positionRandomness),
            recoilKickY + Random.Range(-positionRandomness, positionRandomness),
            Random.Range(-positionRandomness, positionRandomness)
        );

        targetRecoilPos += kickVector * multiplier;

        float randomY = Random.Range(-rotationRandomY, rotationRandomY);
        float randomZ = Random.Range(-rotationRandomY / 2f, rotationRandomY / 2f);

        targetRecoilRot += new Vector3(-rotationKickX, randomY, randomZ) * multiplier;

        // 4. WIBRACJE (Dla wszystkich rąk trzymających TEN obiekt)
        TriggerHaptics();
    }

    private void TriggerHaptics()
    {
        if (weaponController == null || weaponController.weaponGrab == null) return;
        var interactable = weaponController.weaponGrab;

        if (interactable.isSelected)
        {
            foreach (var interactor in interactable.interactorsSelecting)
            {
                if (interactor is XRBaseInputInteractor inputInteractor)
                {
                    inputInteractor.SendHapticImpulse(hapticAmplitude, hapticDuration);
                }
            }
        }
    }
}