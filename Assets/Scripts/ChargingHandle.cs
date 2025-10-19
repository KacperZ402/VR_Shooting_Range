using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ChargingHandle : MonoBehaviour
{
    public WeaponControllerBase weaponControllerBase;
    [Header("Zakres ruchu (lokalne)")]
    public float minLocalY = 0f;
    public float maxLocalY = 0.05f;

    [Header("Pozycja lokalna X/Z")]
    public float localX = 0f;
    public float localZ = 0f;

    [Header("Powrót")]
    public float returnSpeed = 5f;

    [Header("Blokada")]
    public bool lockOnFullPull = true;
    public float lockRotationY = 50f;

    [Header("Obrót podczas chwytu")]
    public bool rotateOnGrab = false;
    public float grabRotationZ = -90f;

    [Header("Eventy")]
    public UnityEvent OnBoltPulled;
    public UnityEvent OnBoltReleased;

    [Header("Animacja strzału")]
    public int holdFrames = 1;
    public int returnFrames = 2;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isGrabbed = false;

    private Vector3 localStartPos;
    private bool boltPulledTriggered = false;
    private Transform parentTransform;

    private bool isLocked = false;
    private Quaternion lockedRotation;
    private Quaternion initialRotation;

    // Nowe pola do animacji
    private bool isAnimating = false;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        localStartPos = transform.localPosition;
        parentTransform = transform.parent;
        initialRotation = transform.localRotation;

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.hoverEntered.AddListener(OnHoverEnter);

        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = false;
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
        grabInteractable.hoverEntered.RemoveListener(OnHoverEnter);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (isLocked) return;

        isGrabbed = true;
        rb.isKinematic = false;
        transform.SetParent(parentTransform, true);

        if (rotateOnGrab)
            transform.localRotation = Quaternion.Euler(0f, 0f, grabRotationZ);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;

        if (!isLocked)
            transform.localRotation = initialRotation;
    }

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (isLocked)
        {
            isLocked = false;
            boltPulledTriggered = false;
            rb.isKinematic = false;
            weaponControllerBase.ReleaseBoltAction(true);
        }
    }

    void LateUpdate()
    {
        if (isAnimating) return;

        if (isLocked)
        {
            transform.localPosition = new Vector3(localX, maxLocalY, localZ);
            transform.localRotation = lockedRotation;
            return;
        }

        if (!isGrabbed)
        {
            transform.SetLocalPositionAndRotation(
                new Vector3(localX, transform.localPosition.y, localZ),
                Quaternion.identity
            );
        }
        transform.localScale = Vector3.one;

        float clampedY = transform.localPosition.y;

        if (isGrabbed)
        {
            clampedY = Mathf.Clamp(clampedY, minLocalY, maxLocalY);
            transform.localPosition = new Vector3(localX, clampedY, localZ);

            if (clampedY >= maxLocalY && !boltPulledTriggered)
            {
                boltPulledTriggered = true;
                OnBoltPulled?.Invoke();

                if (lockOnFullPull)
                {
                    isLocked = true;
                    rb.isKinematic = true;

                    float finalY = lockRotationY;
                    float finalZ = rotateOnGrab ? grabRotationZ : 0f;

                    lockedRotation = Quaternion.Euler(0f, finalY, finalZ);
                    transform.localRotation = lockedRotation;
                }
            }

            if (clampedY <= minLocalY + 0.001f && boltPulledTriggered)
            {
                boltPulledTriggered = false;
                OnBoltReleased?.Invoke();
            }
        }
        else
        {
            float newY = Mathf.Lerp(clampedY, minLocalY, Time.deltaTime * returnSpeed);
            transform.localPosition = new Vector3(localX, newY, localZ);

            if (Mathf.Abs(transform.localPosition.y - minLocalY) < 0.001f)
                rb.isKinematic = true;

            if (boltPulledTriggered)
            {
                boltPulledTriggered = false;
                OnBoltReleased?.Invoke();
            }
        }

        if (transform.parent != parentTransform)
            transform.SetParent(parentTransform, true);
    }

    // ===== Dodane animacje =====
    // ===== Poprawiona animacja kickback =====
    public void AnimateKickback()
    {
        if (isAnimating) return;
        StartCoroutine(KickbackCoroutine());
    }

    private IEnumerator KickbackCoroutine()
    {
        isAnimating = true;
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;

        // Start w pozycji spoczynku (zawsze minLocalY)
        Vector3 startPos = new Vector3(localX, minLocalY, localZ);
        Vector3 kickPos = new Vector3(localX, maxLocalY, localZ);

        // Przejdź od aktualnej pozycji do pozycji startowej (jeśli jest inna)
        transform.localPosition = startPos;

        // Przytrzymaj chwilę w cofnięciu
        for (int i = 0; i < holdFrames; i++)
            yield return null;

        // Cofnij do maxLocalY
        for (int step = 1; step <= returnFrames; step++)
        {
            float t = (float)step / returnFrames;
            transform.localPosition = Vector3.Lerp(startPos, kickPos, t);
            yield return null;
        }

        // Powrót do pozycji spoczynku
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

        OnBoltReleased?.Invoke();
    }
}