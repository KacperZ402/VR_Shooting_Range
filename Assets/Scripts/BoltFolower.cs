using System.Collections;
using UnityEngine;

public class BoltFollower : MonoBehaviour
{
    [Header("Referencje")]
    public Transform chargingHandle;
    public WeaponController weaponController;

    [Header("Opcje śledzenia")]
    public float yEpsilon = 0.0001f;
    public float lockedBackY = 0.05f;   // 🔥 pozycja cofnięcia zamka

    [Header("Efekt przy strzale")]
    public int holdFrames = 1;     // ile klatek ma zostać w lockedBackY
    public int returnFrames = 2;   // ile klatek trwa powrót

    private Vector3 localStartPos;
    private Transform parentTransform;
    private Coroutine effectCoroutine;
    private bool isAnimating = false;

    [HideInInspector] public bool IsBoltForward => Mathf.Abs(transform.localPosition.y) < 0.001f;

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

        if (weaponController != null)
        {
            // podpinamy event strzału
            weaponController.OnFire.AddListener(OnFireKick);
        }
    }

    void OnDestroy()
    {
        if (weaponController != null)
            weaponController.OnFire.RemoveListener(OnFireKick);
    }

    void LateUpdate()
    {
        if (isAnimating) return; // 🔥 w czasie animacji nie nadpisujemy pozycji

        float targetY = (weaponController != null && weaponController.isBoltLockedBack)
            ? lockedBackY
            : chargingHandle.localPosition.y;

        float currentY = transform.localPosition.y;

        if (Mathf.Abs(currentY - targetY) > yEpsilon)
        {
            transform.SetLocalPositionAndRotation(
                new Vector3(localStartPos.x, targetY, localStartPos.z),
                Quaternion.identity
            );
            transform.localScale = Vector3.one;

            if (transform.parent != parentTransform)
                transform.SetParent(parentTransform, true);
        }
    }

    public void OnFireKick()
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(KickBackAndReturn());
    }

    private IEnumerator KickBackAndReturn()
    {
        isAnimating = true;

        Vector3 kickPos = new(localStartPos.x, lockedBackY, localStartPos.z);

        // cofnięcie do lockedBackY
        transform.localPosition = kickPos;

        // przytrzymanie
        for (int i = 0; i < holdFrames; i++)
            yield return null;

        // powrót w returnFrames klatkach
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
}