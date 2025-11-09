using UnityEngine;

/// <summary>
/// Platforma HK: Automatycznie ładuje kolejny nabój.
/// Zamek nigdy nie blokuje się automatycznie.
/// Wersja zaktualizowana do systemu prefabów nabojów.
/// </summary>
public class HKPlatform : WeaponControllerBase
{
    protected override bool FireOnce()
    {
        // 🔹 ZMIANA: Sprawdzamy prefab, nie bool
        if (!bolt.IsBoltForward || chamberedRound == null)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // TODO: Tutaj w przyszłości będzie logika balistyki
        OnFire?.Invoke();

        // 🔹 ZMIANA: Zużywamy prefab z komory
        chamberedRound = null;

        // Automatycznie ładuj następny (bez zmian)
        TryChamberFromMagazine();

        return true;
    }

    public override void ReleaseBoltAction(bool force = false)
    {
        // Ta metoda jest już w 100% kompatybilna z nowym systemem,
        // ponieważ opiera się na TryChamberFromMagazine() z klasy bazowej,
        // która poprawnie ustawia 'chamberedRoundPrefab'.

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