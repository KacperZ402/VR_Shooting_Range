using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ChargingHandle : MonoBehaviour
{
    [Header("Referencje")]
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

    // --- SEKCJA ANIMACJI ZOSTAŁA USUNIĘTA ---

    protected XRGrabInteractable grabInteractable;
    protected Rigidbody rb;
    protected bool isGrabbed = false;

    protected Vector3 localStartPos;
    protected bool boltPulledTriggered = false;
    protected Transform parentTransform;

    protected bool isLocked = false;
    protected Quaternion lockedRotation;
    protected Quaternion initialRotation;

    protected virtual void Awake()
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

    protected virtual void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
        grabInteractable.hoverEntered.RemoveListener(OnHoverEnter);
    }

    protected virtual void OnGrab(SelectEnterEventArgs args)
    {
        if (isLocked) return;
        isGrabbed = true;
        rb.isKinematic = false;
        transform.SetParent(parentTransform, true);
        if (rotateOnGrab)
            transform.localRotation = Quaternion.Euler(0f, 0f, grabRotationZ);
    }

    protected virtual void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        if (!isLocked)
            transform.localRotation = initialRotation;
    }

    protected virtual void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (isLocked)
        {
            isLocked = false;
            boltPulledTriggered = false;
            rb.isKinematic = false;
            weaponControllerBase.ReleaseBoltAction(true);
        }
    }

    // Zmieniono na 'virtual', aby klasa pochodna mogła ją nadpisać
    protected virtual void LateUpdate()
    {
        // 'isAnimating' zostało usunięte

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
}