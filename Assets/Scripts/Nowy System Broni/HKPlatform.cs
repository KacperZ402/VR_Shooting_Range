using UnityEngine;

/// <summary>
/// Platforma HK: Automatycznie ładuje kolejny nabój.
/// Zamek nigdy nie blokuje się automatycznie.
/// Wersja zrefaktoryzowana (używa GetChamberedBulletData).
/// </summary>
public class HKPlatform : WeaponControllerBase
{
    // Awake() jest dziedziczone z WeaponControllerBase,
    // więc automatycznie pobiera 'ammoPool' i 'bulletPool'.

    protected override bool FireOnce()
    {
        // 1. Warunki wstępne
        if (isBoltLockedBack || !bolt.IsBoltForward)
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
        // 5. Logika specyficzna dla HK: Automatyczne przeładowanie
        TryChamberFromMagazine();

        return true;
    }

    public override void ReleaseBoltAction(bool force = false)
    {
        // Ta metoda jest już w 100% kompatybilna,
        // nie modyfikuje 'chamberedRound'.

        isBoltLockedBack = false;

        if (ammoSocket != null && ammoSocket.currentMagazine != null)
        {
            if (TryChamberFromMagazine())
            {
                Debug.Log("[HKPlatform] Nabój załadowany po zwolnieniu zamka.");
            }
            else
            {
                Debug.Log("[HKPlatform] Mag pusty — brak naboju do chamberowania.");
            }
        }

        OnBoltReleasedEvent?.Invoke();
    }
}