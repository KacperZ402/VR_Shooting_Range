using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponControllerBase : MonoBehaviour
{
    [Header("Referencje")]
    public AmmoSocket ammoSocket;
    public ChargingHandle chargingHandle;
    public BoltFollower bolt;
    public FireSelectorSimple fireSelector;
    public WeaponGrabInteractable weaponGrab;

    [Header("Dane broni")]
    public string caliber = "5.56x45";

    [Header("Stan broni")]

    // 🔹 ZMIANA: Przechowujemy instancję naboju, nie prefab
    [Tooltip("Instancja naboju aktualnie w komorze")]
    public GameObject chamberedRound;

    public bool isBoltLockedBack = false;
    public FireMode currentFireMode = FireMode.Safe;

    [Header("Parametry trybów ognia")]
    // ... (reszta pól bez zmian) ...
    public int burstCount = 3;
    public float fireRate = 0.1f;
    public float burstDelay = 0.08f;

    [Header("Eventy")]
    // ... (eventy bez zmian) ...
    public UnityEvent OnFire;
    public UnityEvent OnDryFire;
    public UnityEvent OnRoundChambered;
    public UnityEvent OnRoundEjected;
    public UnityEvent OnBoltLockedBack;
    public UnityEvent OnBoltReleasedEvent;

    protected float lastFireTime;
    protected bool triggerPressed;
    protected int burstShotsRemaining = 0;

    protected AmmoPoolManager ammoPool;

    protected virtual void Awake()
    {
        ammoPool = AmmoPoolManager.Instance; // Pobierz Singleton
        if (ammoPool == null)
        {
            Debug.LogError("AmmoPoolManager nie znaleziony! Broń nie będzie zwracać nabojów do puli.", this);
        }

        if (fireSelector != null)
            fireSelector.weaponController = this;

        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.AddListener(OnBoltPulled);
            chargingHandle.OnBoltReleased.AddListener(() => ReleaseBoltAction(false));
        }

        if (weaponGrab != null)
            weaponGrab.weaponController = this;
    }

    protected virtual void Update()
    {
        HandleBurstLogic();
        HandleAutoFire();
    }

    // -------------------------- STRZAŁ (logika inputu - bez zmian) --------------------------

    public virtual void FireInput(bool pressed)
    {
        if (weaponGrab != null && !weaponGrab.IsGripHeld) return;
        // ... (reszta logiki inputu bez zmian) ...
        switch (currentFireMode)
        {
            case FireMode.Safe:
                break;
            case FireMode.Semi:
                if (pressed && !triggerPressed)
                    FireOnce();
                break;
            case FireMode.Burst:
                if (pressed && !triggerPressed && burstShotsRemaining == 0)
                    burstShotsRemaining = burstCount;
                break;
            case FireMode.BoltAction:
                if (pressed && !triggerPressed)
                    HandleBoltActionFire();
                break;
            case FireMode.Auto:
                break;
        }
        triggerPressed = pressed;
    }

    protected virtual void HandleBurstLogic()
    {
        if (currentFireMode != FireMode.Burst) return;
        if (burstShotsRemaining <= 0) return;

        if (Time.time - lastFireTime >= burstDelay)
        {
            if (FireOnce())
            {
                burstShotsRemaining--;
                lastFireTime = Time.time;
            }
            else
            {
                burstShotsRemaining = 0;
            }
        }
    }

    protected virtual void HandleAutoFire()
    {
        if (currentFireMode != FireMode.Auto) return;
        if (!triggerPressed) return;

        if (Time.time >= lastFireTime + fireRate)
        {
            if (FireOnce())
                lastFireTime = Time.time;
        }
    }


    // -------------------------- 🔹 LOGIKA STRZAŁU (ZMIENIONA) --------------------------

    protected virtual bool FireOnce()
    {
        if (isBoltLockedBack || chamberedRound == null || !bolt.IsBoltForward)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // TODO: BALISTYKA
        // Tutaj odczytujemy dane z 'chamberedRound.GetComponent<Bullet>()'

        OnFire?.Invoke();

        // 🔹 ZMIANA: Zamiast Destroy(), zwracamy do puli
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound); // Wyjście awaryjne, jeśli pula nie działa

        chamberedRound = null;

        TryChamberFromMagazine();
        return true;
    }

    protected virtual void HandleBoltActionFire()
    {
        if (chamberedRound != null && bolt.IsBoltForward)
        {
            OnFire?.Invoke();

            // 🔹 ZMIANA: Zamiast Destroy(), zwracamy do puli
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

    // -------------------------- 🔹 ZAMEK (ZMIENIONY) --------------------------

    public virtual void OnBoltPulled()
    {
        if (chamberedRound != null)
        {
            // TODO: WYRZUCANIE ŁUSKI
            // W przyszłości 'ReturnRound' zamienimy na 'EjectCasing(chamberedRound)'

            OnRoundEjected?.Invoke();

            // 🔹 ZMIANA: Zamiast Destroy(), zwracamy do puli (jako niewystrzelony nabój)
            if (ammoPool != null)
                ammoPool.ReturnRound(chamberedRound);
            else
                Destroy(chamberedRound);

            chamberedRound = null;
        }
    }

    public virtual void ReleaseBoltAction(bool force = false)
    {
        if (!force && !CanReleaseBolt()) return;

        TryChamberFromMagazine();
        isBoltLockedBack = false;
        OnBoltReleasedEvent?.Invoke();
    }

    // 🔹 ZMIANA: Logika ładowania komory
    public virtual bool TryChamberFromMagazine()
    {
        // Jeśli komora jest już pełna, nie rób nic
        if (chamberedRound != null) return true;

        if (ammoSocket == null)
            return false;

        // 🔹 ZMIANA: Pobieramy INSTANCJĘ naboju z gniazda
        GameObject roundToChamber = ammoSocket.TryTakeRound();

        if (roundToChamber == null)
        {
            Debug.Log("Magazynek Jest pusty, podczas próby załadowania naboju");
            return false; // Pusty magazynek
        }

        // Sprawdzamy kaliber pobranej INSTANCJI
        Bullet bulletData = roundToChamber.GetComponent<Bullet>();
        if (bulletData == null)
        {
            Debug.LogError("Pobrany nabój (instancja) nie ma komponentu 'Bullet'!", this);
            Destroy(roundToChamber); // Zniszcz zły obiekt
            return false;
        }

        if (bulletData.caliber != this.caliber)
        {
            Debug.LogWarning($"Próba załadowania złego kalibru! Broń: {this.caliber}, Nabój: {bulletData.caliber}", this);
            // TODO: Co zrobić ze złym nabojem? Na razie go niszczymy.
            Destroy(roundToChamber);
            return false;
        }
        chamberedRound = roundToChamber;
        // Na razie instancja pozostaje nieaktywna, 'w pamięci'.
        // Zostanie aktywowana przy strzale/wyrzucie.

        OnRoundChambered?.Invoke();
        return true;
    }

    // Bez zmian
    public virtual bool CanReleaseBolt()
    {
        bool magExists = ammoSocket != null && ammoSocket.currentMagazine != null;
        bool magHasRounds = magExists && ammoSocket.currentMagazine.currentRounds > 0;
        return isBoltLockedBack && (!magExists || magHasRounds);
    }
}