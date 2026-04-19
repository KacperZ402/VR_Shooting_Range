using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum MagazineLoadType
{
    PumpAction_LoadForward, // Ładujesz, gdy pompka jest z przodu (zamknięta)
    BoltAction_LoadBack     // Ładujesz, gdy zamek jest otwarty
}

public class ShotgunPlatform : WeaponControllerBase
{
    [Header("Magazine")]
    public MagazineLoadType magazineLoadLogic = MagazineLoadType.PumpAction_LoadForward;

    [SerializeField] private MagazineLoader magLoader;
    private bool lastLoaderActiveState;

    protected override void Awake()
    {
        base.Awake();

        // Strzelba nie ma automatycznego zamka, więc bolt = null (chyba że masz hybrydę)
        bolt = null;

        // Auto-wyszukiwanie loadera
        if (magLoader == null)
            magLoader = GetComponentInChildren<MagazineLoader>(true);

        if (magLoader != null)
        {
            lastLoaderActiveState = magLoader.gameObject.activeSelf;
        }
    }

    protected override void Update()
    {
        if (!weaponGrab.IsGripHeld) return;
        base.Update();
        HandleMagazineLoaderLogic();
    }

    private void HandleMagazineLoaderLogic()
    {
        if (magLoader == null) return;

        bool shouldBeActive = false;

        // 🔹 Używamy flagi isBoltLockedBack zamiast pozycji fizycznej.
        // Flaga ta jest ustawiana przez eventy OnBoltPulled/Released w skrypcie pompki.
        switch (magazineLoadLogic)
        {
            case MagazineLoadType.PumpAction_LoadForward:
                // Można ładować tylko, gdy zamek jest zamknięty (Pompka z przodu)
                shouldBeActive = !isBoltLockedBack;
                break;

            case MagazineLoadType.BoltAction_LoadBack:
                // Można ładować tylko, gdy zamek jest otwarty (Pompka z tyłu)
                shouldBeActive = isBoltLockedBack;
                break;
        }

        if (shouldBeActive != lastLoaderActiveState)
        {
            magLoader.gameObject.SetActive(shouldBeActive);
            lastLoaderActiveState = shouldBeActive;
        }
    }

    protected override bool FireOnce()
    {
        // 🔹 NOWOŚĆ: Sprawdzamy flagę logiczną zamiast pozycji fizycznej.
        // Jeśli pompka jest odciągnięta do tyłu (isBoltLockedBack = true), nie można strzelić.
        if (isBoltLockedBack)
        {
            // Kliknięcie na sucho (opcjonalne, bo spust jest wtedy zazwyczaj luźny)
            // OnDryFire?.Invoke(); 
            return false;
        }

        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            // Brak naboju -> Dry Fire (klik)
            OnDryFire?.Invoke();
            return false;
        }

        // --- STRZAŁ ---
        SpawnProjectile(ammoData);

        if (ShootingRangeManager.Instance != null)
        {
            ShootingRangeManager.Instance.RegisterShot();
        }

        OnFire?.Invoke();

        // --- OBSŁUGA ŁUSKI (Shotgun Specific) ---
        // W strzelbie łuska nie wylatuje od razu po strzale (jak w pistolecie).
        // Zostaje w komorze jako "wystrzelona" (sama łuska) i czeka na ruch pompką.

        GameObject casingPrefab = ammoData.casingPrefab;

        // Usuwamy pełny nabój
        if (ammoPool != null) ammoPool.ReturnRound(chamberedRound);
        else Destroy(chamberedRound);

        chamberedRound = null;

        // Wstawiamy pustą łuskę do komory (SpawnAndChamberCasing to Twoja metoda z Base)
        // Zostanie ona wyrzucona dopiero, gdy gracz pociągnie pompkę (OnBoltPulled -> Eject).
        chamberedRound = SpawnAndChamberCasing(casingPrefab);

        // Pamiętaj: W strzelbie nie napinamy kurka automatycznie po strzale.
        // Kurek napnie się dopiero po przeładowaniu pompką.
        isHammerCocked = false;

        return true;
    }
}