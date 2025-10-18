//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.XR.Interaction.Toolkit;
//using UnityEngine.XR.Interaction.Toolkit.Interactables;
//using UnityEngine.XR.Interaction.Toolkit.Interactors;

//public class WeaponController : MonoBehaviour
//{

//    [Header("Referencje")]
//    public AmmoSocket ammoSocket;
//    public ChargingHandle chargingHandle;
//    public BoltFollower bolt;

//    [Header("Dane broni")]
//    public string caliber = "5.56x45";

//    [Header("Stan")]
//    public bool isChambered = false;
//    public bool isBoltLockedBack = false;
//    public FireMode currentFireMode = FireMode.Safe;

//    [Header("Parametry trybów ognia")]
//    public int burstCount = 3;
//    public float fireRate = 0.1f;
//    public float burstDelay = 0.08f;

//    [Header("Eventy")]
//    public UnityEvent OnFire;
//    public UnityEvent OnDryFire;
//    public UnityEvent OnRoundChambered;
//    public UnityEvent OnRoundEjected;
//    public UnityEvent OnBoltLockedBack;
//    public UnityEvent OnBoltReleasedEvent;

//    [Header("XR Grip")]
//    public Transform gripAttachPoint; // przypisz attach transform gripa w inspectorze
//    private XRBaseInteractor gripInteractor; // aktualna ręka trzymająca grip
//    private XRGrabInteractable grabInteractable;

//    private float lastFireTime;
//    private bool triggerPressed;
//    private int burstShotsRemaining = 0;

//    void Awake()
//    {
//        if (chargingHandle != null)
//        {
//            chargingHandle.OnBoltPulled.AddListener(OnBoltPulled);
//            chargingHandle.OnBoltReleased.AddListener(() => ReleaseBoltAction(false));
//        }

//        grabInteractable = GetComponent<XRGrabInteractable>();
//        if (grabInteractable != null)
//        {
//            grabInteractable.selectEntered.AddListener(OnGrab);
//            grabInteractable.selectExited.AddListener(OnRelease);
//        }
//    }

//    void OnDestroy()
//    {
//        if (chargingHandle != null)
//        {
//            chargingHandle.OnBoltPulled.RemoveListener(OnBoltPulled);
//            chargingHandle.OnBoltReleased.RemoveAllListeners();
//        }

//        if (grabInteractable != null)
//        {
//            grabInteractable.selectEntered.RemoveListener(OnGrab);
//            grabInteractable.selectExited.RemoveListener(OnRelease);
//        }
//    }

//    private void OnGrab(SelectEnterEventArgs args)
//    {
//        var interactor = args.interactorObject as XRBaseInteractor;
//        if (interactor == null) return;

//        // sprawdzamy czy to ręka, która chwyta za gripAttachPoint
//        if (grabInteractable.GetAttachTransform(interactor) == gripAttachPoint)
//        {
//            gripInteractor = interactor;
//        }
//    }

//    private void OnRelease(SelectExitEventArgs args)
//    {
//        var interactor = args.interactorObject as XRBaseInteractor;
//        if (interactor == null) return;

//        if (interactor == gripInteractor)
//        {
//            gripInteractor = null;
//        }
//    }

//    // Wywoływane przez rękę (np. w HandController)
//    public void FireInputFromHand(XRBaseInteractor hand, bool pressed)
//    {
//        // tylko ręka trzymająca grip może strzelać
//        if (gripInteractor != null && hand == gripInteractor)
//        {
//            FireInput(pressed);
//        }
//    }

//    // ---------- Logika strzału ----------
//    public void FireInput(bool pressed)
//    {
//        switch (currentFireMode)
//        {
//            case FireMode.Safe:
//                break;

//            case FireMode.Semi:
//                if (pressed && !triggerPressed)
//                    FireOnce();
//                break;

//            case FireMode.Burst:
//                if (pressed && !triggerPressed && burstShotsRemaining == 0)
//                    burstShotsRemaining = burstCount;
//                break;

//            case FireMode.BoltAction:
//                if (pressed && !triggerPressed)
//                {
//                    if (FireOnce())
//                    {
//                        isBoltLockedBack = true;
//                        OnBoltLockedBack?.Invoke();
//                    }
//                }
//                break;

//            case FireMode.Auto:
//                // handled in Update
//                break;
//        }

//        triggerPressed = pressed;
//    }

//    void Update()
//    {
//        HandleBurstLogic();
//        HandleAutoFire();
//    }

//    private void HandleBurstLogic()
//    {
//        if (currentFireMode != FireMode.Burst) return;
//        if (burstShotsRemaining <= 0) return;

//        if (Time.time - lastFireTime >= burstDelay)
//        {
//            if (FireOnce())
//            {
//                burstShotsRemaining--;
//                lastFireTime = Time.time;
//            }
//            else
//            {
//                burstShotsRemaining = 0;
//            }
//        }
//    }

//    private void HandleAutoFire()
//    {
//        if (currentFireMode != FireMode.Auto) return;
//        if (!triggerPressed) return;

//        if (Time.time >= lastFireTime + fireRate)
//        {
//            bool fired = FireOnce();
//            if (fired)
//                lastFireTime = Time.time;
//        }
//    }

//    private bool FireOnce()
//    {
//        if (isBoltLockedBack || !isChambered || !bolt.IsBoltForward)
//        {
//            OnDryFire?.Invoke();
//            return false;
//        }

//        isChambered = false;
//        OnFire?.Invoke();

//        if (!TryChamberFromMagazine())
//        {
//            isBoltLockedBack = true;
//            OnBoltLockedBack?.Invoke();
//        }

//        return true;
//    }

//    // ---------- Bolt ----------
//    public void OnBoltPulled()
//    {
//        if (isChambered)
//        {
//            isChambered = false;
//            OnRoundEjected?.Invoke();
//        }

//        if (!TryChamberFromMagazine())
//        {
//            isBoltLockedBack = true;
//            OnBoltLockedBack?.Invoke();
//        }
//        else
//        {
//            isBoltLockedBack = false;
//        }
//    }

//    public bool CanReleaseBolt()
//    {
//        bool magExists = ammoSocket != null && ammoSocket.currentMagazine != null;
//        bool magHasRounds = magExists && ammoSocket.currentMagazine.currentRounds > 0;
//        return isBoltLockedBack && (!magExists || magHasRounds);
//    }

//    public void ReleaseBoltAction(bool force = false)
//    {
//        if (!force && !CanReleaseBolt())
//            return;

//        TryChamberFromMagazine();
//        isBoltLockedBack = false;

//        OnBoltReleasedEvent?.Invoke();
//    }

//    public bool TryChamberFromMagazine()
//    {
//        if (ammoSocket == null || ammoSocket.currentMagazine == null)
//            return false;

//        if (ammoSocket.currentMagazine.caliber != caliber)
//            return false;

//        if (ammoSocket.TryTakeRound())
//        {
//            isChambered = true;
//            OnRoundChambered?.Invoke();
//            return true;
//        }

//        return false;
//    }
//}


/*
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Uniwersalny kontroler broni obsługujący różne konstrukcje:
/// AR (M4/AR) - domyślnie blokada zamka na pustym
/// HK-style (MP5/G3) - nie zostawia zamka w tyle, zamiast tego "zaczepia dźwignię/rowek"
/// AK-style - zamek i dźwignia jedna, nie blokuje się na pustym
/// Pistol - slide/hammer synergy
/// Shotgun_Pump - pompa (brak automatycznego repetowania), tubowy mag
/// FourStroke - rotacyjny bolt wymagający obrotu o określony kąt przed możliwością repetowania
/// 
/// Zachowuje kompatybilność z eventami z oryginalnego WeaponController.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponController : MonoBehaviour
{
    public enum WeaponPlatform { AR, HK, AK, Pistol, Shotgun_Pump, FourStroke }

    [Header("Referencje")]
    public AmmoSocket ammoSocket;
    public ChargingHandle chargingHandle;   // (opcjonalne) handle do pociągania zamka
    public BoltFollower boltFollower;       // (opcjonalne) viz follower
    public Transform gripAttachPoint;       // attach used by WeaponGrabInteractable

    [Header("Konfiguracja")]
    public WeaponPlatform weaponPlatform = WeaponPlatform.AR;
    public string caliber = "5.56x45";

    [Header("Stan")]
    [SerializeField] public bool isChambered = false;
    [SerializeField] public bool isBoltLockedBack = false; // dla AR-style
    // Dla HK-style: zamiast isBoltLockedBack użyjemy isLeverEngaged
    [SerializeField] public bool isLeverEngaged = false;
    // Dla pistols: slide state
    [SerializeField] public bool isSlidePulled = false;
    // Dla pump: czy wymagane wykonanie pompy, po strzale
    [SerializeField] public bool isPumpRequired = false;
    // Dla FourStroke: kąt aktualny i próg
    [SerializeField] public float fourStrokeCurrentAngle = 0f;
    [SerializeField] public float fourStrokeRequiredAngle = 90f;

    [Header("Parametry ogólne i strzału")]
    public FireMode currentFireMode = FireMode.Semi;
    public float fireRate = 0.1f;
    public int burstCount = 3;
    public float burstDelay = 0.08f;

    [Header("Shotgun Pump (opcje)")]
    public bool shotgunAutoLoadFromTube = true; // czy TryChamberFromMagazine bierze shell z tuby
    public int tubeCapacity = 6;

    [Header("HK / Lever options")]
    public float hkLeverNotchAngle = 10f; // opcjonalny parametr wizualny/logiczny
    public bool hkLockWhenEmpty = true; // zachowanie specyficzne (domyślnie true -> zaczep działa)

    [Header("FourStroke options")]
    public float fourStrokeAnglePerCycle = 90f; // ile stopni obraca się bolt na jeden cykl

    [Header("Eventy")]
    public UnityEvent OnFire;
    public UnityEvent OnDryFire;
    public UnityEvent OnRoundChambered;
    public UnityEvent OnRoundEjected;
    public UnityEvent OnBoltLockedBack;
    public UnityEvent OnBoltReleasedEvent;

    // Dodatkowe eventy specyficzne platform:
    public UnityEvent OnLeverEngaged;       // HK: lever zaskakuje
    public UnityEvent OnSlidePulled;       // Pistol: slide pulled
    public UnityEvent OnPumpRequired;      // Shotgun_Pump: informuje, że trzeba pompować
    public UnityEvent OnFourStrokeRotationReached; // FourStroke: obrócił się wystarczająco

    // XR / interactor
    private XRGrabInteractable grabInteractable;
    private float lastFireTime;
    private bool triggerPressed;
    private int burstShotsRemaining = 0;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Podpięcie do ChargingHandle (jeśli istnieje)
        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.AddListener(() => OnHandlePulled());
            chargingHandle.OnBoltReleased.AddListener(() => OnHandleReleased());
        }

        // Upewnij się, że stan początkowy jest sensowny
        SyncInitialStateWithPlatform();
    }

    private void OnDestroy()
    {
        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.RemoveListener(() => OnHandlePulled());
            chargingHandle.OnBoltReleased.RemoveListener(() => OnHandleReleased());
        }
    }

    private void Update()
    {
        HandleBurstLogic();
        HandleAutoFire();
    }

    private void HandleBurstLogic()
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

    private void HandleAutoFire()
    {
        if (currentFireMode != FireMode.Auto) return;
        if (!triggerPressed) return;
        if (Time.time < lastFireTime + fireRate) return;

        bool fired = FireOnce();
        if (fired) lastFireTime = Time.time;
    }

    /// <summary>
    /// Public entrypoint: input from WeaponGrabInteractable/hand
    /// </summary>
    public void FireInput(bool pressed)
    {
        switch (currentFireMode)
        {
            case FireMode.Safe:
                break;

            case FireMode.Semi:
                if (pressed && !triggerPressed) FireOnce();
                break;

            case FireMode.Burst:
                if (pressed && !triggerPressed && burstShotsRemaining == 0)
                    burstShotsRemaining = burstCount;
                break;

            case FireMode.BoltAction:
                if (pressed && !triggerPressed)
                {
                    if (FireOnce())
                    {
                        // Dla bolt-action chcemy, żeby zamek został cofniety (opcjonalnie)
                        if (weaponPlatform == WeaponPlatform.AR)
                        {
                            isBoltLockedBack = true;
                            OnBoltLockedBack?.Invoke();
                        }
                    }
                }
                break;

            case FireMode.Auto:
                // handled in Update
                break;
        }

        triggerPressed = pressed;
    }

    /// <summary>
    /// Właściwa logika strzału - platform-independent w sensie wywołań eventów,
    /// ale zachowania po strzale są zależne od platformy.
    /// </summary>
    private bool FireOnce()
    {
        // Warunki uniwersalne: nie można strzelić jeśli zamek zablokowany (AR) lub jeśli nie ma naboju w komorze
        if ((weaponPlatform == WeaponPlatform.AR && isBoltLockedBack) ||
            (weaponPlatform == WeaponPlatform.Shotgun_Pump && isPumpRequired) ||
            !isChambered)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał
        isChambered = false;
        OnFire?.Invoke();

        // Zachowania post-strzałowe zależnie od platformy:
        switch (weaponPlatform)
        {
            case WeaponPlatform.AR:
                // Auto-repetowanie: próbujemy załadować z magazynka, jeśli nie -> zamek zostaje w tyle
                if (!TryChamberFromMagazine())
                {
                    isBoltLockedBack = true;
                    OnBoltLockedBack?.Invoke();
                }
                break;

            case WeaponPlatform.HK:
                // HK: zamek zwykle nie zostaje w tyle, zamiast tego dźwignia "zaczepia"
                // Jeśli brak nabojow -> ustawiamy isLeverEngaged (zaczep) i wywołujemy event
                if (!TryChamberFromMagazine())
                {
                    isLeverEngaged = hkLockWhenEmpty;
                    if (isLeverEngaged) OnLeverEngaged?.Invoke();
                }
                else
                {
                    isLeverEngaged = false;
                }
                break;

            case WeaponPlatform.AK:
                // AK: zamek i dźwignia jedna, zwykle nie blokuje się sam na pustym (gracz rzadko chce blokadę)
                // Po prostu próbujemy załadować, jeśli nie -> nie ustawiamy locka
                TryChamberFromMagazine();
                break;

            case WeaponPlatform.Pistol:
                // Pistolety: slide i kurek - po strzale w większości półautomatycznych zamek cykluje automatycznie
                // jeśli brak amunicji -> zależnie od konstrukcji slide może zostać z tyłu (domyślnie true)
                if (!TryChamberFromMagazine())
                {
                    // domyślnie ustawiamy slide pulled (blokada) - możesz to skonfigurować, ale user napisał:
                    // "kurek po odciągnięciu zamka cofa sie razem z nim" -> traktujemy slide jako związany z zamkiem
                    isSlidePulled = true;
                    OnSlidePulled?.Invoke();
                }
                else
                {
                    isSlidePulled = false;
                }
                break;

            case WeaponPlatform.Shotgun_Pump:
                // Po strzale wymagamy ręcznej pompki, aby załadować kolejny shell (brak automatycznego repetowania)
                isPumpRequired = true;
                OnPumpRequired?.Invoke();
                break;

            case WeaponPlatform.FourStroke:
                // FourStroke: po strzale należy wykonać ruch obrotowy zamka (lub inny cykl)
                // Tutaj nie bierzemy automatycznie z magazynka - trzeba obrócić bolt
                // Zostawiamy isChambered=false i czekamy na AdvanceFourStroke() z zewnątrz
                break;
        }

        return true;
    }

    /// <summary>
    /// Próbuj załadować następny nabój z magazynka/tuby; zwraca true jeżeli załadowano
    /// </summary>
    public bool TryChamberFromMagazine()
    {
        if (ammoSocket == null || ammoSocket.currentMagazine == null) return false;

        // Dla shotgun_pump może to być tubowy mag - ammoSocket.TryTakeRound powinno obsługiwać to,
        // zakładamy, że AmmoSocket wie o typie magazynka.
        if (ammoSocket.currentMagazine.caliber != caliber) return false;

        if (ammoSocket.TryTakeRound())
        {
            isChambered = true;
            OnRoundChambered?.Invoke();
            return true;
        }
        return false;
    }

    // --------- ChargingHandle callbacks ----------
    private void OnHandlePulled()
    {
        // Kiedy gracz pociągnie handle: ewentualne ejection i logika
        if (isChambered)
        {
            // ejected round
            isChambered = false;
            OnRoundEjected?.Invoke();
        }

        // w zależności od platformy:
        switch (weaponPlatform)
        {
            case WeaponPlatform.AR:
                // Próba załadowania: gdy pociągnięto zamek ręcznie, próbujemy wziąć rundę
                if (!TryChamberFromMagazine())
                {
                    isBoltLockedBack = true;
                    OnBoltLockedBack?.Invoke();
                }
                else
                {
                    isBoltLockedBack = false;
                }
                break;

            case WeaponPlatform.HK:
                // HK: przy pociągnięciu zamka, jeśli nie ma amunicji -> lever może zostać zablokowany
                if (!TryChamberFromMagazine())
                {
                    isLeverEngaged = hkLockWhenEmpty;
                    if (isLeverEngaged) OnLeverEngaged?.Invoke();
                }
                else isLeverEngaged = false;
                break;

            case WeaponPlatform.AK:
                // AK: pociągnięcie zamka po prostu stara się załadować (bez blokady)
                TryChamberFromMagazine();
                break;

            case WeaponPlatform.Pistol:
                // Slide pulled -> powiązana logika (mutex slide/hammer)
                isSlidePulled = true;
                OnSlidePulled?.Invoke();
                // ewentualne ładowanie:
                TryChamberFromMagazine();
                break;

            case WeaponPlatform.Shotgun_Pump:
                // Przy pociągnięciu ręcznym (np. pump action), jeśli gracz pociąga to bierzemy shell do komory
                // ale zwykle pompa jest oddzielną akcją; tutaj traktujemy jako manualne repetowanie:
                if (!TryChamberFromMagazine())
                {
                    // brak round - nie ma co załadować
                }
                else
                {
                    isPumpRequired = false;
                }
                break;

            case WeaponPlatform.FourStroke:
                // Przy ręcznym pociągnięciu możesz od razu wywołać rotację
                // (zamiast "pull" musisz AdvanceFourStroke)
                break;
        }
    }

    private void OnHandleReleased()
    {
        // Kiedy handle wraca do przodu -> zwalniamy eventy/flag
        switch (weaponPlatform)
        {
            case WeaponPlatform.AR:
                if (isBoltLockedBack)
                {
                    // jeśli był zablokowany logicznie, nie zmieniamy stanu — wymaga ReleaseBoltAction
                }
                else
                {
                    // bolt wrócił normalnie
                    OnBoltReleasedEvent?.Invoke();
                }
                break;

            case WeaponPlatform.HK:
                // Po puszczeniu handle'a, jeśli lever był engaged, pozostaw go (może wymagać specjalnej akcji)
                break;

            case WeaponPlatform.Pistol:
                // slide wrócił -> hammer może być zwolniony
                isSlidePulled = false;
                break;

            case WeaponPlatform.Shotgun_Pump:
                // po puszczeniu pompy jeżeli coś załadowano to isPumpRequired=false
                break;
        }
    }

    /// <summary>
    /// Sprawdza, czy możemy zwolnić zamek (znane reguły: mag istnieje i ma naboje)
    /// Uogólniona metoda - zachowanie może różnić się per platformy.
    /// </summary>
    public bool CanReleaseBolt()
    {
        bool magExists = ammoSocket != null && ammoSocket.currentMagazine != null;
        bool magHasRounds = magExists && ammoSocket.currentMagazine.currentRounds > 0;

        switch (weaponPlatform)
        {
            case WeaponPlatform.AR:
                return isBoltLockedBack && (!magExists || magHasRounds);

            case WeaponPlatform.HK:
                // dla HK wymaga warunku, żeby dźwignia miała co chwycić
                return isLeverEngaged && (!magExists || magHasRounds);

            case WeaponPlatform.AK:
                // AK: nie ma blokady — release zawsze możliwy
                return true;

            case WeaponPlatform.Pistol:
                // Slide release: jeżeli slide był pulled, można go zwolnić tylko jeżeli mag ma round albo wymuszasz
                return isSlidePulled && (!magExists || magHasRounds);

            case WeaponPlatform.Shotgun_Pump:
                // pump: jeśli wymagane, release jest możliwy po pompce albo siłą (force)
                return !isPumpRequired;

            case WeaponPlatform.FourStroke:
                // require rotation threshold
                return fourStrokeCurrentAngle >= fourStrokeRequiredAngle;

            default:
                return false;
        }
    }

    /// <summary>
    /// Akcja zwolnienia zamka (albo dźwigni/pompki/slide) - przyjmuje force by wymusić
    /// </summary>
    public void ReleaseBoltAction(bool force = false)
    {
        if (!force && !CanReleaseBolt()) return;

        switch (weaponPlatform)
        {
            case WeaponPlatform.AR:
                TryChamberFromMagazine();
                isBoltLockedBack = false;
                OnBoltReleasedEvent?.Invoke();
                break;

            case WeaponPlatform.HK:
                // przy lever engage - zwalniamy dźwignię i robimy próbe załadunku
                TryChamberFromMagazine();
                isLeverEngaged = false;
                OnBoltReleasedEvent?.Invoke();
                break;

            case WeaponPlatform.AK:
                // AK: prosty cykl - pobieramy rundę (jeśli jest)
                TryChamberFromMagazine();
                OnBoltReleasedEvent?.Invoke();
                break;

            case WeaponPlatform.Pistol:
                TryChamberFromMagazine();
                isSlidePulled = false;
                OnBoltReleasedEvent?.Invoke();
                break;

            case WeaponPlatform.Shotgun_Pump:
                // pompujesz -> bierzemy shell z tuby i ustawiamy isPumpRequired=false
                if (TryChamberFromMagazine())
                {
                    isPumpRequired = false;
                    OnBoltReleasedEvent?.Invoke();
                }
                break;

            case WeaponPlatform.FourStroke:
                // resetujemy obrót i ewentualnie ładujemy
                if (TryChamberFromMagazine())
                {
                    fourStrokeCurrentAngle = 0f;
                    OnFourStrokeRotationReached?.Invoke();
                    OnBoltReleasedEvent?.Invoke();
                }
                break;
        }
    }

    /// <summary>
    /// Używane dla Shotgun_Pump — gracz wykonuje pump action (może być wołane z inputu lub interaction)
    /// </summary>
    public void PumpAction()
    {
        if (weaponPlatform != WeaponPlatform.Shotgun_Pump) return;

        if (TryChamberFromMagazine())
        {
            isPumpRequired = false;
            OnBoltReleasedEvent?.Invoke();
        }
    }

    /// <summary>
    /// Używane dla Pistol: ręczne odciągnięcie suwadła/slide (np. z ChargingHandle).
    /// </summary>
    public void SlidePull(bool pulled)
    {
        if (weaponPlatform != WeaponPlatform.Pistol) return;

        isSlidePulled = pulled;
        if (pulled) OnSlidePulled?.Invoke();
        else OnBoltReleasedEvent?.Invoke();
    }

    /// <summary>
    /// Używane dla FourStroke: posuwamy bolt o pewien kąt (np. z animacji/handle),
    /// gdy osiągniemy fourStrokeRequiredAngle -> możliwość repetowania
    /// </summary>
    public void AdvanceFourStroke(float deltaAngle)
    {
        if (weaponPlatform != WeaponPlatform.FourStroke) return;

        fourStrokeCurrentAngle += deltaAngle;
        if (fourStrokeCurrentAngle >= fourStrokeRequiredAngle)
        {
            // gotowe do załadowania
            OnFourStrokeRotationReached?.Invoke();
        }
    }

    /// <summary>
    /// Synchronizacja początkowa: ustawienia domyślne per platformy, aby nie startować w dziwnym stanie
    /// </summary>
    private void SyncInitialStateWithPlatform()
    {
        switch (weaponPlatform)
        {
            case WeaponPlatform.AR:
                // AR: jeśli mamy amunicję -> załaduj 1 round do komory
                if (ammoSocket != null && ammoSocket.currentMagazine != null)
                {
                    // tylko jeśli jest dostępna metoda pobierania roundów i chcesz auto-setup - tu zostawiam to wyłączone,
                    // bo w projekcie być może ładujesz manualnie przez editor.
                }
                isBoltLockedBack = false;
                break;

            case WeaponPlatform.HK:
                isLeverEngaged = false;
                break;

            case WeaponPlatform.AK:
                // nie blokujemy niczego
                isBoltLockedBack = false;
                break;

            case WeaponPlatform.Pistol:
                isSlidePulled = false;
                break;

            case WeaponPlatform.Shotgun_Pump:
                isPumpRequired = false;
                break;

            case WeaponPlatform.FourStroke:
                fourStrokeCurrentAngle = 0f;
                break;
        }
    }

    // ----- Dodatkowe pomocnicze metody do debugu / integracji -----
    public void ForceBoltForward()
    {
        isBoltLockedBack = false;
        isLeverEngaged = false;
        isSlidePulled = false;
        isPumpRequired = false;
        fourStrokeCurrentAngle = 0f;

        OnBoltReleasedEvent?.Invoke();
    }
}
*/