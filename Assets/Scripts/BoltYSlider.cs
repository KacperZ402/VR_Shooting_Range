using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ChargingHandle : MonoBehaviour
{
    [Header("Zakres ruchu (lokalne)")]
    public float minLocalY = 0f;
    public float maxLocalY = 0.05f;

    [Header("Powrót")]
    public float returnSpeed = 5f;

    [Header("Event")]
    public UnityEvent OnBoltPulled;
    public UnityEvent OnBoltReleased; // nowy event

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isGrabbed = false;

    private Vector3 localStartPos;
    private bool boltPulledTriggered = false; // oznacza: zamek został odciągnięty do końca przynajmniej raz
    private Transform parentTransform;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // domyślnie kinematic
        localStartPos = transform.localPosition;
        parentTransform = transform.parent;

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = false;
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        rb.isKinematic = false; // pozwalamy na ruch
        transform.SetParent(parentTransform, true);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        // nie czyścimy boltPulledTriggered tutaj — będziemy je czyścić dopiero gdy zamek wróci do przodu
    }

    void LateUpdate()
    {
        // zawsze resetujemy X,Z i rotację
        transform.SetLocalPositionAndRotation(new Vector3(0f, transform.localPosition.y, 0f), Quaternion.identity);
        transform.localScale = Vector3.one;

        float clampedY = transform.localPosition.y;

        if (isGrabbed)
        {
            // clampujemy Y podczas chwytu
            clampedY = Mathf.Clamp(clampedY, minLocalY, maxLocalY);
            transform.localPosition = new Vector3(0f, clampedY, 0f);

            // wywołanie eventu przy pełnym odciągnięciu
            if (clampedY >= maxLocalY && !boltPulledTriggered)
            {
                boltPulledTriggered = true;
                OnBoltPulled?.Invoke();
            }

            // NOWOŚĆ: jeśli zamek wróci do startu podczas trzymania, wywołujemy OnBoltReleased
            if (clampedY <= minLocalY + 0.001f && boltPulledTriggered)
            {
                boltPulledTriggered = false;
                OnBoltReleased?.Invoke();
            }
        }
        else
        {
            // wracamy na start
            float newY = Mathf.Lerp(clampedY, minLocalY, Time.deltaTime * returnSpeed);
            transform.localPosition = new Vector3(0f, newY, 0f);

            // gdy osiągniemy start, ustawiamy kinematic z powrotem
            if (Mathf.Abs(transform.localPosition.y - minLocalY) < 0.001f)
                rb.isKinematic = true;

            // jeśli wcześniej zamek był odciągnięty do końca -> emitujemy OnBoltReleased
            if (boltPulledTriggered)
            {
                boltPulledTriggered = false;
                OnBoltReleased?.Invoke();
            }
        }

        // upewnienie się, że nadal ma rodzica
        if (transform.parent != parentTransform)
            transform.SetParent(parentTransform, true);
    }

}