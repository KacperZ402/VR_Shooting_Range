using UnityEngine;

/// <summary>
/// Platforma AK: Zamek nigdy się nie blokuje.
/// Automatycznie wyrzuca łuskę i ładuje kolejny nabój po strzale.
/// Wersja klasyczna: Bez stagingu, natychmiastowy wyrzut.
/// </summary>
public class AKPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake();

        // Logika specyficzna dla AK: rączka nie blokuje się w tylnym położeniu
        // (chyba że manualnie zahaczysz o wycięcie, ale to robi ChargingHandle)
        if (chargingHandle != null)
            chargingHandle.lockOnFullPull = false;

        // AK nie używa osobnego BoltFollowera w standardowej logice
        bolt = null;
    }

    // Ta metoda jest kompatybilna z "klasycznym" podejściem
    public override void ReleaseBoltAction(bool force = false)
    {
        // W AK puszczenie zamka po prostu próbuje załadować nabój
        TryChamberFromMagazine();
        isBoltLockedBack = false; // Zawsze fałsz dla AK
        OnBoltReleasedEvent?.Invoke();
    }

    /// <summary>
    /// Logika pojedynczego strzału dla AK.
    /// </summary>
    protected override bool FireOnce()
    {
        // 1. Warunki wstępne (Rączka musi być z przodu)
        if (chargingHandle != null &&
           (chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f))
        {
            OnDryFire?.Invoke();
            return false;
        }

        // 2. Pobierz dane
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            return false; // Błąd obsłużony
        }

        // 3. Wystrzel pocisk
        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        // 4. 🔹 WAŻNE: Pobierz prefab łuski ZANIM zniszczymy nabój
        GameObject casingPrefab = ammoData.casingPrefab;

        // 5. Zwróć zużytą amunicję do puli
        // (Tutaj "usuwamy" cały nabój z komory, żeby zrobić miejsce na nowy)
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound);

        chamberedRound = null;

        // 6. 🔹 WAŻNE: Wyrzuć łuskę fizycznie
        // (To brakowało w Twoim kodzie - kałach musi sypać łuskami!)
        if (casingPool != null && casingPrefab != null)
        {
            GameObject casingInstance = casingPool.GetCasing(casingPrefab);
            PhysicallyEjectObject(casingInstance); // Wyrzuć z fizyką i pędem broni
        }

        // 7. Logika specyficzna dla AK: Automatyczne przeładowanie (System gazowy)
        TryChamberFromMagazine();

        return true;
    }

    // Nie musimy nadpisywać OnBoltPulled ani OnChargingHandleReleased.
    // Bazowa klasa WeaponControllerBase (w wersji klasycznej) robi to dobrze:
    // OnBoltPulled -> Wyrzuca to co w komorze (żywy nabój lub łuskę).
    // OnChargingHandleReleased -> Ładuje nowy nabój.
}