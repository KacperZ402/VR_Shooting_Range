using UnityEngine;

/// <summary>
/// Platforma AR: zamek blokuje się po pustym magazynku,
/// automatycznie chamberuje po strzale.
/// Wersja zaktualizowana do systemu prefabów nabojów.
/// ZMODYFIKOWANA LOGIKA OnBoltPulled.
/// </summary>
public class ARPlatform : WeaponControllerBase
{
    protected override bool FireOnce()
    {
        // Ta logika jest poprawna (blokuje tylko z pustym magazynkiem)
        if (!bolt.IsBoltForward || chamberedRound == null)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // TODO: Tutaj w przyszłości będzie logika balistyki
        OnFire?.Invoke();

        chamberedRound = null;

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
            chamberedRound = null;
            OnRoundEjected?.Invoke();
            // TODO: Tutaj w przyszłości będzie wyrzucanie łuski
        }

        bool didChamber = TryChamberFromMagazine();

        bool magExists = (ammoSocket != null && ammoSocket.currentMagazine != null);

        bool magIsEmpty = magExists && ammoSocket.currentMagazine.currentRounds == 0;

        // Jeśli ładowanie się nie powiodło ORAZ jest włożony pusty magazynek
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