using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum MagazineLoadType
{
    PumpAction_LoadForward,
    BoltAction_LoadBack
}

public class ShotgunPlatform : WeaponControllerBase
{
    [Header("Komponenty Strzelby")]
    public ShotgunPump pumpHandleScript;
    private XRGrabInteractable pumpInteractable;

    [Header("System Celowania")]
    [Tooltip("Obiekt wewnątrz broni, który znajduje się w miejscu głównego chwytu (Pivot). Wokół niego będziemy obracać broń.")]
    public Transform recoilPivot;

    // Usunięto muzzleAxis - teraz jest na sztywno "Down Negative Y"

    [Tooltip("Dodatkowa korekta rotacji (Roll/Pitch).")]
    public Vector3 aimRotationOffset = Vector3.zero;

    [Header("Logika Magazynka")]
    public MagazineLoadType magazineLoadLogic = MagazineLoadType.PumpAction_LoadForward;

    // 🔹 ZMIANA: Używamy Loadera zamiast Collidera
    [SerializeField] private MagazineLoader magLoader; // [SerializeField] pozwoli Ci przypisać go ręcznie
    private bool lastLoaderActiveState;

    protected override void Awake()
    {
        base.Awake();
        bolt = null;

        if (pumpHandleScript != null)
            pumpInteractable = pumpHandleScript.GetComponent<XRGrabInteractable>();

        // 🔹 ZMIANA: Szukamy Loadera w dzieciach całej broni (jeśli nie przypisany ręcznie)

        if (magLoader != null)
        {
            lastLoaderActiveState = magLoader.gameObject.activeSelf;
        }
    }

    protected void LateUpdate()
    {
        if (!weaponGrab.IsGripHeld) return;

        bool isTwoHanded = (pumpInteractable != null && pumpInteractable.isSelected);

        if (isTwoHanded)
        {
            weaponGrab.trackRotation = false;
            StabilizeAim();
        }
        else
        {
            weaponGrab.trackRotation = true;
        }
    }

    private void StabilizeAim()
    {
        if (weaponGrab.interactorsSelecting.Count == 0 || pumpInteractable.interactorsSelecting.Count == 0) return;

        Transform mainHand = weaponGrab.interactorsSelecting[0].transform;
        Transform pumpHand = pumpInteractable.interactorsSelecting[0].transform;

        Vector3 direction = pumpHand.position - mainHand.position;

        if (direction.sqrMagnitude > 0.05f)
        {
            // 1. Obliczamy bazową rotację
            Quaternion targetRotation = Quaternion.LookRotation(direction, mainHand.up);

            // 2. KOREKTA DLA "DOWN NEGATIVE Y" (Na sztywno -90 stopni na osi X)
            // To odpowiada Twojemu poprzedniemu ustawieniu: WeaponFacingAxis.Down_NegativeY
            Quaternion correction = Quaternion.Euler(-90, 0, 0);

            Quaternion manualOffset = Quaternion.Euler(aimRotationOffset);

            // 3. Łączymy rotacje
            targetRotation = targetRotation * correction * manualOffset;
            transform.rotation = targetRotation;

            // 4. Pivot Fix (dociągnięcie gripu do dłoni)
            Vector3 localGripPos = Vector3.zero;
            if (weaponGrab.attachTransform != null)
            {
                localGripPos = weaponGrab.attachTransform.localPosition;
            }

            Vector3 rotatedGripOffset = transform.rotation * localGripPos;
            transform.position = mainHand.position - rotatedGripOffset;
        }
    }

    protected override void Update()
    {
        base.Update();
        HandleMagazineLoaderLogic();
    }

    private void HandleMagazineLoaderLogic()
    {
        if (magLoader == null || chargingHandle == null) return;

        bool shouldBeActive = false;
        float currentY = chargingHandle.transform.localPosition.y;

        switch (magazineLoadLogic)
        {
            case MagazineLoadType.PumpAction_LoadForward:
                // Włączony tylko gdy pompka jest z przodu (zamknięta)
                shouldBeActive = Mathf.Abs(currentY - chargingHandle.minLocalY) < 0.001f;
                break;
            case MagazineLoadType.BoltAction_LoadBack:
                // Włączony tylko gdy zamek jest z tyłu
                shouldBeActive = Mathf.Abs(currentY - chargingHandle.maxLocalY) < 0.001f;
                break;
        }

        if (shouldBeActive != lastLoaderActiveState)
        {
            magLoader.gameObject.SetActive(shouldBeActive);
            lastLoaderActiveState = shouldBeActive;
        }
    }
    protected override bool FireOnce()
    {
        if (chargingHandle != null && chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f) return false;

        Bullet ammoData = GetChamberedBulletData();
        if (ammoData == null) return false;

        SpawnProjectile(ammoData);
        OnFire?.Invoke();

        GameObject casingPrefab = ammoData.casingPrefab;
        if (ammoPool != null) ammoPool.ReturnRound(chamberedRound); else Destroy(chamberedRound);
        chamberedRound = null;

        chamberedRound = SpawnAndChamberCasing(casingPrefab);
        return true;
    }
}