using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// PistolPlatform - finalna wersja:
/// - używa ChargingHandle (nie Bolta)
/// - blokuje zamek po pustym magazynku
/// - fizycznie odciąga handle do maxLocalY przy pustym magazynku
/// - po przeładowaniu handle można zwolnić i wtedy chamberuje
/// - zabezpieczenia przed podwójnym chamberowaniem
/// </summary>
public class PistolPlatform : WeaponControllerBase
{
    [Header("Timing / Guards")]
    [Tooltip("Minimalny odstęp pomiędzy kolejnymi ReleaseBoltAction (sekundy)")]
    public float releaseDebounce = 0.08f;
    private float lastReleaseTime = -999f;

    [Tooltip("Jeśli niedawno nastąpiło chamberowanie, ReleaseBoltAction nie chamberuje ponownie")]
    public float chamberIgnoreWindow = 0.12f;
    private float lastChamberTime = -999f;

    protected override void Awake()
    {
        base.Awake();

        // Pistolety nie mają bolta, tylko charging handle
        bolt = null;

        // Charging handle może się blokować (domyślnie)
        if (chargingHandle != null)
            chargingHandle.lockOnFullPull = true;
    }

    protected override bool FireOnce()
    {
        // Jeśli handle nie w spoczynku — blokujemy strzał
        if (chargingHandle != null &&
            chargingHandle.transform.localPosition.y > chargingHandle.minLocalY + 0.001f)
        {
            return false;
        }

        if (!isChambered)
        {
            OnDryFire?.Invoke();
            return false;
        }

        // Strzał
        OnFire?.Invoke();
        isChambered = false;

        // Spróbuj chamberować tylko jeśli jest nabój w magazynku
        if (ammoSocket != null && ammoSocket.currentMagazine != null &&
            ammoSocket.currentMagazine.currentRounds > 0)
        {
            bool chambered = TryChamberFromMagazine();
            if (chambered)
            {
                isBoltLockedBack = false;
                if (chargingHandle != null)
                {
                    chargingHandle.lockOnFullPull = false;
                    chargingHandle.GetComponent<XRGrabInteractable>().trackPosition = true;
                }
            }
        }
        else
        {
            // Mag pusty -> blokada zamka i fizyczne odciągnięcie handle
            isBoltLockedBack = true;
            OnBoltLockedBack?.Invoke();

            if (chargingHandle != null)
            {
                chargingHandle.lockOnFullPull = false; // nie myl z mechaniczną blokadą chwytu
                chargingHandle.transform.localPosition = new Vector3(
                    chargingHandle.localX,
                    chargingHandle.maxLocalY,
                    chargingHandle.localZ
                );

                // Wyłącz śledzenie pozycji, dopóki gracz nie puści handle
                var grab = chargingHandle.GetComponent<XRGrabInteractable>();
                if (grab != null)
                    grab.trackPosition = false;
            }
        }

        return true;
    }

    public override bool TryChamberFromMagazine()
    {
        bool result = base.TryChamberFromMagazine();
        if (result)
        {
            lastChamberTime = Time.time;
        }
        return result;
    }

    public override void OnBoltPulled()
    {
        if (isChambered)
        {
            isChambered = false;
            OnRoundEjected?.Invoke();
        }

        if (ammoSocket != null && ammoSocket.currentMagazine != null)
        {
            if (ammoSocket.currentMagazine.currentRounds > 0)
            {
                isBoltLockedBack = false;
                if (chargingHandle != null)
                {
                    chargingHandle.lockOnFullPull = false;
                    var grab = chargingHandle.GetComponent<XRGrabInteractable>();
                    if (grab != null)
                        grab.trackPosition = true;
                }
            }
            else
            {
                isBoltLockedBack = true;
                if (chargingHandle != null)
                {
                    chargingHandle.lockOnFullPull = false;
                    chargingHandle.transform.localPosition = new Vector3(
                        chargingHandle.localX,
                        chargingHandle.maxLocalY,
                        chargingHandle.localZ
                    );
                    var grab = chargingHandle.GetComponent<XRGrabInteractable>();
                    if (grab != null)
                        grab.trackPosition = false;
                }
                OnBoltLockedBack?.Invoke();
            }
        }
        else
        {
            isBoltLockedBack = false;
            if (chargingHandle != null)
            {
                chargingHandle.lockOnFullPull = false;
                var grab = chargingHandle.GetComponent<XRGrabInteractable>();
                if (grab != null)
                    grab.trackPosition = true;
            }
        }
    }

    public override void ReleaseBoltAction(bool force = false)
    {
        if (Time.time - lastReleaseTime < releaseDebounce && !force)
            return;
        lastReleaseTime = Time.time;

        if (Time.time - lastChamberTime < chamberIgnoreWindow && !force)
        {
            if (isBoltLockedBack)
            {
                isBoltLockedBack = false;
                if (chargingHandle != null)
                {
                    chargingHandle.lockOnFullPull = false;
                    var grab = chargingHandle.GetComponent<XRGrabInteractable>();
                    if (grab != null)
                        grab.trackPosition = true;
                }
            }
            OnBoltReleasedEvent?.Invoke();
            return;
        }

        if (isChambered)
        {
            isBoltLockedBack = false;
            if (chargingHandle != null)
            {
                chargingHandle.lockOnFullPull = false;
                var grab = chargingHandle.GetComponent<XRGrabInteractable>();
                if (grab != null)
                    grab.trackPosition = true;
            }
            OnBoltReleasedEvent?.Invoke();
            return;
        }

        bool chambered = TryChamberFromMagazine();

        if (chambered)
        {
            isBoltLockedBack = false;
            if (chargingHandle != null)
            {
                chargingHandle.lockOnFullPull = false;
                var grab = chargingHandle.GetComponent<XRGrabInteractable>();
                if (grab != null)
                    grab.trackPosition = true;
            }
        }
        else
        {
            if (ammoSocket != null && ammoSocket.currentMagazine != null &&
                ammoSocket.currentMagazine.currentRounds == 0)
            {
                isBoltLockedBack = true;
                if (chargingHandle != null)
                {
                    chargingHandle.lockOnFullPull = false;
                    chargingHandle.transform.localPosition = new Vector3(
                        chargingHandle.localX,
                        chargingHandle.maxLocalY,
                        chargingHandle.localZ
                    );
                    var grab = chargingHandle.GetComponent<XRGrabInteractable>();
                    if (grab != null)
                        grab.trackPosition = false;
                }
                OnBoltLockedBack?.Invoke();
            }
        }

        OnBoltReleasedEvent?.Invoke();
    }
}