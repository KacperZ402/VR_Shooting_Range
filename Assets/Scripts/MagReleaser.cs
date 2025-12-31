using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class MagazineReleaseSimple : MonoBehaviour
{
    [Header("Referencje")]
    public XRGrabInteractable weaponGrab;
    public Transform gripPoint; // Ten sam attach point co w FireSelector (Główny chwyt)
    public WeaponControllerBase weaponController;

    // POPRAWKA: Zmiana const na readonly
    // FireSelector używa secondaryButton (B/Y), więc tutaj używamy primaryButton (A/X)
    private readonly InputFeatureUsage<bool> magReleaseButton = CommonUsages.primaryButton;

    private XRBaseInteractor activeHand; // ręka trzymająca gripPoint
    private bool lastButtonPressed;
    private float buttonCooldown = 0.2f;
    private float nextButtonTime = 0f;

    void Awake()
    {
        if (!weaponGrab)
            weaponGrab = GetComponent<XRGrabInteractable>();

        weaponGrab.selectEntered.AddListener(OnGrabbed);
        weaponGrab.selectExited.AddListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == null) return;

        var attach = weaponGrab.GetAttachTransform(interactor);

        // Identyczna logika: sprawdzamy czy to ręka na głównym gripie
        if (attach == gripPoint)
        {
            activeHand = interactor;
            Debug.Log($"[MagRelease] Aktywna ręka ustawiona: {activeHand.name}");
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == activeHand)
        {
            Debug.Log("[MagRelease] Ręka zwolniła gripPoint — reset.");
            activeHand = null;
            lastButtonPressed = false;
        }
    }

    void Update()
    {
        // 🔹 1. Failsafe: Automatyczna detekcja
        if (activeHand == null && weaponGrab != null && weaponGrab.interactorsSelecting.Count > 0)
        {
            foreach (var ix in weaponGrab.interactorsSelecting)
            {
                var interactor = ix as XRBaseInteractor;
                if (interactor == null) continue;

                var attach = weaponGrab.GetAttachTransform(interactor);
                if (attach == gripPoint)
                {
                    activeHand = interactor;
                    break;
                }
            }
        }

        // 🔹 2. Jeśli brak ręki - wyjdź
        if (activeHand == null) return;

        // 🔹 3. Sprawdzanie Inputu (Primary Button - A lub X)
        bool pressed = false;
        XRNode node = GetHandNode(activeHand);
        var device = InputDevices.GetDeviceAtXRNode(node);

        if (device.isValid)
            device.TryGetFeatureValue(magReleaseButton, out pressed);

        // 🔹 4. Wykonanie akcji
        if (pressed && !lastButtonPressed && Time.time >= nextButtonTime)
        {
            TryDropMagazine();
            nextButtonTime = Time.time + buttonCooldown;
        }

        lastButtonPressed = pressed;
    }

    XRNode GetHandNode(XRBaseInteractor interactor)
    {
        string tag = interactor.gameObject.tag;
        if (tag == "LeftHand") return XRNode.LeftHand;
        if (tag == "RightHand") return XRNode.RightHand;

        return XRNode.RightHand; // Fallback
    }

    void TryDropMagazine()
    {
        if (weaponController != null)
        {
            weaponController.EjectMagazine();
        }
    }
}