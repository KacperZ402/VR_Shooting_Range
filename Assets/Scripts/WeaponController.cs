using UnityEngine;
using UnityEngine.Events;

public class WeaponController : MonoBehaviour
{
    public Animation anim;

    [Header("Referencje")]
    public AmmoSocket ammoSocket;
    public ChargingHandle chargingHandle;
    public BoltFollower bolt;

    [Header("Dane broni")]
    public string caliber = "5.56x45";

    [Header("Stan")]
    public bool isChambered = false;
    public bool isBoltLockedBack = false;

    [Header("Eventy")]
    public UnityEvent OnFire;
    public UnityEvent OnDryFire;
    public UnityEvent OnRoundChambered;
    public UnityEvent OnRoundEjected;
    public UnityEvent OnBoltLockedBack;
    public UnityEvent OnBoltReleasedEvent;

    void Awake()
    {
        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.AddListener(OnBoltPulled);
            // zamiast OnBoltReleased() -> bezpośrednio wywołujemy ReleaseBoltAction
            chargingHandle.OnBoltReleased.AddListener(() => ReleaseBoltAction(false));
        }
    }

    void OnDestroy()
    {
        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.RemoveListener(OnBoltPulled);
            chargingHandle.OnBoltReleased.RemoveAllListeners();
        }
    }

    public void OnBoltPulled()
    {
        // jeśli był nabój w komorze -> wyrzucamy go
        if (isChambered)
        {
            isChambered = false;
            OnRoundEjected?.Invoke();
        }

        // jeśli magazynek pusty -> zamek blokuje się z tyłu
        if (!TryChamberFromMagazine())
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
        else
        {
            isBoltLockedBack = false;
        }
    }

    /// <summary>
    /// Sprawdza, czy zamek może zostać zwolniony.
    /// </summary>
    public bool CanReleaseBolt()
    {
        bool magExists = ammoSocket != null && ammoSocket.currentMagazine != null;
        bool magHasRounds = magExists && ammoSocket.currentMagazine.currentRounds > 0;

        // Można zrzucić zamek jeśli:
        // - zamek jest zablokowany
        // - i jest magazynek z nabojami lub nie ma magazynka wcale
        return isBoltLockedBack && (!magExists || magHasRounds);
    }

    /// <summary>
    /// Wykonuje zwolnienie zamka — niezależnie od warunków jeśli force = true.
    /// </summary>
    public void ReleaseBoltAction(bool force = false)
    {
        if (!force && !CanReleaseBolt())
            return;

        // próba pobrania naboju
        if (TryChamberFromMagazine())
            isBoltLockedBack = false;
        else
            isBoltLockedBack = false; // zamek zawsze wraca do przodu, nawet jeśli pusto

        OnBoltReleasedEvent?.Invoke();
    }

    public bool TryChamberFromMagazine()
    {
        if (ammoSocket == null || ammoSocket.currentMagazine == null)
            return false;

        if (ammoSocket.currentMagazine.caliber != caliber)
            return false;

        if (ammoSocket.TryTakeRound())
        {
            isChambered = true;
            OnRoundChambered?.Invoke();
            return true;
        }

        return false;
    }

    public void FireSemiAuto()
    {
        // zamek musi być w pozycji przedniej i nabój musi być w komorze
        if (isBoltLockedBack || !isChambered || !bolt.IsBoltForward)
        {
            OnDryFire?.Invoke();
            return;
        }

        // strzał
        isChambered = false;
        OnFire?.Invoke();

        // próba automatycznego doładowania
        if (!TryChamberFromMagazine() && ammoSocket != null)
        {
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();
        }
    }
}
