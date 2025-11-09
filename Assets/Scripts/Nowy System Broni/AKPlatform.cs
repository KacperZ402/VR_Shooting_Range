using UnityEngine;

public class AKPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake();

        // Logika specyficzna dla AK: zamek nigdy nie blokuje się przy pustym magazynku
        if (chargingHandle != null)
            chargingHandle.lockOnFullPull = false;

        bolt = null;
    }
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
            // Rączka nie jest z przodu, potraktuj jak suchy strzał
            OnDryFire?.Invoke();
            return false;
        }

        // 🔹 ZMIANA: Sprawdzamy prefab, nie bool
        if (chamberedRound == null)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał
        // TODO: Tutaj w przyszłości będzie logika balistyki
        OnFire?.Invoke();
        chamberedRound = null;

        TryChamberFromMagazine();

        return true;
    }
}