using System.Collections;
using UnityEngine;

public class BoltFollower : MonoBehaviour
{
    [Header("Referencje")]
    public Transform chargingHandle;
    public WeaponControllerBase weaponController;

    [Header("Tryb działania")]
    [Tooltip("Jeśli true, zamek jest fizycznie połączony z rączką przeładowania (np. AK)")]
    public bool boltLinkedToHandle = false;

    [Header("Opcje śledzenia")]
    public float yEpsilon = 0.0001f;
    public float lockedBackY = 0.05f;

    [Header("Efekt przy strzale")]
    public int holdFrames = 1;
    public int returnFrames = 2;

    private Vector3 localStartPos;
    private Transform parentTransform;
    private Coroutine effectCoroutine;
    private bool isAnimating = false;
    private bool isSameObject = false;

    [HideInInspector] public bool IsBoltForward => Mathf.Abs(transform.localPosition.y - localStartPos.y) < 0.001f;

    void Awake()
    {
        if (chargingHandle == null)
        {
            Debug.LogError("[BoltFollower] chargingHandle nie jest przypisany!");
            enabled = false;
            return;
        }

        parentTransform = transform.parent;
        localStartPos = transform.localPosition;

        // 🔹 wykrywanie, czy bolt i handle to ten sam obiekt (np. AK)
        isSameObject = (chargingHandle == transform);

        if (weaponController != null)
            weaponController.OnFire.AddListener(OnFireKick);
    }

    void OnDestroy()
    {
        if (weaponController != null)
            weaponController.OnFire.RemoveListener(OnFireKick);
    }

    void LateUpdate()
    {
        if (isAnimating)
            return;

        // 🔹 Jeśli AK (boltLinkedToHandle) i to ten sam obiekt — brak animacji
        if (boltLinkedToHandle && isSameObject)
            return;

        // 🔹 W trybie AK, ale różne obiekty — ustawiamy bolt względem handle
        if (boltLinkedToHandle && !isSameObject)
        {
            transform.localPosition = chargingHandle.localPosition;
            transform.localRotation = chargingHandle.localRotation;
            return;
        }

        // 🔹 Normalne zachowanie (AR / HK)
        float targetY = (weaponController != null && weaponController.isBoltLockedBack)
            ? lockedBackY
            : chargingHandle.localPosition.y;

        if (Mathf.Abs(transform.localPosition.y - targetY) > yEpsilon)
        {
            transform.localPosition = new Vector3(localStartPos.x, targetY, localStartPos.z);
        }
    }

    // 🔹 Animacja strzału
    public void StartKickbackAnimation(float lockedBackY, int holdFrames, int returnFrames)
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(KickBackAndReturn(lockedBackY, holdFrames, returnFrames));
    }

    private IEnumerator KickBackAndReturn(float lockedBackY, int holdFrames, int returnFrames)
    {
        isAnimating = true;

        Vector3 startPos = transform.localPosition;
        Vector3 kickPos = isSameObject
            ? localStartPos + new Vector3(0f, lockedBackY, 0f)   // AK: bolt == handle
            : new Vector3(localStartPos.x, localStartPos.y + lockedBackY, localStartPos.z); // AR/HK

        transform.localPosition = kickPos;

        // Przytrzymanie
        for (int i = 0; i < holdFrames; i++)
            yield return null;

        // Powrót
        for (int step = 1; step <= Mathf.Max(1, returnFrames); step++)
        {
            float t = (float)step / returnFrames;
            transform.localPosition = Vector3.Lerp(kickPos, localStartPos, t);
            yield return null;
        }

        transform.localPosition = localStartPos;
        isAnimating = false;
        effectCoroutine = null;
    }

    // 🔹 Łączenie bolt + handle (np. dla AK)
    public void LinkBoltWithHandle(bool linked)
    {
        boltLinkedToHandle = linked;
        isSameObject = (chargingHandle == transform);
    }

    public void OnFireKick()
    {
        StartKickbackAnimation(lockedBackY, holdFrames, returnFrames);
    }
}