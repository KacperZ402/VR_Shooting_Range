using UnityEngine;

public class PistolPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake();

        // Pistolety nie mają bolta, tylko charging handle
        bolt = null;
    }

    protected override bool FireOnce()
    {
        // Jeśli handle nie w spoczynku — blokujemy strzał
        if (chargingHandle != null &&
            chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f)
        {
            return false;
        }

        if (!isChambered)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał
        OnFire?.Invoke();
        isChambered = false;

        return true;
    }

    public override void OnBoltPulled()
    {
        if (isChambered)
        {
            isChambered = false;
            OnRoundEjected?.Invoke();
        }

        // Jeśli mag pusty, ustaw stan blokady (do wizualnego efektu handle)
        if (ammoSocket != null && ammoSocket.currentMagazine != null &&
            ammoSocket.currentMagazine.currentRounds == 0)
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
    }

    public override void ReleaseBoltAction(bool force = false)
    {
        if (TryChamberFromMagazine())
        {
            isBoltLockedBack = false;
        }
        else
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }

        OnBoltReleasedEvent?.Invoke();
    }
}