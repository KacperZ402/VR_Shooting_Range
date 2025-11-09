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

        // 🔹 ZMIANA: Sprawdzamy prefab, nie bool
        if (chamberedRound == null)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał
        OnFire?.Invoke();

        // 🔹 ZMIANA: Zużywamy prefab z komory
        chamberedRound = null;

        // Twoja oryginalna logika nie miała tutaj automatycznego przeładowania
        return true;
    }

    public override void OnBoltPulled()
    {
        // 🔹 ZMIANA: Sprawdzamy prefab
        if (chamberedRound != null)
        {
            // 🔹 ZMIANA: Wyrzucamy prefab z komory
            chamberedRound = null;
            OnRoundEjected?.Invoke();
        }

        // Jeśli mag pusty, ustaw stan blokady (do wizualnego efektu handle)
        // Ta logika jest poprawna, bo sprawdza stan magazynka
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
            // Pozostaje zablokowany, jeśli TryChamberFromMagazine się nie powiodło
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }

        OnBoltReleasedEvent?.Invoke();
    }
}