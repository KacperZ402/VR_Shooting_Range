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
    }

    protected virtual void Start()
    {
        // 🔹 NOWE: Inicjalizacja pozycji startowej
        lastFramePosition = transform.position;
    }

    protected virtual void Update()
    {
        // 🔹 NOWE: Obliczanie prędkości broni w każdej klatce
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
        switch (currentFireMode)
        {
            case FireMode.Safe: break;
            case FireMode.Semi:
                if (pressed && !triggerPressed) FireOnce();
                break;
            case FireMode.Burst:
                if (pressed && !triggerPressed && burstShotsRemaining == 0) burstShotsRemaining = burstCount;
                break;
            case FireMode.BoltAction:
                if (pressed && !triggerPressed) HandleBoltActionFire();
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
            if (FireOnce()) { burstShotsRemaining--; lastFireTime = Time.time; }
            else { burstShotsRemaining = 0; }
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
        // ... reszta parametrów bez zmian ...

        for (int i = 0; i < ammoData.projectileCount; i++)
        {
            GameObject projectileInstance = bulletPool.GetBullet(universalProjectilePrefab);
            projectileInstance.transform.position = muzzleTransform.position;
            projectileInstance.transform.rotation = muzzleTransform.rotation;
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

    protected virtual void PhysicallyEjectObject(GameObject objToEject)
    {
        if (objToEject == null) return;
        objToEject.transform.SetParent(null);

        Collider col = objToEject.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true; // Włącz kolizje z otoczeniem
        }

        Rigidbody rb = objToEject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;

            if (ejectionPort != null)
            {
                objToEject.transform.position = ejectionPort.position;
                objToEject.transform.rotation = ejectionPort.rotation;

                // 🔹 NOWE: Dodajemy prędkość broni do łuski
                // Najpierw ustawiamy bazową prędkość (dziedziczoną od broni)
                rb.linearVelocity = currentGunVelocity * velocityInheritance;

                rb.angularVelocity = Vector3.zero;

                // Potem dodajemy siłę wyrzutu (lokalną przekształconą na światową)
                Vector3 worldForce = ejectionPort.TransformDirection(ejectionForce);
                Vector3 worldTorque = ejectionPort.TransformDirection(ejectionTorque);

                rb.AddForce(worldForce, ForceMode.Impulse);
                rb.AddTorque(worldTorque, ForceMode.Impulse);
            }
            else
            {
                Debug.LogError($"Brak 'ejectionPort' w {name}! Łuska spada na (0,0,0).", this);
            }
        }

        Casing casingScript = objToEject.GetComponent<Casing>();
        if (casingScript != null) casingScript.enabled = true;
    }

    protected virtual GameObject SpawnAndChamberCasing(GameObject casingPrefab)
    {
        if (casingPool == null || casingPrefab == null) return null;
        GameObject casingInstance = casingPool.GetCasing(casingPrefab);

        if (chamberTransform != null)
        {
            casingInstance.transform.position = chamberTransform.position; // Fix pozycji
            casingInstance.transform.rotation = chamberTransform.rotation;
            casingInstance.transform.SetParent(chamberTransform);
        }

        Rigidbody rb = casingInstance.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Casing casingScript = casingInstance.GetComponent<Casing>();
        if (casingScript != null) casingScript.enabled = false;

        return casingInstance;
    }

    protected virtual Bullet GetChamberedBulletData()
    {
        if (chamberedRound == null) { OnDryFire?.Invoke(); return null; }
        Bullet ammoData = chamberedRound.GetComponent<Bullet>();
        if (ammoData == null) { OnDryFire?.Invoke(); return null; }
        return ammoData;
    }

    protected virtual bool FireOnce()
    {
        if (isBoltLockedBack || !bolt.IsBoltForward) { OnDryFire?.Invoke(); return false; }
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
        isBoltLockedBack = false;
    }

    protected virtual void OnChargingHandleReleased()
    {
        isBoltLockedBack = false;
        TryChamberFromMagazine();
        OnBoltReleasedEvent?.Invoke();
    }

    public virtual void ReleaseBoltAction(bool force = false)
    {
        if (!force && !CanReleaseBolt()) return;
        TryChamberFromMagazine();
        isBoltLockedBack = false;
        OnBoltReleasedEvent?.Invoke();
    }

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
            chamberedRound.transform.position = chamberTransform.position; // Fix pozycji
            chamberedRound.transform.rotation = chamberTransform.rotation;
            chamberedRound.transform.SetParent(chamberTransform);
            Rigidbody rb = chamberedRound.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Collider col = chamberedRound.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false; // Wyłączamy kolizje, żeby nie gryzło się z bronią
            }
            chamberedRound.SetActive(true);
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
}