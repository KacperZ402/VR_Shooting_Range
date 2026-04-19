using UnityEngine;
public class ARPlatform : WeaponControllerBase
{
    protected override bool FireOnce()
    {
        // 1. Sprawdzenie warunków (z bazy)
        if ((isBoltLockedBack || !bolt.IsBoltForward) && isHammerCocked)
        {
            OnDryFire?.Invoke();
            return false;
        }
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            return false; // Błąd obsłużony w GetChamberedBulletData
        }
        // 2. Strzał (z bazy)
        SpawnProjectile(ammoData);

        if (ShootingRangeManager.Instance != null)
        {
            ShootingRangeManager.Instance.RegisterShot();
        }

        OnFire?.Invoke();
        GameObject casingPrefab = ammoData.casingPrefab;
        // 3. Zwróć nabój do puli (z bazy)
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound);

        chamberedRound = null;
        // 4. Wyrzuć łuskę (z bazy)
        if (casingPool != null && casingPrefab != null)
        {
            GameObject casingInstance = casingPool.GetCasing(casingPrefab);
            PhysicallyEjectObject(casingInstance);
        }
        // 5. Załaduj nowy nabój (z bazy)
        bool didChamber = TryChamberFromMagazine();
        // 6. 🔹 LOGIKA SPECIFICZNA DLA AR 🔹
        // Sprawdź, czy zamek powinien się zablokować PO strzale
        bool magExists = (ammoSocket != null && ammoSocket.currentMagazine != null);
        bool magIsEmpty = magExists && ammoSocket.currentMagazine.currentRounds == 0;

        if (!didChamber && magIsEmpty)
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
        isHammerCocked = true;
        return true;
    }
    /// <summary>
    /// Puszczenie rączki zamka (dla AR).
    /// Nadpisane, aby zablokować zamek na pustym magazynku.
    /// </summary>
    protected override void OnChargingHandleReleased()
    {
        // Sprawdź, czy zamek powinien się zablokować
        bool magExists = (ammoSocket != null && ammoSocket.currentMagazine != null);
        bool magIsEmpty = magExists && ammoSocket.currentMagazine.currentRounds == 0;

        if (magIsEmpty)
        {
            // Magazynek pusty -> zablokuj zamek
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
            return; // Nie próbuj ładować
        }

        // 2. 🔹 LOGIKA BAZOWA 🔹
        // Magazynek ma naboje -> wykonaj domyślną akcję (zwolnij zamek i załaduj)
        base.OnChargingHandleReleased();
    }
}