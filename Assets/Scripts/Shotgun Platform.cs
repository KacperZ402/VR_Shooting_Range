using UnityEngine;

public class ShotgunPlatform : WeaponControllerBase
{
    [Header("Strzelba typu pump-action")]
    [Tooltip("Czy po cofnięciu zamka wyrzuca pustą łuskę?")]
    public bool ejectOnPump = true;
    public Magazine magazine;

    protected override void Awake()
    {
        base.Awake();
        if (magazine != null)
            magCollider = magazine.GetComponent<Collider>();
    }

    protected override bool FireOnce()
    {
        // Nie strzela, jeśli zamek jest zablokowany z tyłu
        if (isBoltLockedBack)
        {
            OnDryFire?.Invoke();
            return false;
        }
        if(chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f)
        {
            return false;
        }
        // Jeśli brak naboju w komorze → pusty strzał
        if (!isChambered)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał!
        OnFire?.Invoke();
        isChambered = false;

        // Strzelba nie chamberuje automatycznie — wymaga manualnego przeładowania
        return true;
    }

    // Cofnięcie zamka / pompki (ręczne przeładowanie)
    public override void OnBoltPulled()
    {
        // Jeśli był nabój w komorze → wyrzucamy łuskę
        if (isChambered && ejectOnPump)
        {
            OnRoundEjected?.Invoke();
            isChambered = false;
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

    //Załadowanie naboju z magazynka
    public override bool TryChamberFromMagazine()
    {
        if (ammoSocket == null || ammoSocket.currentMagazine == null)
            return false;

        var mag = ammoSocket.currentMagazine;

        if (mag.caliber != caliber || mag.currentRounds <= 0)
            return false;

        // Pobieramy jeden nabój z magazynka
        if (ammoSocket.TryTakeRound())
        {
            isChambered = true;
            OnRoundChambered?.Invoke();
            return true;
        }

        return false;
    }

    // 🔫 BoltAction fire — używamy bez zmian, ale nadpisujemy, żeby było czytelne
    protected override void HandleBoltActionFire()
    {
        if (isChambered && chargingHandle.transform.localPosition.y <= chargingHandle.minLocalY + 0.001f)
        {
            OnFire?.Invoke();
            isChambered = false;
        }
        else
        {
            OnDryFire?.Invoke();
        }
    }
    private bool lastEnabledState;
    private Collider magCollider;

    void Update()
    {
        bool shouldBeEnabled = Mathf.Abs(chargingHandle.transform.localPosition.y - chargingHandle.minLocalY) < 0.001f;
        if (shouldBeEnabled != lastEnabledState)
        {
            magCollider.enabled = shouldBeEnabled;
            lastEnabledState = shouldBeEnabled;
        }
    }
}