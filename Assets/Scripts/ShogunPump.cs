using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// System pompki do strzelby — dziedziczy po ChargingHandle.
/// - Ma ograniczony zakres ruchu (minLocalY - maxLocalY)
/// - Wywołuje OnBoltPulled przy maksymalnym cofnięciu
/// - Wywołuje OnBoltReleased przy powrocie do przodu
/// - NIE wraca automatycznie po puszczeniu — zostaje w miejscu
/// </summary>
public class ShotgunPump : ChargingHandle
{
    protected override void LateUpdate()
    {
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