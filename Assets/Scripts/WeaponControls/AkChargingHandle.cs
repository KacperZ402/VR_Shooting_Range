using UnityEngine;
using System.Collections;

// Dziedziczy z ChargingHandle zamiast MonoBehaviour
public class AnimatedBoltHandle : ChargingHandle
{
    [Header("Animacja strza³u")]
    public int holdFrames = 1;
    public int returnFrames = 2;

    protected bool isAnimating = false;

    // Nadpisujemy metodê LateUpdate z klasy bazowej
    protected override void LateUpdate()
    {
        // Dodajemy blokadê na czas animacji
        if (isAnimating) return;

        // Wywo³ujemy ca³¹ logikê ruchu/grabu z klasy bazowej
        base.LateUpdate();
    }

    // ===== Funkcje animacji (nowe/przeniesione) =====
    public void AnimateKickback()
    {
        if (isAnimating) return;
        StartCoroutine(KickbackCoroutine());
    }

    private IEnumerator KickbackCoroutine()
    {
        isAnimating = true;
        // Dziedziczymy 'grabInteractable' z klasy bazowej
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;

        // Dziedziczymy zmienne 'localX', 'minLocalY', 'localZ', 'maxLocalY'
        Vector3 startPos = new Vector3(localX, minLocalY, localZ);
        Vector3 kickPos = new Vector3(localX, maxLocalY, localZ);

        transform.localPosition = startPos;

        for (int i = 0; i < holdFrames; i++)
            yield return null;

        for (int step = 1; step <= returnFrames; step++)
        {
            float t = (float)step / returnFrames;
            transform.localPosition = Vector3.Lerp(startPos, kickPos, t);
            yield return null;
        }

        for (int step = 1; step <= returnFrames; step++)
        {
            float t = (float)step / returnFrames;
            transform.localPosition = Vector3.Lerp(kickPos, startPos, t);
            yield return null;
        }

        transform.localPosition = startPos;
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = false;
        isAnimating = false;

        // Dziedziczymy event 'OnBoltReleased'
        OnBoltReleased?.Invoke();
    }
}