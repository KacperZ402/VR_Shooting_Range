using UnityEngine;
using System.Collections;

public class ShootingTarget : MonoBehaviour
{
    [Header("Ustawienia Rotacji")]
    public Vector3 uprightRotation = Vector3.zero;
    public Vector3 lieDownRotation = new Vector3(-90f, 0f, 0f);
    public float rotationSpeed = 5f;

    [Header("Dźwięk")]
    public AudioSource audioSource;
    public AudioClip hitSound;

    private bool isActive = false;
    private ShootingRangeManager manager;
    private Coroutine currentMoveCoroutine;

    public void Setup(ShootingRangeManager rngManager)
    {
        manager = rngManager;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Reagujemy na wszystko co fizyczne (kule)
        if (isActive) RegisterHit();
    }

    void OnTriggerEnter(Collider other)
    {
        // Reagujemy na triggery (jeśli pocisk jest triggerem)
        if (isActive) RegisterHit();
    }

    void RegisterHit()
    {
        if (!isActive) return; // Zapobiega podwójnym trafieniom
        isActive = false;

        // 1. Dźwięk
        if (audioSource && hitSound) audioSource.PlayOneShot(hitSound);

        // 2. Najpierw chowamy tę tarczę
        MoveToRotation(lieDownRotation);

        // 3. Potem mówimy managerowi (żeby wiedział, że ma odczekać i wystawić nową)
        if (manager != null)
        {
            manager.RegisterHit(this);
        }
    }

    public void PopUp()
    {
        isActive = true;
        MoveToRotation(uprightRotation);
    }

    public void FoldDown()
    {
        isActive = false;
        MoveToRotation(lieDownRotation);
    }

    void MoveToRotation(Vector3 targetEuler)
    {
        if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);
        currentMoveCoroutine = StartCoroutine(RotateRoutine(targetEuler));
    }

    IEnumerator RotateRoutine(Vector3 targetEuler)
    {
        Quaternion startRot = transform.localRotation;
        Quaternion endRot = Quaternion.Euler(targetEuler);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            transform.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        transform.localRotation = endRot;
    }
}