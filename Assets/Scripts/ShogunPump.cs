using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// System pompki do strzelby — dziedziczy po ChargingHandle.
/// - Ma ograniczony zakres ruchu (minLocalY - maxLocalY)
/// - Wywo³uje OnBoltPulled przy maksymalnym cofniêciu
/// - Wywo³uje OnBoltReleased przy powrocie do przodu
/// - NIE wraca automatycznie po puszczeniu — zostaje w miejscu
/// </summary>
public class ShotgunPump : ChargingHandle
{
    protected override void LateUpdate()
    {
        // Nie wywo³ujemy bazowego LateUpdate, bo zmieniamy zachowanie
        if (isGrabbed)
        {
            // Ogranicz ruch tylko w osi Y
            float clampedY = Mathf.Clamp(transform.localPosition.y, minLocalY, maxLocalY);
            transform.localPosition = new Vector3(localX, clampedY, localZ);

            // Jeœli do koñca cofniêta — wywo³aj OnBoltPulled (tylko raz)
            if (!boltPulledTriggered && Mathf.Approximately(clampedY, maxLocalY))
            {
                boltPulledTriggered = true;
                OnBoltPulled?.Invoke();
            }

            // Jeœli wróci³a do przodu — wywo³aj OnBoltReleased
            if (boltPulledTriggered && clampedY <= minLocalY + 0.001f)
            {
                boltPulledTriggered = false;
                OnBoltReleased?.Invoke();
            }
        }
        else
        {
            // Po puszczeniu pompka zostaje tam, gdzie by³a — bez automatycznego powrotu
            rb.isKinematic = true;
        }

        // Utrzymuj pozycjê X/Z i rotacjê
        transform.localPosition = new Vector3(localX, transform.localPosition.y, localZ);
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