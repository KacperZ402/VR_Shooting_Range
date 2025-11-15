using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
public class ShotgunPump : ChargingHandle
{
    protected override void LateUpdate()
    {
        if (weaponControllerBase.weaponGrab == null || !weaponControllerBase.weaponGrab.IsGripHeld)
        {
            return;
        }
        float clampedY = transform.localPosition.y;

        if (isGrabbed)
        {
            // 🔹 Ograniczamy do zakresu minLocalY - maxLocalY
            clampedY = Mathf.Clamp(clampedY, minLocalY, maxLocalY);

            // 🔹 Wyzwalamy OnBoltPulled jeśli osiągnięto maxLocalY
            if (!boltPulledTriggered && Mathf.Approximately(clampedY, maxLocalY))
            {
                boltPulledTriggered = true;
                OnBoltPulled?.Invoke();
            }

            // 🔹 Wyzwalamy OnBoltReleased jeśli wrócono do przodu
            if (boltPulledTriggered && clampedY <= minLocalY + 0.001f)
            {
                boltPulledTriggered = false;
                OnBoltReleased?.Invoke();
            }

            // 🔹 Ustawiamy pompke w ograniczonej pozycji
            transform.localPosition = new Vector3(localX, clampedY, localZ);
        }
        else
        {
            // Pompka poza zakresem? ustawiamy ją na najbliższą granicę
            clampedY = Mathf.Clamp(transform.localPosition.y, minLocalY, maxLocalY);

            // Wyzwalanie zdarzeń również po „przesunięciu” ręką
            if (!boltPulledTriggered && Mathf.Approximately(clampedY, maxLocalY))
            {
                boltPulledTriggered = true;
                OnBoltPulled?.Invoke();
            }

            if (boltPulledTriggered && clampedY <= minLocalY + 0.001f)
            {
                boltPulledTriggered = false;
                OnBoltReleased?.Invoke();
            }

            // Ustawiamy pompke w granicy
            transform.localPosition = new Vector3(localX, clampedY, localZ);
            rb.isKinematic = true;
        }

        transform.localScale = Vector3.one;

        if (transform.parent != parentTransform)
            transform.SetParent(parentTransform, true);
    }
    protected override void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        rb.isKinematic = false;
        transform.SetParent(parentTransform, true);
        if (rotateOnGrab)
            transform.localRotation = Quaternion.Euler(0f, 0f, grabRotationZ);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}