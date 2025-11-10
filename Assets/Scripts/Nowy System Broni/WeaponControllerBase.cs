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

    protected virtual void Awake()
    {
        // Pobieramy oba menedżery
        ammoPool = AmmoPoolManager.Instance;
        bulletPool = BulletPoolManager.Instance;

        if (ammoPool == null) Debug.LogError("Nie znaleziono AmmoPoolManager!", this);
        if (bulletPool == null) Debug.LogError("Nie znaleziono BulletPoolManager!", this);

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

    public virtual void FireInput(bool pressed)
    {
        if (weaponGrab != null && !weaponGrab.IsGripHeld) return;
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

    // -------------------------- FUNKCJA BALISTYKI --------------------------

    /// <summary>
    /// Główna logika balistyczna. Pobiera dane z naboju, oblicza trajektorię
    /// i wystrzeliwuje pocisk(i) z puli.
    /// </summary>
    /// <param name="ammoData">Komponent 'Bullet' z instancji naboju w komorze.</param>
    protected virtual void SpawnProjectile(Bullet ammoData)
    {
        // 1. Sprawdź, czy mamy czym strzelać
        if (universalProjectilePrefab == null)
        {
            Debug.LogError("Brak 'universalProjectilePrefab' na broni!", this);
            return;
        }

        // 2. Oblicz prędkość wylotową
        float muzzleVelocity = Mathf.Sqrt((2f * ammoData.muzzleEnergy) / ammoData.mass) * velocityMultiplier;
        float drag = ammoData.dragCoefficient;
        int count = ammoData.projectileCount; // Dla śrutu

        // 3. Wystrzel wymaganą liczbę pocisków
        for (int i = 0; i < count; i++)
        {
            // 4. Pobierz instancję POCISKU z puli pocisków
            GameObject projectileInstance = bulletPool.GetBullet(universalProjectilePrefab);

            projectileInstance.transform.position = muzzleTransform.position;
            projectileInstance.transform.rotation = muzzleTransform.rotation;

            Projectile projectileLogic = projectileInstance.GetComponent<Projectile>();
            if (projectileLogic != null)
            {
                projectileLogic.Launch(projectileInstance.transform.forward * muzzleVelocity, drag);
            }
        }
    }

    protected virtual Bullet GetChamberedBulletData()
    {
        if (chamberedRound == null)
        {
            OnDryFire?.Invoke(); // Pusta komora
            return null;
        }

        Bullet ammoData = chamberedRound.GetComponent<Bullet>();
        if (ammoData == null)
        {
            // Krytyczny błąd - obiekt w komorze nie ma danych
            Debug.LogError("Nabój w komorze nie ma komponentu Bullet!", this);

            // Zwróć zły obiekt do puli, żeby posprzątać
            if (ammoPool != null)
                ammoPool.ReturnRound(chamberedRound);
            else
                Destroy(chamberedRound);

            chamberedRound = null;
            OnDryFire?.Invoke();
            return null;
        }

        return ammoData;
    }
    protected virtual bool FireOnce()
    {
        // 1. Sprawdź warunki broni (np. zamek)
        if (isBoltLockedBack || !bolt.IsBoltForward)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // 2. 🔹 UPROSZCZENIE: Pobierz dane (funkcja sama obsłuży błędy i OnDryFire)
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            return false; // Błąd został już obsłużony w GetChamberedBulletData
        }

        // 3. Wystrzel
        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        // 4. Zwróć zużytą amunicję
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound);

        chamberedRound = null;

        // 5. Przeładuj (dla semi-auto)
        TryChamberFromMagazine();
        return true;
    }

    protected virtual void HandleBoltActionFire()
    {
        // 1. Sprawdź warunki broni
        if (isBoltLockedBack || !bolt.IsBoltForward)
        {
            OnDryFire?.Invoke();
            return;
        }

        // 2. 🔹 UPROSZCZENIE: Pobierz dane
        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null)
        {
            return; // Błąd obsłużony
        }

        // 3. Wystrzel
        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        // 4. Zwróć zużytą amunicję
        if (ammoPool != null)
            ammoPool.ReturnRound(chamberedRound);
        else
            Destroy(chamberedRound);

        chamberedRound = null;
    }

    // -------------------------- ZAMEK (BEZ ZMIAN) --------------------------

    public virtual void OnBoltPulled()
    {
        if (chamberedRound != null)
        {
            OnRoundEjected?.Invoke();

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

    public virtual bool TryChamberFromMagazine()
    {
        if (chamberedRound != null) return true;

        if (ammoSocket == null)
            return false;

        GameObject roundToChamber = ammoSocket.TryTakeRound();

        if (roundToChamber == null)
        {
            Debug.Log("Magazynek Jest pusty, podczas próby załadowania naboju");
            return false;
        }

        Bullet bulletData = roundToChamber.GetComponent<Bullet>();
        if (bulletData == null)
        {
            Debug.LogError("Pobrany nabój (instancja) nie ma komponentu 'Bullet'!", this);
            Destroy(roundToChamber);
            return false;
        }

        if (bulletData.caliber != this.caliber)
        {
            Debug.LogWarning($"Próba załadowania złego kalibru! Broń: {this.caliber}, Nabój: {bulletData.caliber}", this);
            Destroy(roundToChamber);
            return false;
        }

        chamberedRound = roundToChamber;
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