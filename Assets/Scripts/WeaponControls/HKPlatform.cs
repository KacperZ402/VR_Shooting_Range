using UnityEngine;

/// <summary>
/// Platforma HK (MP5, G3, itp.): 
/// - Automatycznie wyrzuca łuskę i ładuje kolejny nabój po strzale.
/// - Zamek NIGDY nie blokuje się automatycznie po ostatnim strzale (tylko ręcznie).
/// Wersja dostosowana do systemu 'Klasycznego' (z wyrzutem łusek).
/// </summary>
public class HKPlatform : WeaponControllerBase
{
    // Awake jest dziedziczone, więc setup menedżerów dzieje się automatycznie.

    protected override bool FireOnce()
    {
        // 1. Warunki wstępne
        // Sprawdzamy, czy zamek nie jest zablokowany w tylnym położeniu (HK Slap notch)
        // oraz czy rygiel jest z przodu.
        if (isBoltLockedBack || !bolt.IsBoltForward)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // 2. Pobierz dane
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            return false; // Błąd obsłużony (pusta komora)
        }

        // 3. Wystrzel pocisk
        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        // 4. 🔹 WAŻNE: Pobierz prefab łuski ZANIM zwrócimy nabój do puli
        GameObject casingPrefab = ammoData.casingPrefab;

        // 5. Zwróć zużytą amunicję (nabój) do puli
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound);

        chamberedRound = null;

        // 6. 🔹 WAŻNE: Wyrzuć łuskę fizycznie
        // HK słyną z energicznego wyrzutu, więc używamy PhysicallyEjectObject
        if (casingPool != null && casingPrefab != null)
        {
            GameObject casingInstance = casingPool.GetCasing(casingPrefab);
            PhysicallyEjectObject(casingInstance);
        }

        // 7. Logika specyficzna dla HK: Natychmiastowe przeładowanie
        // HK nie blokuje się na pustym, więc zawsze próbujemy załadować.
        // Jeśli magazynek jest pusty, komora po prostu zostanie pusta (klik przy następnym strzale).
        TryChamberFromMagazine();

        return true;
    }

    // UWAGA: Usunąłem nadpisanie ReleaseBoltAction().
    // Logika w nowym WeaponControllerBase jest identyczna i idealna dla HK:
    // - isBoltLockedBack = false
    // - TryChamberFromMagazine()
    // - OnBoltReleasedEvent
    // To obsłuży "HK Slap" automatycznie.
}