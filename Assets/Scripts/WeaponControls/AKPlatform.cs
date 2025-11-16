using UnityEngine;

/// <summary>
/// Platforma AK: Zamek nigdy się nie blokuje.
/// Automatycznie ładuje kolejny nabój po strzale.
/// Wersja zrefaktoryzowana (używa GetChamberedBulletData).
/// </summary>
public class AKPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake(); // To już pobiera 'ammoPool' i 'bulletPool'

        // Logika specyficzna dla AK
        if (chargingHandle != null)
            chargingHandle.lockOnFullPull = false;
        bolt = null;
    }

    // Ta metoda jest już kompatybilna
    public override void ReleaseBoltAction(bool force = false)
    {
        TryChamberFromMagazine();
        isBoltLockedBack = false; // Zawsze fałsz dla AK
        OnBoltReleasedEvent?.Invoke();
    }

    /// <summary>
    /// Logika pojedynczego strzału dla AK.
    /// </summary>
    protected override bool FireOnce()
    {
        // 1. Warunki wstępne (specyficzne dla AK)
        if (chargingHandle != null &&
           (chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f))
        {
            OnDryFire?.Invoke();
            return false;
        }

        // 2. 🔹 UPROSZCZENIE: Pobierz dane (funkcja bazowa obsługuje błędy)
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            return false; // Błąd został już obsłużony
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

        // 5. Logika specyficzna dla AK: Automatyczne przeładowanie
        TryChamberFromMagazine();

        return true;
    }
}