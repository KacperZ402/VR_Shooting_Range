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
    [Tooltip("Czy po cofnięciu zamka wyrzuca pustą łuskę?")]
    public bool ejectOnPump = true;

    [Header("Logika Magazynka")]
    [Tooltip("Definiuje, kiedy collider magazynka (do ładowania) jest aktywny.")]
    public MagazineLoadType magazineLoadLogic = MagazineLoadType.PumpAction_LoadForward;

    [Tooltip("Referencja do magazynka (dla collidera)")]
    public Magazine magazine;

    private bool lastEnabledState;
    private Collider magCollider;

    protected override void Awake()
    {
        base.Awake(); // To już pobiera 'ammoPool'
        if (magazine != null)
            magCollider = magazine.GetComponent<Collider>();

        if (magCollider != null)
        {
            lastEnabledState = magCollider.enabled;
        }
    }

    protected override bool FireOnce()
    {
        if (isBoltLockedBack)
        {
            OnDryFire?.Invoke();
            return false;
        }
        if (chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f)
        {
            return false;
        }

        if (chamberedRound == null)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał!
        // TODO: Balistyka
        OnFire?.Invoke();

        // 🔹 ZMIANA: Zwracamy do puli
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound);

        chamberedRound = null;
        return true;
    }

    // Cofnięcie zamka / pompki
    public override void OnBoltPulled()
    {
        if (chamberedRound != null && ejectOnPump)
        {
            OnRoundEjected?.Invoke();
            // TODO: Wyrzucanie łuski

            // 🔹 ZMIANA: Zwracamy do puli
            if (ammoPool != null)
                ammoPool.ReturnRound(chamberedRound);
            else
                Destroy(chamberedRound);

            chamberedRound = null;
        }

        isBoltLockedBack = true;
    }

    // Zwolnienie zamka (pchnięcie pompki)
    public override void ReleaseBoltAction(bool force = false)
    {
        TryChamberFromMagazine();
        isBoltLockedBack = false;
        OnBoltReleasedEvent?.Invoke();
    }

    // 🔹 ZMIANA: Zaktualizowana logika ładowania
    public override bool TryChamberFromMagazine()
    {
        if (chamberedRound != null) return true;

        if (ammoSocket == null)
            return false;

        GameObject roundToChamber = ammoSocket.TryTakeRound();

        if (roundToChamber == null)
        {
            return false; // Pusty magazynek
        }

        Bullet bulletData = roundToChamber.GetComponent<Bullet>();
        if (bulletData == null)
        {
            Debug.LogError("Pobrany nabój nie ma komponentu 'Bullet'!", this);

            // 🔹 ZMIANA: Zwracamy zepsuty nabój do puli
            if (ammoPool != null)
                ammoPool.ReturnRound(roundToChamber);
            else
                Destroy(roundToChamber);

            return false;
        }

        if (bulletData.caliber != this.caliber)
        {
            Debug.LogWarning($"Próba załadowania złego kalibru! Broń: {this.caliber}, Nabój: {bulletData.caliber}", this);

            // 🔹 ZMIANA: Zwracamy zły nabój do puli
            if (ammoPool != null)
                ammoPool.ReturnRound(roundToChamber);
            else
                Destroy(roundToChamber);

            return false;
        }

        chamberedRound = roundToChamber;
        OnRoundChambered?.Invoke();
        return true;
    }

    // BoltAction fire
    protected override void HandleBoltActionFire()
    {
        if (chamberedRound != null && chargingHandle.transform.localPosition.y <= chargingHandle.minLocalY + 0.001f)
        {
            OnFire?.Invoke();

            // 🔹 ZMIANA: Zwracamy do puli
            if (ammoPool != null)
                ammoPool.ReturnRound(chamberedRound);
            else
                Destroy(chamberedRound);

            chamberedRound = null;
        }
        else
        {
            OnDryFire?.Invoke();
        }
    }

    // Logika Update pozostaje BEZ ZMIAN
    protected override void Update()
    {
        if (weaponGrab == null || !weaponGrab.IsGripHeld)
        {
            // 🔹 DODANA OPTYMALIZACJA: Sprzątanie collidera po upuszczeniu broni
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
                shouldBeEnabled = Mathf.Abs(currentY - chargingHandle.minLocalY) < 0.001f;
                break;

            case MagazineLoadType.BoltAction_LoadBack:
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