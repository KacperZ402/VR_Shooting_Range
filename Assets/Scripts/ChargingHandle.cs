using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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
    public bool lockOnFullPull = true; // jeśli true -> zamek się blokuje po pełnym odciągnięciu
    public float lockRotationY = 50f;  // kąt obrotu po blokadzie

    [Header("Obrót podczas chwytu")]
    public bool rotateOnGrab = false;  // jeśli true -> obraca się o -90° w osi Z podczas chwytu
    public float grabRotationZ = -90f; // kąt obrotu podczas chwytu

    [Header("Eventy")]
    public UnityEvent OnBoltPulled;
    public UnityEvent OnBoltReleased;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isGrabbed = false;

    private Vector3 localStartPos;
    private bool boltPulledTriggered = false;
    private Transform parentTransform;

    private bool isLocked = false;
    private Quaternion lockedRotation;
    private Quaternion initialRotation;

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
        if (isLocked)
            return;

        isGrabbed = true;
        rb.isKinematic = false;
        transform.SetParent(parentTransform, true);

        // ✅ Jeśli włączono rotateOnGrab -> obrót w osi Z
        if (rotateOnGrab)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, grabRotationZ);
        }
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // ✅ Po puszczeniu wracamy do rotacji początkowej (tylko jeśli nie zablokowany)
        if (!isLocked)
        {
            transform.localRotation = initialRotation;
        }
    }

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        // jeśli zamek był zablokowany, to go “zrzucamy”
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
        if (isLocked)
        {
            // zamek zablokowany – trzyma pozycję i rotację
            transform.localPosition = new Vector3(localX, maxLocalY, localZ);
            transform.localRotation = lockedRotation;
            return;
        }

        // reset X/Z i rotację (tylko gdy nie trzymamy)
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

            // pełne odciągnięcie
            if (clampedY >= maxLocalY && !boltPulledTriggered)
            {
                boltPulledTriggered = true;
                OnBoltPulled?.Invoke();

                // tylko jeśli opcja blokady jest włączona
                if (lockOnFullPull)
                {
                    isLocked = true;
                    rb.isKinematic = true;

                    // ✅ jeśli rotateOnGrab = true, zostaje z obrotem Z = grabRotationZ + Y = 50°
                    float finalY = lockRotationY;
                    float finalZ = rotateOnGrab ? grabRotationZ : 0f;

                    lockedRotation = Quaternion.Euler(0f, finalY, finalZ);
                    transform.localRotation = lockedRotation;
                }
            }

            // powrót do przodu podczas trzymania
            if (clampedY <= minLocalY + 0.001f && boltPulledTriggered)
            {
                boltPulledTriggered = false;
                OnBoltReleased?.Invoke();
            }
        }
        else
        {
            // automatyczny powrót
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

        // upewnienie się, że ma poprawnego rodzica
        if (transform.parent != parentTransform)
            transform.SetParent(parentTransform, true);
    }
}
