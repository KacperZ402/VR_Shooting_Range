using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

public class WeaponGrabInteractable : XRGrabInteractable
{
    [Header("Grip reference (attach point)")]
    public Transform gripAttachPoint; // przypisz w inspectorze attach point gripu
    public WeaponControllerBase weaponController;

    private IXRSelectInteractor gripInteractor; // kto faktycznie trzyma za grip

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        // jeśli nowy interactor trzyma gripAttachPoint, ustaw go jako gripInteractor
        if (GetAttachTransform(args.interactorObject) == gripAttachPoint)
        {
            gripInteractor = args.interactorObject;
            Debug.Log($"[WeaponGrab] GripInteractor ustawiony: {((gripInteractor as MonoBehaviour)?.name ?? gripInteractor.ToString())}");
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        // jeśli zwolniono gripInteractor, sprawdź, czy ktoś inny przejął grip
        if (args.interactorObject == gripInteractor)
        {
            gripInteractor = null;

            foreach (var ix in interactorsSelecting)
            {
                if (GetAttachTransform(ix) == gripAttachPoint)
                {
                    gripInteractor = ix;
                    Debug.Log($"[WeaponGrab] GripInteractor przejęty przez inną rękę: {((gripInteractor as MonoBehaviour)?.name ?? gripInteractor.ToString())}");
                    break;
                }
            }

            if (gripInteractor == null)
                Debug.Log("[WeaponGrab] GripInteractor zwolniony, brak aktywnej ręki na gripa.");
        }
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);

        // tylko ręka trzymająca grip może strzelać
        if (weaponController == null || args.interactorObject != gripInteractor) return;

        weaponController.FireInput(true);
    }

    protected override void OnDeactivated(DeactivateEventArgs args)
    {
        base.OnDeactivated(args);

        if (weaponController == null || args.interactorObject != gripInteractor) return;

        weaponController.FireInput(false);
    }

    public bool IsGripHeld => gripInteractor != null;

    // Opcjonalnie metoda do debugowania
    public IXRSelectInteractor GetGripInteractor() => gripInteractor;
}