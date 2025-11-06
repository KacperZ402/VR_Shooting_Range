using UnityEngine;
public class HKPlatform : WeaponControllerBase
{
    protected override bool FireOnce()
    {
        if (!bolt.IsBoltForward || !isChambered)
        {
            OnDryFire?.Invoke();
            return false;
        }


        OnFire?.Invoke();
        isChambered = false;
        TryChamberFromMagazine();

        return true;
    }

    public override void ReleaseBoltAction(bool force = false)
    {
        isBoltLockedBack = false;
        if (ammoSocket != null && ammoSocket.currentMagazine != null)
        {
            if (TryChamberFromMagazine())
            {
                Debug.Log("[HKPlatform] Nabój załadowany po zwolnieniu zamka.");
            }
            else
            {
                Debug.Log("[HKPlatform] Mag pusty — brak naboju do chamberowania.");
            }
        }

        OnBoltReleasedEvent?.Invoke();
    }
}