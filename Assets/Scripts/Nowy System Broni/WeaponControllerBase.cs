using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponControllerBase : MonoBehaviour
{
    [Header("Referencje")]
    public AmmoSocket ammoSocket;
    public ChargingHandle chargingHandle;
    public BoltFollower bolt;
    public FireSelectorSimple fireSelector;
    public WeaponGrabInteractable weaponGrab;

    [Header("Dane broni")]
    public string caliber = "5.56x45";

    [Header("Stan broni")]
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

    protected float lastFireTime;
    protected bool triggerPressed;
    protected int burstShotsRemaining = 0;

    protected virtual void Awake()
    {
        if (fireSelector != null)
            fireSelector.weaponController = this;

        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.AddListener(OnBoltPulled);
            chargingHandle.OnBoltReleased.AddListener(() => ReleaseBoltAction(false));
        }

        if (weaponGrab != null)
            weaponGrab.weaponController = this;
    }

    protected virtual void Update()
    {
        HandleBurstLogic();
        HandleAutoFire();
    }

    // -------------------------- STRZAŁ --------------------------

    public virtual void FireInput(bool pressed)
    {
        // tylko ręka trzymająca grip może strzelać
        if (weaponGrab != null && !weaponGrab.IsGripHeld) return;

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
                    HandleBoltActionFire();
                break;

            case FireMode.Auto:
                // obsługiwany w Update
                break;
        }

        triggerPressed = pressed;
    }

    protected virtual void HandleBurstLogic()
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

    protected virtual void HandleAutoFire()
    {
        if (currentFireMode != FireMode.Auto) return;
        if (!triggerPressed) return;

        if (Time.time >= lastFireTime + fireRate)
        {
            if (FireOnce())
                lastFireTime = Time.time;
        }
    }

    // -------------------------- LOGIKA STRZAŁU --------------------------

    protected virtual bool FireOnce()
    {
        if (isBoltLockedBack || !isChambered || !bolt.IsBoltForward)
        {
            OnDryFire?.Invoke();
            return false;
        }

        isChambered = false;
        OnFire?.Invoke();

        // 🔹 brak automatycznego blokowania zamka przy pustym magu!
        TryChamberFromMagazine();

        return true;
    }

    protected virtual void HandleBoltActionFire()
    {
        if (isChambered && bolt.IsBoltForward)
        {
            OnFire?.Invoke();
            isChambered = false;
        }
        else
        {
            OnDryFire?.Invoke();
        }
    }

    // -------------------------- ZAMEK --------------------------

    public virtual void OnBoltPulled()
    {
        if (isChambered)
        {
            isChambered = false;
            OnRoundEjected?.Invoke();
        }
    }

    public virtual void ReleaseBoltAction(bool force = false)
    {
        if (!force && !CanReleaseBolt()) return;

        TryChamberFromMagazine();
        isBoltLockedBack = false;
        OnBoltReleasedEvent?.Invoke();
    }

    public virtual bool TryChamberFromMagazine()
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

    public virtual bool CanReleaseBolt()
    {
        bool magExists = ammoSocket != null && ammoSocket.currentMagazine != null;
        bool magHasRounds = magExists && ammoSocket.currentMagazine.currentRounds > 0;
        return isBoltLockedBack && (!magExists || magHasRounds);
    }
}