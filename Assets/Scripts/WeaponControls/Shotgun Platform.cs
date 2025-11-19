using UnityEngine;

// Definicja typu ładowania magazynka (bez zmian)
public enum MagazineLoadType
{
    PumpAction_LoadForward,
    BoltAction_LoadBack
}

public class ShotgunPlatform : WeaponControllerBase
{
    [Header("Strzelba typu pump-action")]
    [Header("Logika Magazynka")]
    [Tooltip("Definiuje, kiedy collider magazynka (do ładowania) jest aktywny.")]
    public MagazineLoadType magazineLoadLogic = MagazineLoadType.PumpAction_LoadForward;

    [Tooltip("Referencja do magazynka (dla collidera)")]
    public Magazine magazine;

    private bool lastEnabledState;
    private Collider magCollider;

    protected override void Awake()
    {
        base.Awake(); // Pobiera menedżery

        // Strzelby nie mają osobnego rygla, używają pompki (ChargingHandle)
        bolt = null;

        if (magazine != null)
            magCollider = magazine.GetComponent<Collider>();

        if (magCollider != null)
        {
            lastEnabledState = magCollider.enabled;
        }
    }

    protected override bool FireOnce()
    {
        // 1. Warunki wstępne (Pompka musi być z przodu)
        if (chargingHandle != null &&
            chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f)
        {
            return false; // Niezaryglowana
        }

        // 2. Pobierz dane
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            return false; // Pusta komora
        }

        // 3. Wystrzel pocisk
        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        // 4. 🔹 PODMIANA NA ŁUSKĘ 🔹
        // W strzelbie po strzale łuska ZOSTAJE w komorze.
        GameObject casingPrefab = ammoData.casingPrefab;

        // Zwróć żywy nabój do puli
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound);

        chamberedRound = null;

        // Wstaw łuskę do komory (używając funkcji pomocniczej z klasy bazowej)
        // Ta łuska będzie siedzieć w komorze, dopóki gracz nie pociągnie pompki.
        chamberedRound = SpawnAndChamberCasing(casingPrefab);

        // Logika specyficzna dla strzelby: 
        // NIE wywołujemy tu TryChamberFromMagazine(). Czekamy na ruch pompki.
        return true;
    }

    // Nie musimy nadpisywać OnBoltPulled.
    // W klasie bazowej OnBoltPulled robi dokładnie to co trzeba: 
    // "PhysicallyEjectObject(chamberedRound)" -> Wyrzuca łuskę, którą wstawiliśmy w FireOnce.

    // -------------------------- LOGIKA COLLIDERA MAGAZYNKA --------------------------

    protected override void Update()
    {
        base.Update(); // WAŻNE: Zachowaj logikę fizyki łusek z bazy!

        if (weaponGrab == null || !weaponGrab.IsGripHeld)
        {
            if (magCollider != null && lastEnabledState == true)
            {
                magCollider.enabled = false;
                lastEnabledState = false;
            }
            return;
        }

        if (magCollider == null || chargingHandle == null)
        {
            return;
        }

        bool shouldBeEnabled = false;
        float currentY = chargingHandle.transform.localPosition.y;

        switch (magazineLoadLogic)
        {
            case MagazineLoadType.PumpAction_LoadForward:
                // Ładowanie możliwe tylko gdy pompka jest z przodu
                shouldBeEnabled = Mathf.Abs(currentY - chargingHandle.minLocalY) < 0.001f;
                break;

            case MagazineLoadType.BoltAction_LoadBack:
                // Ładowanie możliwe tylko gdy pompka jest z tyłu
                shouldBeEnabled = Mathf.Abs(currentY - chargingHandle.maxLocalY) < 0.001f;
                break;
        }

        if (shouldBeEnabled != lastEnabledState)
        {
            magCollider.enabled = shouldBeEnabled;
            lastEnabledState = shouldBeEnabled;
        }
    }
}