using UnityEngine;

/// <summary>
/// Platforma HK (np. MP5, G3, HK416 z klasycznym handlem).
/// Zamek NIE blokuje się automatycznie po pustym magazynku.
/// Można go jednak ręcznie zablokować dźwignią (handle slot).
/// </summary>
public class HKPlatform : WeaponControllerBase
{
    protected override bool FireOnce()
    {
        // 🔹 Jeśli nie ma naboju lub zamek nie jest do przodu — klik (dry fire)
        if (!bolt.IsBoltForward || !isChambered)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // 🔹 Strzał
        OnFire?.Invoke();
        isChambered = false;

        // 🔹 HK NIE blokuje się automatycznie po pustym magazynku.
        //    Próba chamberowania z magazynka, jeśli jest.
        TryChamberFromMagazine();

        return true;
    }

    public override void OnBoltPulled()
    {
        // 🔹 Jeśli był nabój w komorze — wyrzuć
        if (isChambered)
        {
            isChambered = false;
            OnRoundEjected?.Invoke();
        }

        // 🔹 HK nie blokuje się automatycznie — ale handle może ustawić blokadę ręcznie
        // (np. ChargingHandle ma wewnętrzną logikę: jeśli user "zahaczy" o slot → zamek się blokuje)
        // Więc nie ruszamy isBoltLockedBack tutaj.

        // 🔹 Nie próbujemy chamberować teraz — dopiero po zwolnieniu handle
    }

    public override void ReleaseBoltAction(bool force = false)
    {
        isBoltLockedBack = false;
        // 🔹 Spróbuj chamberować, jeśli mag jest dostępny
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
