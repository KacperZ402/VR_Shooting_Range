using UnityEngine;

/// <summary>
/// Platforma AK: Zamek nigdy się nie blokuje.
/// Automatycznie ładuje kolejny nabój po strzale.
/// Wersja zaktualizowana do systemu AmmoPoolManager.
/// </summary>
public class AKPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake(); // To już pobiera 'ammoPool'

        // Logika specyficzna dla AK: zamek nigdy nie blokuje się przy pustym magazynku
        if (chargingHandle != null)
            chargingHandle.lockOnFullPull = false;

        bolt = null;
    }

    // Ta metoda jest już kompatybilna, nie rusza nabojów
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
        // Specyficzna dla AK weryfikacja: sprawdź, czy rączka zamka jest z przodu
        if (chargingHandle != null &&
           (chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f))
        {
            OnDryFire?.Invoke();
            return false;
        }

        if (chamberedRound == null)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał
        // TODO: Tutaj w przyszłości będzie logika balistyki
        OnFire?.Invoke();

        // 🔹 ZMIANA: Zamiast niszczyć, zwracamy nabój do puli
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound); // Wyjście awaryjne

        chamberedRound = null;

        // Automatyczne przeładowanie (bez zmian)
        TryChamberFromMagazine();

        return true;
    }
}