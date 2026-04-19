using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // 🔹 DODANO: Wymagane dla UnityEvent
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum FireMode { Safe, Semi, Burst, Auto, BoltAction }

[RequireComponent(typeof(XRGrabInteractable))]
public class FireSelectorSimple : MonoBehaviour
{
    [Header("References")]
    public XRGrabInteractable weaponGrab;
    public Transform selectorLever;
    public Transform gripPoint;
    public WeaponControllerBase weaponController;

    [Header("Fire Mods")]
    public List<FireMode> availableModes = new List<FireMode> { FireMode.Safe, FireMode.Semi, FireMode.Auto };
    public float rotationStep = 45f;
    public Vector3 positionStep = Vector3.zero;

    [Header("Events")]
    public UnityEvent OnFireModeChanged; // 🔹 DODANO: Miejsce na podpięcie dźwięku

    private int fireModeIndex = 0;
    private XRBaseInteractor activeHand;
    private bool lastButtonPressed;
    private float buttonCooldown = 0.2f;
    private float nextButtonTime = 0f;
    private Vector3 initialPosition;

    void Awake()
    {
        if (!weaponGrab)
            weaponGrab = GetComponent<XRGrabInteractable>();

        if (selectorLever)
            initialPosition = selectorLever.localPosition;

        ApplyRotation();
        ApplyMode();

        weaponGrab.selectEntered.AddListener(OnGrabbed);
        weaponGrab.selectExited.AddListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == null) return;

        var attach = weaponGrab.GetAttachTransform(interactor);

        if (attach == gripPoint)
        {
            activeHand = interactor;
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == activeHand)
        {
            activeHand = null;
            lastButtonPressed = false;
        }
    }

    void Update()
    {
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

        if (activeHand == null) return;

        bool pressed = false;
        XRNode node = GetHandNode(activeHand);
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid)
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed);

        if (pressed && !lastButtonPressed && Time.time >= nextButtonTime)
        {
            CycleFireMode();
            nextButtonTime = Time.time + buttonCooldown;
        }

        lastButtonPressed = pressed;
    }

    XRNode GetHandNode(XRBaseInteractor interactor)
    {
        string tag = interactor.gameObject.tag;
        if (tag == "LeftHand") return XRNode.LeftHand;
        if (tag == "RightHand") return XRNode.RightHand;
        return XRNode.RightHand;
    }

    void CycleFireMode()
    {
        if (availableModes == null || availableModes.Count == 0) return;

        fireModeIndex = (fireModeIndex + 1) % availableModes.Count;
        ApplyRotation();
        ApplyMode();

        // 🔹 DODANO: Odpalamy event (tutaj podepniesz dźwięk)
        OnFireModeChanged?.Invoke();

        Debug.Log($"[FireSelector] Tryb ognia zmieniony na: {availableModes[fireModeIndex]}");
    }

    void ApplyRotation()
    {
        if (!selectorLever) return;
        selectorLever.localRotation = Quaternion.Euler(fireModeIndex * -rotationStep, 0f, 0f);
        selectorLever.localPosition = initialPosition + (positionStep * fireModeIndex);
    }

    void ApplyMode()
    {
        if (weaponController && availableModes.Count > 0)
            weaponController.currentFireMode = availableModes[fireModeIndex];
    }
}