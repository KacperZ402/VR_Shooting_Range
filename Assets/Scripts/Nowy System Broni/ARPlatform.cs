using UnityEngine;

/// <summary>
/// Platforma AR: zamek blokuje się po pustym magazynku,
/// automatycznie chamberuje po strzale.
/// </summary>
public class ARPlatform : WeaponControllerBase
{
    protected override bool FireOnce()
    {
        if (!bolt.IsBoltForward || !isChambered)
        {
            OnDryFire?.Invoke();
            return false;
        }

        OnFire?.Invoke();
        isChambered = false;

        // 🔹 Próbuj chamberować z magazynka
        if (!TryChamberFromMagazine() && ammoSocket == null)
        {
            // 🔹 Jeśli pusto — blokuj zamek
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }

        return true;
    }

    public override void OnBoltPulled()
    {
        if (isChambered)
        {
            isChambered = false;
            OnRoundEjected?.Invoke();
        }

        // 🔹 AR blokuje się po pustym magazynku
        if (!TryChamberFromMagazine())
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
        else
        {
            isBoltLockedBack = false;
        }
    }
}
