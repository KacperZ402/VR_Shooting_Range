using UnityEngine;

// Definicja typu ładowania magazynka (bez zmian)
public enum MagazineLoadType
{
    PumpAction_LoadForward, // Collider aktywny, gdy pompka jest z przodu (minLocalY)
    BoltAction_LoadBack     // Collider aktywny, gdy zamek jest z tyłu (maxLocalY)
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
    public Magazine magazine; // Używane do włączania/wyłączania collidera ładowania

    private bool lastEnabledState;
    private Collider magCollider;

    protected override void Awake()
    {
        base.Awake();
        if (magazine != null)
            magCollider = magazine.GetComponent<Collider>();

        // Ustawiamy stan początkowy, aby uniknąć "błysku" collidera
        if (magCollider != null)
        {
            lastEnabledState = magCollider.enabled;
        }
    }

    protected override bool FireOnce()
    {
        // Nie strzela, jeśli zamek jest zablokowany z tyłu
        if (isBoltLockedBack)
        {
            OnDryFire?.Invoke();
            return false;
        }
        if (chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f)
        {
            return false;
        }

        // 🔹 ZMIANA: Sprawdzamy prefab, nie bool
        if (chamberedRound == null)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał!
        // TODO: Tutaj w przyszłości będzie logika balistyki
        OnFire?.Invoke();

        // 🔹 ZMIANA: Zużywamy prefab z komory
        chamberedRound = null;

        // Strzelba nie chamberuje automatycznie — wymaga manualnego przeładowania
        return true;
    }

    // Cofnięcie zamka / pompki (ręczne przeładowanie)
    public override void OnBoltPulled()
    {
        // 🔹 ZMIANA: Sprawdzamy prefab
        if (chamberedRound != null && ejectOnPump)
        {
            OnRoundEjected?.Invoke();
            // 🔹 ZMIANA: Wyrzucamy prefab z komory
            chamberedRound = null;
            // TODO: Wyrzucanie łuski
        }

        // Strzelba nie ma klasycznego “bolt locked back” — zamek po prostu cofa się
        isBoltLockedBack = true;
    }

    // Zwolnienie zamka (pchnięcie pompki)
    public override void ReleaseBoltAction(bool force = false)
    {
        // Po pchnięciu pompki próbujemy załadować nowy nabój z magazynka
        TryChamberFromMagazine();

        // W każdej sytuacji zamek wraca do przodu
        isBoltLockedBack = false;

        OnBoltReleasedEvent?.Invoke();
    }

    // 🔹 ZMIANA: Zastąpiono całą metodę nową logiką z WeaponControllerBase
    /// <summary>
    /// Załadowanie naboju z magazynka (teraz pobiera prefab)
    /// </summary>
    public override bool TryChamberFromMagazine()
    {
        // Jeśli komora jest już pełna, nie rób nic
        if (chamberedRound != null) return true;

        if (ammoSocket == null)
            return false;

        // Pobieramy prefab naboju z gniazda
        GameObject roundToChamber = ammoSocket.TryTakeRound();

        if (roundToChamber == null)
        {
            return false; // Pusty magazynek
        }

        // Sprawdzamy kaliber pobranego naboju
        Bullet bulletData = roundToChamber.GetComponent<Bullet>();
        if (bulletData == null)
        {
            Debug.LogError("Pobrany nabój nie ma komponentu 'Bullet'!", this);
            return false;
        }

        if (bulletData.caliber != this.caliber)
        {
            Debug.LogWarning($"Próba załadowania złego kalibru! Broń: {this.caliber}, Nabój: {bulletData.caliber}", this);
            // TODO: Zacięcie
            return false;
        }

        // Wszystko się zgadza - ładujemy prefab do komory
        chamberedRound = roundToChamber;
        OnRoundChambered?.Invoke();
        return true;
    }

    // 🔫 BoltAction fire — używamy bez zmian, ale nadpisujemy, żeby było czytelne
    protected override void HandleBoltActionFire()
    {
        // 🔹 ZMIANA: Sprawdzamy prefab
        if (chamberedRound != null && chargingHandle.transform.localPosition.y <= chargingHandle.minLocalY + 0.001f)
        {
            OnFire?.Invoke();
            // 🔹 ZMIANA: Zużywamy prefab
            chamberedRound = null;
        }
        else
        {
            OnDryFire?.Invoke();
        }
    }
    protected override void Update()
    {

        if (weaponGrab == null || !weaponGrab.IsGripHeld)
        {
            return;
        }
        if (magCollider == null || chargingHandle == null)
        {
            return;
        }

        bool shouldBeEnabled = false;
        float currentY = chargingHandle.transform.localPosition.y;

        // Sprawdzamy, którą logikę mamy zastosować
        switch (magazineLoadLogic)
        {
            // Logika dla strzelby: Włącz collider, gdy pompka jest z przodu
            case MagazineLoadType.PumpAction_LoadForward:
                shouldBeEnabled = Mathf.Abs(currentY - chargingHandle.minLocalY) < 0.001f;
                break;

            // Logika dla 4-taktów: Włącz collider, gdy zamek jest z tyłu
            case MagazineLoadType.BoltAction_LoadBack:
                shouldBeEnabled = Mathf.Abs(currentY - chargingHandle.maxLocalY) < 0.001f;
                break;
        }

        // Zastosuj zmianę (tylko jeśli stan się zmienił, dla optymalizacji)
        if (shouldBeEnabled != lastEnabledState)
        {
            magCollider.enabled = shouldBeEnabled;
            lastEnabledState = shouldBeEnabled;
        }
    }
}