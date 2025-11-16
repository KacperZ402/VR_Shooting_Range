using UnityEngine;

/// <summary>
/// Platforma AR: zamek blokuje się po pustym magazynku,
/// automatycznie chamberuje po strzale.
/// Wersja zrefaktoryzowana (używa SpawnProjectile).
/// </summary>
public class ARPlatform : WeaponControllerBase
{
    protected override bool FireOnce()
    {
        // 1. Warunki wstępne
        if (isBoltLockedBack || !bolt.IsBoltForward)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // 2. Pobierz dane (funkcja bazowa obsługuje błędy)
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            return false;
        }

        // 3. Wystrzel pocisk
        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        // 4. Zwróć zużytą amunicję do puli
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound);

        chamberedRound = null;

        // 5. Logika specyficzna dla AR: Blokada zamka
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

    // OnBoltPulled dziedziczy poprawną logikę z WeaponControllerBase
    // Ale jeśli chcesz zachować swoją specyficzną logikę blokowania zamka:
    public override void OnBoltPulled()
    {
        if (chamberedRound != null)
        {
            OnRoundEjected?.Invoke();
            if (ammoPool != null)
                ammoPool.ReturnRound(chamberedRound);
            else
                Destroy(chamberedRound);
            chamberedRound = null;
        }

        // Logika blokady zamka (Twoja z poprzedniej wersji)
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