using UnityEngine;

public class AKPlatform : WeaponControllerBase
{
    protected override void Awake()
    {
        base.Awake();

        // AK: zamek nie blokuje siê po pustym magazynku
        if (chargingHandle != null)
        {
            chargingHandle.lockOnFullPull = false;
        }
    }

    public override void OnBoltPulled()
    {
        // W AK – zawsze mo¿na wyrzuciæ nabój i spróbowaæ za³adowaæ nowy
        if (isChambered)
        {
            isChambered = false;
            OnRoundEjected?.Invoke();
        }

        TryChamberFromMagazine();
    }

    public override void ReleaseBoltAction(bool force = false)
    {
        isBoltLockedBack = false;
        OnBoltReleasedEvent?.Invoke();
    }
}