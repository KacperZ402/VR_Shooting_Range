using UnityEngine;

/// <summary>
/// Platforma Pistoletu. Używa SpawnProjectile.
/// Zachowuje oryginalną logikę (brak auto-chamber).
/// </summary>
public class PistolPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake();
        bolt = null; // Pistolety używają 'chargingHandle' jako zamka
    }

    protected override bool FireOnce()
    {
        // 1. Warunki wstępne (specyficzne dla pistoletu)
        if (chargingHandle != null &&
            chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f)
        {
            return false;
        }

        // 2. Pobierz dane (funkcja bazowa obsługuje błędy)
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            return false;
        }

        // 3. Wystrzel pocisk
        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        // 4. Zwróć zużytą amunicję do puli
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound);

        chamberedRound = null;

        // Logika specyficzna dla pistoletu: Brak automatycznego przeładowania
        return true;
    }

    public override void OnBoltPulled()
    {
        if (chamberedRound != null)
        {
            if (ammoPool != null)
                ammoPool.ReturnRound(chamberedRound);
            else
                Destroy(chamberedRound);
            chamberedRound = null;
            OnRoundEjected?.Invoke();
        }

        // Logika blokady zamka (Twoja z poprzedniej wersji)
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