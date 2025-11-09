using UnityEngine;

/// <summary>
/// Platforma HK: Automatycznie ładuje kolejny nabój.
/// Zamek nigdy nie blokuje się automatycznie.
/// Wersja zaktualizowana do systemu AmmoPoolManager.
/// </summary>
public class HKPlatform : WeaponControllerBase
{
    // Awake() jest dziedziczone z WeaponControllerBase,
    // więc automatycznie pobiera referencję 'ammoPool'.

    protected override bool FireOnce()
    {
        if (!bolt.IsBoltForward || chamberedRound == null)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // TODO: Tutaj w przyszłości będzie logika balistyki
        OnFire?.Invoke();

        // 🔹 ZMIANA: Zamiast niszczyć, zwracamy nabój do puli
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound); // Wyjście awaryjne

        chamberedRound = null;

        // Automatycznie ładuj następny (bez zmian)
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