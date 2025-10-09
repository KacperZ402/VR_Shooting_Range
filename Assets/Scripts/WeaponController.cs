using UnityEngine;
using UnityEngine.Events;

public class WeaponController : MonoBehaviour
{

    public Animation anim;

    [Header("Referencje")]
    public AmmoSocket ammoSocket;
    public ChargingHandle chargingHandle;
    public BoltFollower bolt;

    [Header("Dane broni")]
    public string caliber = "5.56x45";

    [Header("Stan")]
    public bool isChambered = false;
    public bool isBoltLockedBack = false;

    [Header("Eventy")]
    public UnityEvent OnFire;
    public UnityEvent OnDryFire;
    public UnityEvent OnRoundChambered;
    public UnityEvent OnRoundEjected;
    public UnityEvent OnBoltLockedBack;
    public UnityEvent OnBoltReleasedEvent; // zmieniona nazwa eventu



    void Awake()
    {
        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.AddListener(OnBoltPulled);
            chargingHandle.OnBoltReleased.AddListener(OnBoltReleased);
        }
    }

    void OnDestroy()
    {
        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.RemoveListener(OnBoltPulled);
            chargingHandle.OnBoltReleased.RemoveListener(OnBoltReleased);
        }
    }

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

    public void OnBoltReleased()
    {
        if (!isBoltLockedBack)
        {
            OnBoltReleasedEvent?.Invoke();
            return;
        }

        bool magExists = (ammoSocket != null && ammoSocket.currentMagazine != null);
        bool magHasRounds = magExists && ammoSocket.currentMagazine.currentRounds > 0;

        if (!magExists)
        {
            isBoltLockedBack = false;
            OnBoltReleasedEvent?.Invoke();
        }
        else if (magHasRounds)
        {
            if (TryChamberFromMagazine())
            {
                isBoltLockedBack = false;
                OnBoltReleasedEvent?.Invoke();
            }
            else
            {
                isBoltLockedBack = true;
                OnBoltLockedBack?.Invoke();
            }
        }
        else
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
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

    public void FireSemiAuto()
    {
        if (isBoltLockedBack || !isChambered || !bolt.IsBoltForward)
        {
            OnDryFire?.Invoke();
            return;
        }

        isChambered = false;
        OnFire?.Invoke();

        if (!TryChamberFromMagazine() && ammoSocket != null)
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
    }

    public void ReleaseBolt()
    {
        OnBoltReleased();
    }
}