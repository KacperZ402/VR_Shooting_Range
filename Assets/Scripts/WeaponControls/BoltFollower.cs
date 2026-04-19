using System.Collections;
using UnityEngine;

public class BoltFollower : MonoBehaviour
{
    [Header("Refs")]
    public Transform chargingHandle;
    public WeaponControllerBase weaponController;

    [Header("Tracking")]
    public float yEpsilon = 0.0001f;
    public float lockedBackY = 0.05f;

    [Header("Shoot animation")]
    public int holdFrames = 1;
    public int returnFrames = 2;

    private Vector3 localStartPos;
    private Transform parentTransform;
    private Coroutine effectCoroutine;
    private bool isAnimating = false;

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
        if (isAnimating) return;

        if (!weaponController.weaponGrab.IsGripHeld) return;

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
        if (effectCoroutine != null) StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(KickBackAndReturn(lockedBackY, holdFrames, returnFrames));
    }

    private IEnumerator KickBackAndReturn(float lockedBackY, int holdFrames, int returnFrames)
    {
        isAnimating = true;

        Vector3 startPos = transform.localPosition;
        Vector3 kickPos = new Vector3(localStartPos.x, localStartPos.y + Mathf.Abs(lockedBackY), localStartPos.z);

        transform.localPosition = kickPos;

        // Przytrzymanie
        for (int i = 0; i < holdFrames; i++)
            yield return null;

        // Powrót
        for (int step = 1; step <= Mathf.Max(1, returnFrames); step++)
        {
            float t = (float)step / returnFrames;
            transform.localPosition = Vector3.Lerp(kickPos, startPos, t);
            yield return null;
        }

        transform.localPosition = startPos;
        isAnimating = false;
        effectCoroutine = null;
    }

    public void OnFireKick()
    {
        StartKickbackAnimation(lockedBackY, holdFrames, returnFrames);
    }
}