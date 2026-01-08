using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
    [Tooltip("Transform (pusty GameObject) reprezentujący pozycję komory nabojowej.")]
    public Transform chamberTransform;
    // Kolzije Magazynka (aby je wyłączyć podczas podpiecia do gniazda)
    private List<Collider> internalGunParts = new List<Collider>();

    [Header("Wyrzut Łuski")]
    [Tooltip("Transform (pusty GameObject) w miejscu, z którego wylatuje łuska.")]
    public Transform ejectionPort;
    [Tooltip("Siła wyrzutu łuski.")]
    public Vector3 ejectionForce = new Vector3(1.5f, 0.5f, 0f);
    [Tooltip("Siła obrotu łuski.")]
    public Vector3 ejectionTorque = new Vector3(10f, 5f, 20f);

    // 🔹 NOWE: Parametr dziedziczenia prędkości
    [Tooltip("Jak bardzo łuska dziedziczy prędkość ruchu broni (0 = wcale, 1 = w pełni).")]
    [Range(0f, 1f)]
    public float velocityInheritance = 1.0f;

    [Header("Dane broni")]
    public string caliber = "5.56x45";

    [Header("Stan broni")]
    public GameObject chamberedRound;
    public bool isHammerCocked = false;
    public bool isBoltLockedBack = false;
    public FireMode currentFireMode = FireMode.Safe;

    [Header("Parametry trybów ognia")]
    public int burstCount = 3;
    public float fireRate = 0.1f;
    public float burstDelay = 0.08f;

    [Header("Balistyka")]
    public Transform muzzleTransform;
    public float velocityMultiplier = 1.0f;
    public GameObject universalProjectilePrefab;

    [Header("Eventy")]
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
    protected BulletPoolManager bulletPool;
    protected CasingPoolManager casingPool;

    // 🔹 NOWE: Zmienne do obliczania prędkości broni
    protected Vector3 lastFramePosition;
    protected Vector3 currentGunVelocity;

    private GameObject _cachedMagazine;

    protected virtual void Awake()
    {
        ammoPool = AmmoPoolManager.Instance;
        bulletPool = BulletPoolManager.Instance;
        casingPool = CasingPoolManager.Instance;

        if (ammoPool == null) Debug.LogError("Nie znaleziono AmmoPoolManager!", this);
        if (bulletPool == null) Debug.LogError("Nie znaleziono BulletPoolManager!", this);
        if (casingPool == null) Debug.LogError("Nie znaleziono CasingPoolManager!", this);

        if (fireSelector != null)
            fireSelector.weaponController = this;

        if (chargingHandle != null)
        {
            chargingHandle.OnBoltPulled.AddListener(OnBoltPulled);
            chargingHandle.OnBoltReleased.AddListener(OnChargingHandleReleased);
        }

        if (weaponGrab != null)
            weaponGrab.weaponController = this;
        BuildCollisionList();
    }

    protected virtual void Start()
    {
        // 🔹 NOWE: Inicjalizacja pozycji startowej
        lastFramePosition = transform.position;
    }

    protected virtual void Update()
    {
        if (!weaponGrab.IsGripHeld) {
            return;
        }
        // Obliczanie prędkości broni w każdej klatce
        // Robimy to ręcznie, bo Rigidbody w VR (Kinematic) często zwraca 0.
        if (Time.deltaTime > 0)
        {
            currentGunVelocity = (transform.position - lastFramePosition) / Time.deltaTime;
            lastFramePosition = transform.position;
        }

        HandleBurstLogic();
        HandleAutoFire();
    }

    public virtual void FireInput(bool pressed)
    {
        if (weaponGrab != null && !weaponGrab.IsGripHeld) return;

        if (!pressed)
        {
            burstShotsRemaining = 0;
            triggerPressed = false;
            return;
        }

        // Reagujemy tylko na naciśnięcie (Semi/Burst)
        if (pressed && !triggerPressed)
        {
            // 1. Sprawdzamy czy iglica jest napięta
            if (!isHammerCocked)
            {
                // Iglica nienapięta -> Spust jest martwy, nic nie rób
                return;
            }

            // 2. Jeśli jest napięta -> zwalniamy ją (klik!)
            // Chyba że to Safe, wtedy nie zwalniamy
            if (currentFireMode == FireMode.Safe) return;

            // Zrzucamy iglicę

            isHammerCocked = false;


            // 3. Teraz sprawdzamy czy w ogóle możemy strzelić (czy jest nabój)
            // Jeśli nie ma naboju, robimy DryFire (Klik)
            Bullet ammoData = GetChamberedBulletData();
            if (ammoData == null)
            {
                OnDryFire?.Invoke();
                return; // Koniec, słychać tylko klik
            }

            // 4. Jeśli jest nabój -> Strzelamy normalnie
            // (FireOnce w środku sam sobie znowu napnie iglicę po strzale)
            switch (currentFireMode)
            {
                case FireMode.Semi:
                    FireOnce();
                    break;
                case FireMode.Burst:
                    burstShotsRemaining = burstCount;
                    if (burstShotsRemaining > 0) { FireOnce(); burstShotsRemaining--; lastFireTime = Time.time; }
                    break;
                case FireMode.BoltAction:
                    HandleBoltActionFire();
                    break;
            }
        }
        triggerPressed = pressed;
    }

    protected virtual void HandleBurstLogic()
    {
        if (currentFireMode != FireMode.Burst) return;
        if (burstShotsRemaining <= 0) return;
        if (Time.time - lastFireTime >= burstDelay)
        {
            if (FireOnce()) { burstShotsRemaining--; lastFireTime = Time.time; }
        }
    }

    protected virtual void HandleAutoFire()
    {
        if (currentFireMode != FireMode.Auto) return;
        if (!triggerPressed) return;
        if (Time.time >= lastFireTime + fireRate)
        {
            if (FireOnce()) lastFireTime = Time.time;
        }
    }

    protected virtual void SpawnProjectile(Bullet ammoData)
    {
        if (universalProjectilePrefab == null) { Debug.LogError("Brak 'universalProjectilePrefab'!", this); return; }

        float muzzleVelocity = Mathf.Sqrt((2f * ammoData.muzzleEnergy) / ammoData.mass) * velocityMultiplier;

        for (int i = 0; i < ammoData.projectileCount; i++)
        {
            GameObject projectileInstance = bulletPool.GetBullet(universalProjectilePrefab);
            projectileInstance.transform.position = muzzleTransform.position;

            // 🔹 NOWOŚĆ: Obliczanie rozrzutu (Spread)
            Quaternion finalRotation = muzzleTransform.rotation;

            if (ammoData.spreadAngle > 0)
            {
                // Random.insideUnitCircle daje punkt w kole o promieniu 1.
                // Mnożymy przez spreadAngle, żeby uzyskać odchylenie w stopniach.
                Vector2 deviation = Random.insideUnitCircle * ammoData.spreadAngle;

                // Tworzymy rotację odchylającą (X to góra/dół, Y to lewo/prawo)
                Quaternion spreadRot = Quaternion.Euler(deviation.x, deviation.y, 0);

                // Łączymy rotację lufy z losowym odchyleniem
                finalRotation = muzzleTransform.rotation * spreadRot;
            }

            projectileInstance.transform.rotation = finalRotation;

            Projectile projectileLogic = projectileInstance.GetComponent<Projectile>();
            if (projectileLogic != null)
            {
                projectileLogic.Launch(
                    projectileInstance.transform.forward * muzzleVelocity,
                    ammoData.dragCoefficient, ammoData.ricochetAngle, ammoData.maxRicochets,
                    ammoData.ricochetBounciness, ammoData.ricochetFriction, ammoData.penetrationPower
                );
            }
        }
    }

    // -------------------------- FIZYKA WYRZUTU --------------------------

    /// <summary>
    /// Tworzy instancję łuski i umieszcza ją w komorze (kinematycznie).
    /// Używane przez Bolt-Action i Shotguny (gdzie łuska zostaje w środku).
    /// </summary>
    protected virtual GameObject SpawnAndChamberCasing(GameObject casingPrefab)
    {
        if (casingPool == null || casingPrefab == null) return null;

        GameObject casingInstance = casingPool.GetCasing(casingPrefab);

        // Pobieramy komponent, żeby znać skalę
        Casing casingScript = casingInstance.GetComponent<Casing>();

        if (chamberTransform != null)
        {
            casingInstance.transform.position = chamberTransform.position;
            casingInstance.transform.rotation = chamberTransform.rotation;
            casingInstance.transform.SetParent(chamberTransform);

            // 🔹 ZMIANA: Przywróć oryginalną skalę
            if (casingScript != null)
            {
                casingInstance.transform.localScale = casingScript.defaultScale;
            }
            else
            {
                // Fallback, jeśli zapomniałeś dodać skrypt Casing
                casingInstance.transform.localScale = casingPrefab.transform.localScale;
            }
        }

        Collider col = casingInstance.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = casingInstance.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (casingScript != null) casingScript.enabled = false;

        casingInstance.SetActive(true);
        return casingInstance;
    }

    /// <summary>
    /// Fizycznie wyrzuca obiekt z portu (dla łuski lub naboju).
    /// </summary>
    protected virtual void PhysicallyEjectObject(GameObject objToEject)
    {
        if (objToEject == null) return;

        objToEject.transform.SetParent(null);

        // 🔹 ZMIANA: Upewnij się, że po odczepieniu skala wraca do normy
        // Sprawdzamy czy to nabój czy łuska
        Bullet b = objToEject.GetComponent<Bullet>();
        Casing c = objToEject.GetComponent<Casing>();

        if (b != null) objToEject.transform.localScale = b.defaultScale;
        else if (c != null) objToEject.transform.localScale = c.defaultScale;
        // Jeśli nie ma żadnego skryptu, zostawiamy taką jaka jest (lub Vector3.one jako ostateczność)

        Collider col = objToEject.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Rigidbody rb = objToEject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            if (ejectionPort != null)
            {
                objToEject.transform.position = ejectionPort.position;
                objToEject.transform.rotation = ejectionPort.rotation;

                rb.linearVelocity = currentGunVelocity * velocityInheritance;
                rb.angularVelocity = Vector3.zero;

                Vector3 worldForce = ejectionPort.TransformDirection(ejectionForce);
                Vector3 worldTorque = ejectionPort.TransformDirection(ejectionTorque);

                rb.AddForce(worldForce, ForceMode.Impulse);
                rb.AddTorque(worldTorque, ForceMode.Impulse);
            }
            else
            {
                rb.WakeUp();
            }
        }

        if (c != null) c.enabled = true;
    }

    protected virtual Bullet GetChamberedBulletData()
    {
        if (chamberedRound == null) {; return null; }
        Bullet ammoData = chamberedRound.GetComponent<Bullet>();
        if (ammoData == null) {; return null; }
        return ammoData;
    }

    protected virtual bool FireOnce()
    {
        if (isBoltLockedBack || !bolt.IsBoltForward) { return false; }
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null) return false;

        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        GameObject casingPrefab = ammoData.casingPrefab;
        if (ammoPool != null) ammoPool.ReturnRound(chamberedRound);
        else Destroy(chamberedRound);
        chamberedRound = null;

        if (casingPool != null && casingPrefab != null)
        {
            GameObject casingInstance = casingPool.GetCasing(casingPrefab);
            PhysicallyEjectObject(casingInstance);
        }

        TryChamberFromMagazine();
        isHammerCocked = true;
        return true;
    }

    protected virtual void HandleBoltActionFire()
    {
        if (chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f) { OnDryFire?.Invoke(); return; }
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null) return;

        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        GameObject casingPrefab = ammoData.casingPrefab;
        if (ammoPool != null) ammoPool.ReturnRound(chamberedRound);
        else Destroy(chamberedRound);
        chamberedRound = null;

        chamberedRound = SpawnAndChamberCasing(casingPrefab);
    }

    public virtual void OnBoltPulled()
    {
        if (chamberedRound != null)
        {
            OnRoundEjected?.Invoke();
            PhysicallyEjectObject(chamberedRound);
            chamberedRound = null;
        }
        isBoltLockedBack = true;
        OnBoltLockedBack?.Invoke();
    }

    protected virtual void OnChargingHandleReleased()
    {
        isBoltLockedBack = false;
        isHammerCocked = true;
        TryChamberFromMagazine();
        OnBoltReleasedEvent?.Invoke();
    }

    public virtual void ReleaseBoltAction(bool force = false)
    {
        if (!isBoltLockedBack) return;
        if (!force && !CanReleaseBolt()) return;
        TryChamberFromMagazine();
        isBoltLockedBack = false;
        isHammerCocked = true;
        OnBoltReleasedEvent?.Invoke();
    }
    /// <summary>
    /// Metoda do manualnego zrzucenia suwadła/zamka (np. przyciskiem Slide Release w pistolecie).
    /// Wymusza powrót rączki przeładowania do pozycji zerowej i wprowadza nabój.
    /// </summary>
    /// <param name="force">Czy zignorować stan magazynka (np. zrzut na sucho)?</param>
    public virtual bool TryChamberFromMagazine()
    {
        if (chamberedRound != null) return true;
        if (ammoSocket == null) return false;

        GameObject roundToChamber = ammoSocket.TryTakeRound();
        if (roundToChamber == null) return false;

        Bullet bulletData = roundToChamber.GetComponent<Bullet>();
        if (bulletData == null) { Destroy(roundToChamber); return false; }

        if (bulletData.caliber != this.caliber)
        {
            if (ammoPool != null) ammoPool.ReturnRound(roundToChamber);
            else Destroy(roundToChamber);
            return false;
        }

        chamberedRound = roundToChamber;

        if (chamberTransform != null)
        {
            chamberedRound.transform.position = chamberTransform.position;
            chamberedRound.transform.rotation = chamberTransform.rotation;
            chamberedRound.transform.SetParent(chamberTransform);

            // 🔹 ZMIANA: Przywróć oryginalną skalę z prefabu
            chamberedRound.transform.localScale = bulletData.defaultScale;

            Rigidbody rb = chamberedRound.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Collider col = chamberedRound.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            chamberedRound.SetActive(true);
        }
        else
        {
            Debug.LogError("Brak 'chamberTransform'!", this);
        }

        OnRoundChambered?.Invoke();
        return true;
    }

    public virtual bool CanReleaseBolt()
    {
        bool magExists = ammoSocket != null && ammoSocket.currentMagazine != null;
        bool magHasRounds = magExists && ammoSocket.currentMagazine.currentRounds > 0;
        return isBoltLockedBack && (!magExists || magHasRounds);
    }
    public virtual void EjectMagazine()
    {
        // 1. Sprawdzamy czy mamy podpięty AmmoSocket
        if (ammoSocket == null)
        {
            Debug.LogError("[WeaponController] Brak przypisanego AmmoSocket!");
            return;
        }

        // 2. Pobieramy XRSocketInteractora bezpośrednio z obiektu, na którym jest AmmoSocket
        var socketInteractor = ammoSocket.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();

        // 3. Sprawdzamy czy w sockecie w ogóle coś jest
        if (socketInteractor.hasSelection)
        {
            Debug.Log("[WeaponController] Procedura zrzutu magazynka rozpoczęta.");

            // Pobieramy obiekt magazynka
            var interactable = socketInteractor.interactablesSelected.Count > 0 ?
                               socketInteractor.interactablesSelected[0] as MonoBehaviour : null;

            StartCoroutine(EjectRoutine(socketInteractor, interactable));
        }
    }

    // Musiałem lekko zmienić parametry Coroutine, żeby przyjmowała znaleziony socket
    private IEnumerator EjectRoutine(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket, MonoBehaviour magInteractable)
    {
        // Wyłączamy przyciąganie
        socket.socketActive = false;

        // Fizyczne wypchnięcie
        if (magInteractable != null)
        {
            Rigidbody rb = magInteractable.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.WakeUp();
                // Pchamy w dół względem socketa
                rb.AddForce(-socket.transform.up * 2f, ForceMode.Impulse);
            }
        }

        // Czekamy
        yield return new WaitForSeconds(0.5f);

        // Włączamy z powrotem
        socket.socketActive = true;
    }
    private void BuildCollisionList()
    {
        internalGunParts.Clear();
        // Zbieramy wszystko z broni
        internalGunParts.AddRange(GetComponentsInChildren<Collider>(true));

        // Dodajemy Zamek (Charging Handle)
        if (chargingHandle != null)
        {
            var handleCols = chargingHandle.GetComponentsInChildren<Collider>(true);
            foreach (var col in handleCols)
            {
                if (!internalGunParts.Contains(col)) internalGunParts.Add(col);
            }
        }

        // Logujemy ile znaleźliśmy części (jeśli wyjdzie 0, to tu jest błąd)
        Debug.Log($"[WeaponCollision] Zbudowano listę koliderów broni. Znaleziono: {internalGunParts.Count} części.");
    }

    // --- FUNKCJE DLA EVENTÓW (BEZ ARGUMENTÓW) ---
    // Podepnij to pod OnMagazineInserted (nie wymaga parametru!)
    public void OnMagazineInsertedEvent()
    {
        // 1. Upewnij się, że mamy listę części broni
        if (internalGunParts.Count == 0) BuildCollisionList();

        if (ammoSocket == null) return;

        var socket = ammoSocket.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();

        // Sprawdzamy co weszło
        if (socket != null && socket.hasSelection)
        {
            var magInteractable = socket.interactablesSelected[0] as MonoBehaviour;
            if (magInteractable != null)
            {
                // ZAPAMIĘTUJEMY MAGAZYNEK!
                _cachedMagazine = magInteractable.gameObject;

                // Wyłączamy kolizję (TRUE)
                SetMagCollision(_cachedMagazine, true);

                Debug.Log($"[WeaponCollision] Włożono magazynek: {_cachedMagazine.name}. Kolizje wyłączone.");
            }
        }
    }

    // Podepnij to pod OnMagazineRemoved
    public void OnMagazineRemovedEvent()
    {
        // Tutaj socket jest już pusty, więc nie możemy z niego pobrać magazynka.
        // Ale mamy go zapisanego w zmiennej _cachedMagazine!

        if (_cachedMagazine != null)
        {
            // Przywracamy kolizję (FALSE)
            SetMagCollision(_cachedMagazine, false);

            Debug.Log($"[WeaponCollision] Wyjęto magazynek: {_cachedMagazine.name}. Kolizje przywrócone.");

            // Czyścimy zmienną, bo magazynek już wyszedł
            _cachedMagazine = null;
        }
        else
        {
            Debug.LogWarning("[WeaponCollision] Próba przywrócenia kolizji, ale nie znaleziono zapamiętanego magazynka.");
        }
    }
    public void SetMagazineCollisionIgnored(GameObject mag)
    {
        if (mag == null) return;
        Debug.Log($"[WeaponCollision] Manualne wyłączenie dla: {mag.name}");
        SetMagCollision(mag, true);
    }

    // --- LOGIKA GŁÓWNA ---
    private void SetMagCollision(GameObject magazineRoot, bool ignore)
    {
        var magColliders = magazineRoot.GetComponentsInChildren<Collider>(true);

        int count = 0;
        foreach (var gunPart in internalGunParts)
        {
            if (gunPart == null || gunPart.isTrigger) continue;

            foreach (var magPart in magColliders)
            {
                if (magPart.isTrigger) continue;
                Physics.IgnoreCollision(gunPart, magPart, ignore);
                count++;
            }
        }
        Debug.Log($"[WeaponCollision] Zaktualizowano {count} par kolizji (Ignore={ignore}).");
    }
}