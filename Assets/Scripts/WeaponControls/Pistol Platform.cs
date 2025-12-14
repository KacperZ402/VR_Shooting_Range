using UnityEngine;
using System.Collections;

/// <summary>
/// Platforma Pistoletu.
/// - Automatycznie wyrzuca łuskę.
/// - Blokuje zamek (Slide Lock) po ostatnim strzale.
/// - Blokuje zamek przy ręcznym przeładowaniu TYLKO jeśli nie udało się załadować naboju.
/// </summary>
public class PistolPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake();
        // Pistolety nie używają osobnego rygla (BoltFollower), 
        // całą robotę robi ChargingHandle (zamek/suwadło).
        bolt = null;
    }

    /// <summary>
    /// Logika strzału pistoletu.
    /// </summary>
    protected override bool FireOnce()
    {
        // 1. Warunki wstępne
        if (chargingHandle != null && chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f) return false;

        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null) return false;

        // 2. Strzał
        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        // 3. Wyrzut łuski
        GameObject casingPrefab = ammoData.casingPrefab;

        if (ammoPool != null) ammoPool.ReturnRound(chamberedRound);
        else Destroy(chamberedRound);
        chamberedRound = null;

        if (casingPool != null && casingPrefab != null)
        {
            GameObject casingInstance = casingPool.GetCasing(casingPrefab);
            PhysicallyEjectObject(casingInstance);
        }

        // 4. Przeładowanie i Blokada po strzale
        bool didChamber = TryChamberFromMagazine();

        bool magExists = (ammoSocket != null && ammoSocket.currentMagazine != null);
        bool magIsEmpty = magExists && ammoSocket.currentMagazine.currentRounds == 0;

        // Jeśli nie udało się załadować I magazynek pusty -> Zablokuj
        if (!didChamber && magIsEmpty)
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// Obsługa puszczenia zamka ręką.
    /// Poprawiona logika: Najpierw próbuje załadować, potem decyduje czy blokować.
    /// </summary>
    protected override void OnChargingHandleReleased()
    {
        // 1. Najpierw próbujemy załadować nabój z magazynka
        // (To jest kluczowe - robimy to PRZED sprawdzeniem czy blokować)
        bool didChamber = TryChamberFromMagazine();

        // 2. Sprawdzamy stan magazynka PO próbie pobrania naboju
        bool magExists = (ammoSocket != null && ammoSocket.currentMagazine != null);
        bool magIsEmpty = magExists && ammoSocket.currentMagazine.currentRounds == 0;

        // 3. Logika blokady:
        // Blokujemy TYLKO WTEDY, gdy:
        // A. Nie udało się załadować naboju (didChamber == false)
        // B. ORAZ magazynek jest pusty (magIsEmpty == true)
        if (!didChamber && magIsEmpty)
        {
            // Slide Lock (zamek zostaje w tyle)
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
        else
        {
            // W każdym innym przypadku (załadowano nabój ALBO brak magazynka)
            // Zamek wraca do przodu
            isBoltLockedBack = false;
            OnBoltReleasedEvent?.Invoke();
        }
    }
}