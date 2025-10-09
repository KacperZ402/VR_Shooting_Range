using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class FireSelectorSimple : MonoBehaviour
{
    public XRGrabInteractable weaponGrab;
    public Transform selectorLever;

    int fireModeIndex = 0;                // 0 = Safe, 1 = Semi, 2 = Auto
    XRBaseInteractor primaryInteractor;   // pierwsza rêka
    bool lastButtonPressed;

    void Awake()
    {
        if (!weaponGrab) weaponGrab = GetComponent<XRGrabInteractable>();
        ApplyRotation();

        weaponGrab.selectEntered.AddListener(a => {
            if (primaryInteractor == null)
                primaryInteractor = a.interactorObject as XRBaseInteractor;
        });
        weaponGrab.selectExited.AddListener(a => {
            if (a.interactorObject == primaryInteractor) {
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
        fireModeIndex = (fireModeIndex + 1) % 3;
        ApplyRotation();
    }

    void ApplyRotation() =>
        selectorLever.localRotation = Quaternion.Euler(fireModeIndex * -90f, 0f, 0f);
}
