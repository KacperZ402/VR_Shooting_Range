using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BoltActionHandle : ChargingHandle
{
    [Header("Bolt Action Settings")]
    [Tooltip("Kąt (w stopniach) w lokalnej osi Y, o który trzeba obrócić dźwignię, aby ją odblokować.")]
    public float unlockAngleY = 60f;

    [Tooltip("Jak blisko kąta docelowego (0 lub unlockAngleY) musi być dźwignia, aby zmienić stan.")]
    public float angleTolerance = 10f;

    [Tooltip("Jak blisko pozycji startowej (minLocalY) musi być dźwignia, aby można było ją zablokować.")]
    public float positionTolerance = 0.01f;

    private bool canPull = false;
    private float startLocalX;
    private float startLocalZ;

    // 🔹 POPRAWKA 1: Zmienna do przechowywania początkowej skali
    private Vector3 initialScale;

    protected override void Awake()
    {
        base.Awake();

        startLocalX = transform.localPosition.x;
        startLocalZ = transform.localPosition.z;

        // 🔹 POPRAWKA 1: Zapisujemy początkową skalę
        initialScale = transform.localScale;

        if (rotateOnGrab)
        {
            Debug.LogWarning("rotateOnGrab było włączone. Wyłączam, aby BoltActionHandle działał poprawnie.");
            rotateOnGrab = false;
        }
    }

    protected override void LateUpdate()
    {
        if (isGrabbed)
        {
            // Odczytujemy aktualne wartości (ustawione przez XR Interactor)
            float targetYPos = transform.localPosition.y;
            float targetYAngle = transform.localEulerAngles.y;
            // Konwertujemy kąt do zakresu -180/180 dla poprawnego clampowania
            if (targetYAngle > 180) targetYAngle -= 360;

            float clampedY;
            float clampedAngle;

            // --- 1. Logika Stanów ---
            bool atStartPos = (targetYPos <= minLocalY + positionTolerance);
            bool atUnlockAngle = (Mathf.Abs(Mathf.DeltaAngle(targetYAngle, unlockAngleY)) < angleTolerance);
            bool atLockAngle = (Mathf.Abs(Mathf.DeltaAngle(targetYAngle, 0)) < angleTolerance);

            if (!canPull && atStartPos && atUnlockAngle)
            {
                canPull = true;
            }
            else if (canPull && atStartPos && atLockAngle)
            {
                canPull = false;
            }

            // --- 2. Ograniczenia (Constraints) ---
            if (canPull)
            {
                // Stan: ODBLOKOWANY (można ciągnąć)
                clampedY = Mathf.Clamp(targetYPos, minLocalY, maxLocalY);
                // Wymuś rotację (zatrzask) - to jest konieczne
                clampedAngle = unlockAngleY;
            }
            else
            {
                // Stan: ZABLOKOWANY (można obracać)
                clampedY = minLocalY;
                // Ogranicz rotację (gracz wykonuje ruch)
                clampedAngle = Mathf.Clamp(targetYAngle, 0, unlockAngleY);
            }

            // --- 3. Zastosuj Ograniczenia ---
            transform.localPosition = new Vector3(startLocalX, clampedY, startLocalZ);

            // 🔹 POPRAWKA 2: Ustawiamy tylko oś Y, zachowując X i Z
            Vector3 currentAngles = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(currentAngles.x, clampedAngle, currentAngles.z);

            // --- 4. Wyzwalanie Eventów (tylko w stanie 'canPull') ---
            if (canPull)
            {
                if (!boltPulledTriggered && Mathf.Approximately(clampedY, maxLocalY))
                {
                    boltPulledTriggered = true;
                    OnBoltPulled?.Invoke();
                }

                if (boltPulledTriggered && clampedY <= minLocalY + positionTolerance)
                {
                    boltPulledTriggered = false;
                    OnBoltReleased?.Invoke();
                }
            }
        }
        else // Kiedy dźwignia nie jest chwycona (logika "snap back")
        {
            float clampedY = Mathf.Clamp(transform.localPosition.y, minLocalY, maxLocalY);
            bool atStart = (clampedY <= minLocalY + positionTolerance);

            // ... (eventy jak w oryginale) ...
            if (!boltPulledTriggered && Mathf.Approximately(clampedY, maxLocalY))
            {
                boltPulledTriggered = true;
                OnBoltPulled?.Invoke();
            }
            if (boltPulledTriggered && clampedY <= minLocalY + positionTolerance)
            {
                boltPulledTriggered = false;
                OnBoltReleased?.Invoke();
            }

            // Logika przywracania stanu
            if (atStart)
            {
                transform.localPosition = new Vector3(startLocalX, minLocalY, startLocalZ);
                Vector3 currentAngles = transform.localEulerAngles;
                transform.localRotation = Quaternion.Euler(currentAngles.x, 0, currentAngles.z);
                canPull = false;
            }
            else
            {
                transform.localPosition = new Vector3(startLocalX, clampedY, startLocalZ);
                Vector3 currentAngles = transform.localEulerAngles;
                transform.localRotation = Quaternion.Euler(currentAngles.x, unlockAngleY, currentAngles.z);
                canPull = true;
            }

            rb.isKinematic = true;
        }
        transform.localScale = initialScale;

        if (transform.parent != parentTransform)
            transform.SetParent(parentTransform, true);
    }

    protected override void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        rb.isKinematic = false;
        transform.SetParent(parentTransform, true);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}