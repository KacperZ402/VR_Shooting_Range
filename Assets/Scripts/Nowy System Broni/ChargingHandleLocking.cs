using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ChargingHandleLocking : ChargingHandle
{
    [Header("Blokowanie przy pustym magazynku")]
    [Tooltip("Czy automatycznie blokowaæ zamek po pustym magazynku?")]
    public bool enableAutoLockOnEmptyMag = true;

    private bool simpleLocked = false;

    protected override void Awake()
    {
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

        base.OnDestroy();
    }

    private void OnBoltLockedBackFromWeapon()
    {
        if (!enableAutoLockOnEmptyMag) return;
        if (simpleLocked) return;

        var mag = weaponControllerBase?.ammoSocket?.currentMagazine;
        if (mag != null && mag.currentRounds == 0) // tylko w przypadku wpiêtego pustego magazynka
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

    protected override void OnGrab(SelectEnterEventArgs args)
    {
        var mag = weaponControllerBase?.ammoSocket?.currentMagazine;

        // Grab mo¿liwy tylko jeœli brak magazynka lub mag ma naboje
        if (simpleLocked)
        {
            if (mag == null || mag.currentRounds > 0)
            {
                UnlockSimpleLock();
                weaponControllerBase.ReleaseBoltAction(true);
            }
            else
            {
                // mag pusty -> grab zablokowany
                return;
            }
        }

        base.OnGrab(args);
    }

    protected override void LateUpdate()
    {
        var mag = weaponControllerBase?.ammoSocket?.currentMagazine;

        // Sprawdzenie blokady i dostêpnoœci graba
        if (simpleLocked)
        {
            transform.localPosition = new Vector3(localX, maxLocalY, localZ);

            // Przywróæ parent
            if (transform.parent != parentTransform)
                transform.SetParent(parentTransform, true);

            // Grab aktywny tylko gdy mo¿na zrzuciæ zamek
            if (mag == null || mag.currentRounds > 0)
                grabInteractable.enabled = true;
            else
                grabInteractable.enabled = false;

            return;
        }

        grabInteractable.enabled = true;
        base.LateUpdate();
    }
}