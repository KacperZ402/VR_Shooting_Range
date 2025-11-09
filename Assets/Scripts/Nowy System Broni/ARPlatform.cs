using UnityEngine;

/// <summary>
/// Platforma AR: zamek blokuje się po pustym magazynku,
/// automatycznie chamberuje po strzale.
/// Wersja zaktualizowana do systemu AmmoPoolManager.
/// </summary>
public class ARPlatform : WeaponControllerBase
{
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

        // --- Logika blokady zamka (bez zmian) ---
        bool didChamber = TryChamberFromMagazine();
        bool magExists = (ammoSocket != null && ammoSocket.currentMagazine != null);
        bool magIsEmpty = magExists && ammoSocket.currentMagazine.currentRounds == 0;

        if (!didChamber && magIsEmpty)
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }

        return true;
    }

    public override void OnBoltPulled()
    {
        if (chamberedRound != null)
        {
            // TODO: Tutaj w przyszłości będzie wyrzucanie łuski
            OnRoundEjected?.Invoke();

            // 🔹 ZMIANA: Zamiast niszczyć, zwracamy nabój do puli
            if (ammoPool != null)
                ammoPool.ReturnRound(chamberedRound);
            else
                Destroy(chamberedRound); // Wyjście awaryjne

            chamberedRound = null;
        }

        // --- Logika blokady zamka (bez zmian) ---
        bool didChamber = TryChamberFromMagazine();
        bool magExists = (ammoSocket != null && ammoSocket.currentMagazine != null);
        bool magIsEmpty = magExists && ammoSocket.currentMagazine.currentRounds == 0;

        if (!didChamber && magIsEmpty)
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