using UnityEngine;

public class AKPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake();

        // AK: zamek nigdy nie blokuje się przy pustym magazynku
        if (chargingHandle != null)
            chargingHandle.lockOnFullPull = false;

        // AK: nie potrzebuje boltFollowera
        bolt = null;
    }

    /// <summary>
    /// AK: Odciągnięcie zamka wyrzuca nabój, ale zamek nie blokuje się
    /// </summary>
    public override void OnBoltPulled()
    {
        if (isChambered)
        {
            isChambered = false;
            OnRoundEjected?.Invoke();
        }

        // Nie blokujemy zamka, niezależnie od stanu magazynku
        isBoltLockedBack = false;
    }

    /// <summary>
    /// AK: Próba przerepetowania/załadowania nowego naboju
    /// </summary>
    public override void ReleaseBoltAction(bool force = false)
    {
        TryChamberFromMagazine();
        isBoltLockedBack = false;
        OnBoltReleasedEvent?.Invoke();
    }

    /// <summary>
    /// AK: FireOnce nie korzysta z bolta, tylko sprawdza czy jest nabój w komorze
    /// </summary>
    protected override bool FireOnce()
    {

        if (chargingHandle != null &&
           (chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f))
        {
            // opcjonalnie debug
            // Debug.Log("[AKPlatform] Strzał zablokowany – charging handle nie w spoczynku!");
            return false;
        }
        if (!isChambered)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał
        OnFire?.Invoke();
        isChambered = false;

        return true;
    }

}