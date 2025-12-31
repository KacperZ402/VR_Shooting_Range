using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors; // Dodaj ten using

// 1. ZMIANA: Dziedziczymy z AnimatedBoltHandle, a nie ChargingHandle
public class ChargingHandleLocking : AnimatedBoltHandle
{
    [Header("Blokowanie przy pustym magazynku")]
    [Tooltip("Czy automatycznie blokowaæ zamek po pustym magazynku?")]
    public bool enableAutoLockOnEmptyMag = true;

    private bool simpleLocked = false;

    protected override void Awake()
    {
        // Wywo³aj Awake() z klasy bazowej (AnimatedBoltHandle -> ChargingHandle)
        base.Awake();

        if (weaponControllerBase != null)
        {
            weaponControllerBase.OnBoltLockedBack.AddListener(OnBoltLockedBackFromWeapon);
        }
    }

    protected override void OnDestroy()
    {
        if (weaponControllerBase != null)
            weaponControllerBase.OnBoltLockedBack.RemoveListener(OnBoltLockedBackFromWeapon);

        // Wywo³aj OnDestroy() z klasy bazowej
        base.OnDestroy();
    }

    private void OnBoltLockedBackFromWeapon()
    {
        if (!enableAutoLockOnEmptyMag) return;
        if (simpleLocked) return;

        var mag = weaponControllerBase?.ammoSocket?.currentMagazine;
        if (mag != null && mag.currentRounds == 0)
        {
            LockBackSimple();
        }
    }

    public void LockBackSimple()
    {
        simpleLocked = true;
        rb.isKinematic = true;
        transform.localPosition = new Vector3(localX, maxLocalY, localZ);
    }

    public void UnlockSimpleLock()
    {
        simpleLocked = false;
        rb.isKinematic = false;
    }

    // 2. ZMIANA: Nadpisujemy OnGrab, aby uwzglêdniæ logikê blokady ORAZ animacji
    protected override void OnGrab(SelectEnterEventArgs args)
    {
        if (isAnimating) return;

        // --- NAPRAWA WYRZUCANIA NABOJU PRZY ŁAPANIU ---
        if (simpleLocked)
        {
            var mag = weaponControllerBase?.ammoSocket?.currentMagazine;
            bool magIsReallyEmpty = (mag != null && mag.currentRounds == 0);

            // Pozwalamy odblokować fizykę, jeśli jest po co (naboje lub brak maga)
            if (!magIsReallyEmpty)
            {
                UnlockSimpleLock();

                // UWAGA: USUNĄŁEM TU LINJKĘ "ReleaseBoltAction"!
                // Dzięki temu broń nie załaduje naboju w momencie chwytu,
                // tylko dopiero jak puścisz zamek.
            }
            else
            {
                return; // Jak pusty magazynek, nie dajemy ruszyć
            }
        }
        // ---------------------------------------------

        base.OnGrab(args);
    }

    // 3. ZMIANA: Nadpisujemy LateUpdate, aby uwzglêdniæ WSZYSTKIE stany
    protected override void LateUpdate()
    {
        if (!weaponControllerBase.weaponGrab.IsGripHeld)
        {
            return;
        }
        if (isAnimating)
        {
            return;
        }


        var mag = weaponControllerBase?.ammoSocket?.currentMagazine;

        // Stan 2: Zamek jest zablokowany (Twoja logika)
        if (simpleLocked)
        {
            transform.localPosition = new Vector3(localX, maxLocalY, localZ);

            if (transform.parent != parentTransform)
                transform.SetParent(parentTransform, true);

            // Aktywuj/deaktywuj graba
            if (mag == null || mag.currentRounds > 0)
                grabInteractable.enabled = true;
            else
                grabInteractable.enabled = false;

            return;
        }

        // Stan 3: Normalne dzia³anie (ani animacja, ani blokada)
        // Wywo³aj LateUpdate() z klasy bazowej (ChargingHandle)
        grabInteractable.enabled = true;
        base.LateUpdate();
    }
}