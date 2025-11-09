using UnityEngine;

/// <summary>
/// Platforma Pistoletu. Oryginalna logika zachowana.
/// Wersja zaktualizowana do systemu AmmoPoolManager.
/// </summary>
public class PistolPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake(); // To już pobiera 'ammoPool'

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

        if (chamberedRound == null)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał
        OnFire?.Invoke();

        // 🔹 ZMIANA: Zamiast niszczyć, zwracamy nabój do puli
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound); // Wyjście awaryjne

        chamberedRound = null;

        // Twoja oryginalna logika nie miała tutaj automatycznego przeładowania
        return true;
    }

    public override void OnBoltPulled()
    {
        if (chamberedRound != null)
        {
            // 🔹 ZMIANA: Zwracamy nabój do puli (jako niewystrzelony)
            if (ammoPool != null)
                ammoPool.ReturnRound(chamberedRound);
            else
                Destroy(chamberedRound); // Wyjście awaryjne

            chamberedRound = null;
            OnRoundEjected?.Invoke();
        }

        // Jeśli mag pusty, ustaw stan blokady (logika bez zmian)
        if (ammoSocket != null && ammoSocket.currentMagazine != null &&
            ammoSocket.currentMagazine.currentRounds == 0)
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
    }

    public override void ReleaseBoltAction(bool force = false)
    {
        // Ta logika jest już w 100% kompatybilna,
        // ponieważ opiera się na TryChamberFromMagazine() z klasy bazowej.

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