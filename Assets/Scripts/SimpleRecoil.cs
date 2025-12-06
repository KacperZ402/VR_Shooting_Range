using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XR Toolkit 3.x

public class WeaponRecoilSystem : MonoBehaviour
{
    [Header("Konfiguracja")]
    public WeaponControllerBase weaponController;
    public Transform recoilTransform;

    [Header("Parametry Odrzutu")]
    public float recoilPositionKick = -0.05f;
    public float recoilRotationKick = 10f;
    public float sideRecoilRandomness = 2f;

    [Header("Dynamika")]
    public float snappiness = 6f;
    public float returnSpeed = 2f;

    [Header("Haptyka VR")]
    public float hapticAmplitude = 0.8f;
    public float hapticDuration = 0.1f;

    // Zmienne wewnętrzne do obliczeń
    private Vector3 currentRecoilPos;
    private Vector3 targetRecoilPos;
    private Vector3 currentRecoilRot;
    private Vector3 targetRecoilRot;

    // 🔹 NOWE: Zmienne do zapamiętania pozycji startowej
    private Vector3 initialPos;
    private Quaternion initialRot;

    private void Start()
    {
        if (weaponController != null)
        {
            weaponController.OnFire.AddListener(AddRecoil);
        }

        // 🔹 NOWE: Zapamiętujemy, gdzie recoilTransform stał na początku gry
        if (recoilTransform != null)
        {
            initialPos = recoilTransform.localPosition;
            initialRot = recoilTransform.localRotation;
        }
    }

    private void Update()
    {
        if (weaponController.weaponGrab.IsGripHeld == false) return;

        // 1. Interpolacja celu do zera
        targetRecoilPos = Vector3.Lerp(targetRecoilPos, Vector3.zero, returnSpeed * Time.deltaTime);
        targetRecoilRot = Vector3.Lerp(targetRecoilRot, Vector3.zero, returnSpeed * Time.deltaTime);

        // 2. Interpolacja obecnej pozycji
        currentRecoilPos = Vector3.Lerp(currentRecoilPos, targetRecoilPos, snappiness * Time.deltaTime);
        currentRecoilRot = Vector3.Lerp(currentRecoilRot, targetRecoilRot, snappiness * Time.deltaTime);

        // 3. Aplikowanie transformacji
        if (recoilTransform != null)
        {
            // 🔹 ZMIANA: Dodajemy odrzut do pozycji startowej (zamiast nadpisywać)
            recoilTransform.localPosition = initialPos + currentRecoilPos;

            // 🔹 ZMIANA: Mnożymy rotację startową przez rotację odrzutu
            recoilTransform.localRotation = initialRot * Quaternion.Euler(currentRecoilRot);
        }
    }

    public void AddRecoil()
    {
        targetRecoilPos += Vector3.forward * recoilPositionKick;
        float randomY = Random.Range(-sideRecoilRandomness, sideRecoilRandomness);
        targetRecoilRot += new Vector3(-recoilRotationKick, randomY, 0f);
        TriggerHaptics();
    }

    private void TriggerHaptics()
    {
        if (weaponController == null || weaponController.weaponGrab == null) return;
        var interactable = weaponController.weaponGrab;

        if (interactable.isSelected)
        {
            var interactor = interactable.interactorsSelecting[0];
            if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputInteractor)
            {
                inputInteractor.SendHapticImpulse(hapticAmplitude, hapticDuration);
            }
        }
    }
}