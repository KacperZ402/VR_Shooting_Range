using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum FireMode { Safe, Semi, Burst, Auto, BoltAction }

public class FireSelectorSimple : MonoBehaviour
{
    [Header("Referencje")]
    public XRGrabInteractable weaponGrab;
    public Transform selectorLever;
    public WeaponController weaponController; // referencja do broni

    [Header("Ustawienia")]
    [Tooltip("Lista dostępnych trybów ognia w kolejności przełączania.")]
    public List<FireMode> availableModes = new List<FireMode> { FireMode.Safe, FireMode.Semi, FireMode.Auto };

    [Tooltip("Kąt obrotu między trybami (w stopniach).")]
    public float rotationStep = 45f;

    private int fireModeIndex = 0;
    private XRBaseInteractor primaryInteractor;
    private bool lastButtonPressed;

    void Awake()
    {
        if (!weaponGrab) weaponGrab = GetComponent<XRGrabInteractable>();
        ApplyRotation();
        ApplyMode();

        weaponGrab.selectEntered.AddListener(a =>
        {
            if (primaryInteractor == null)
                primaryInteractor = a.interactorObject as XRBaseInteractor;
        });

        weaponGrab.selectExited.AddListener(a =>
        {
            if (a.interactorObject == primaryInteractor)
            {
                primaryInteractor = null;
                lastButtonPressed = false;
            }
        });
    }

    void Update()
    {
        if (!primaryInteractor) return;

        var nf = primaryInteractor.GetComponent<NearFarInteractor>();
        if (!nf) return;

        bool pressed = false;
        var node = nf.handedness == InteractorHandedness.Left ? XRNode.LeftHand : XRNode.RightHand;
        var dev = InputDevices.GetDeviceAtXRNode(node);
        if (dev.isValid)
            dev.TryGetFeatureValue(nf.handedness == InteractorHandedness.Left ? CommonUsages.primaryButton : CommonUsages.secondaryButton, out pressed);

        if (pressed && !lastButtonPressed) CycleFireMode();
        lastButtonPressed = pressed;
    }

    void CycleFireMode()
    {
        if (availableModes == null || availableModes.Count == 0) return;

        fireModeIndex = (fireModeIndex + 1) % availableModes.Count;
        ApplyRotation();
        ApplyMode();
    }

    void ApplyRotation()
    {
        if (!selectorLever) return;
        selectorLever.localRotation = Quaternion.Euler(fireModeIndex * -rotationStep, 0f, 0f);
    }

    void ApplyMode()
    {
        if (weaponController && availableModes.Count > 0)
            weaponController.currentFireMode = availableModes[fireModeIndex];
    }
}