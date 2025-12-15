using UnityEngine;

/// <summary>
/// Platforma AR: zamek blokuje się po pustym magazynku.
/// Poprawnie dziedziczy z nowej klasy bazowej.
/// </summary>
public class ARPlatform : WeaponControllerBase
{
    /// <summary>
    /// Strzał Semi-Auto / Auto.
    /// Nadpisane, aby dodać logikę blokady zamka AR.
    /// </summary>
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
            PhysicallyEjectObject(casingInstance); // Wyrzuć ją od razu
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
        // 🔹 KONIEC LOGIKI AR 🔹
        isHammerCocked = true;
        return true;
    }

    /// <summary>
    /// Puszczenie rączki zamka (dla AR).
    /// Nadpisane, aby zablokować zamek na pustym magazynku.
    /// </summary>
    protected override void OnChargingHandleReleased()
    {
        // 1. 🔹 LOGIKA SPECIFICZNA DLA AR 🔹
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

    // Nie ma potrzeby nadpisywania OnBoltPulled().
    // Wersja bazowa (wyrzuć fizycznie obiekt z komory) jest idealna dla AR.
}