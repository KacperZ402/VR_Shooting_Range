using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class WeaponController : MonoBehaviour
{

    [Header("Referencje")]
    public AmmoSocket ammoSocket;
    public ChargingHandle chargingHandle;
    public BoltFollower bolt;

    [Header("Dane broni")]
    public string caliber = "5.56x45";

    [Header("Stan")]
    public bool isChambered = false;
    public bool isBoltLockedBack = false;
    public FireMode currentFireMode = FireMode.Safe;

    [Header("Parametry trybów ognia")]
    public int burstCount = 3;
    public float fireRate = 0.1f;
    public float burstDelay = 0.08f;

    [Header("Eventy")]
    public UnityEvent OnFire;
    public UnityEvent OnDryFire;
    public UnityEvent OnRoundChambered;
    public UnityEvent OnRoundEjected;
    public UnityEvent OnBoltLockedBack;
    public UnityEvent OnBoltReleasedEvent;

    [Header("XR Grip")]
    public Transform gripAttachPoint; // przypisz attach transform gripa w inspectorze
    private XRBaseInteractor gripInteractor; // aktualna ręka trzymająca grip
    private XRGrabInteractable grabInteractable;

    private float lastFireTime;
    private bool triggerPressed;
    private int burstShotsRemaining = 0;

    void Awake()
    {
        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.AddListener(OnBoltPulled);
            chargingHandle.OnBoltReleased.AddListener(() => ReleaseBoltAction(false));
        }

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    void OnDestroy()
    {
        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.RemoveListener(OnBoltPulled);
            chargingHandle.OnBoltReleased.RemoveAllListeners();
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == null) return;

        // sprawdzamy czy to ręka, która chwyta za gripAttachPoint
        if (grabInteractable.GetAttachTransform(interactor) == gripAttachPoint)
        {
            gripInteractor = interactor;
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == null) return;

        if (interactor == gripInteractor)
        {
            gripInteractor = null;
        }
    }

    // Wywoływane przez rękę (np. w HandController)
    public void FireInputFromHand(XRBaseInteractor hand, bool pressed)
    {
        // tylko ręka trzymająca grip może strzelać
        if (gripInteractor != null && hand == gripInteractor)
        {
            FireInput(pressed);
        }
    }

    // ---------- Logika strzału ----------
    public void FireInput(bool pressed)
    {
        switch (currentFireMode)
        {
            case FireMode.Safe:
                break;

            case FireMode.Semi:
                if (pressed && !triggerPressed)
                    FireOnce();
                break;

            case FireMode.Burst:
                if (pressed && !triggerPressed && burstShotsRemaining == 0)
                    burstShotsRemaining = burstCount;
                break;

            case FireMode.BoltAction:
                if (pressed && !triggerPressed)
                {
                    if (FireOnce())
                    {
                        isBoltLockedBack = true;
                        OnBoltLockedBack?.Invoke();
                    }
                }
                break;

            case FireMode.Auto:
                // handled in Update
                break;
        }

        triggerPressed = pressed;
    }

    void Update()
    {
        HandleBurstLogic();
        HandleAutoFire();
    }

    private void HandleBurstLogic()
    {
        if (currentFireMode != FireMode.Burst) return;
        if (burstShotsRemaining <= 0) return;

        if (Time.time - lastFireTime >= burstDelay)
        {
            if (FireOnce())
            {
                burstShotsRemaining--;
                lastFireTime = Time.time;
            }
            else
            {
                burstShotsRemaining = 0;
            }
        }
    }

    private void HandleAutoFire()
    {
        if (currentFireMode != FireMode.Auto) return;
        if (!triggerPressed) return;

        if (Time.time >= lastFireTime + fireRate)
        {
            bool fired = FireOnce();
            if (fired)
                lastFireTime = Time.time;
        }
    }

    private bool FireOnce()
    {
        if (isBoltLockedBack || !isChambered || !bolt.IsBoltForward)
        {
            OnDryFire?.Invoke();
            return false;
        }

        isChambered = false;
        OnFire?.Invoke();

        if (!TryChamberFromMagazine())
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }

        return true;
    }

    // ---------- Bolt ----------
    public void OnBoltPulled()
    {
        if (isChambered)
        {
            isChambered = false;
            OnRoundEjected?.Invoke();
        }

        if (!TryChamberFromMagazine())
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
        else
        {
            isBoltLockedBack = false;
        }
    }

    public bool CanReleaseBolt()
    {
        bool magExists = ammoSocket != null && ammoSocket.currentMagazine != null;
        bool magHasRounds = magExists && ammoSocket.currentMagazine.currentRounds > 0;
        return isBoltLockedBack && (!magExists || magHasRounds);
    }

    public void ReleaseBoltAction(bool force = false)
    {
        if (!force && !CanReleaseBolt())
            return;

        TryChamberFromMagazine();
        isBoltLockedBack = false;

        OnBoltReleasedEvent?.Invoke();
    }

    public bool TryChamberFromMagazine()
    {
        if (ammoSocket == null || ammoSocket.currentMagazine == null)
            return false;

        if (ammoSocket.currentMagazine.caliber != caliber)
            return false;

        if (ammoSocket.TryTakeRound())
        {
            isChambered = true;
            OnRoundChambered?.Invoke();
            return true;
        }

        return false;
    }
}