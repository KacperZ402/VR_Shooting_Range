using UnityEngine;
using System.Collections;

public class ShootingTarget : MonoBehaviour
{
    [Header("Ustawienia Rotacji")]
    public Vector3 uprightRotation = Vector3.zero;      // Pozycja stojąca
    public Vector3 lieDownRotation = new Vector3(-90f, 0f, 0f); // Pozycja leżąca
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

    // 🔥 ZMIANA: Reagujemy na JAKĄKOLWIEK kolizję fizyczną
    void OnCollisionEnter(Collision collision)
    {
        // Jeśli tarcza jest aktywna (stoi) i coś jej dotknęło -> trafienie
        if (isActive)
        {
            RegisterHit();
        }
    }

    // Opcjonalnie: Reagujemy też na triggery (jeśli pociski są triggerami)
    void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            RegisterHit();
        }
    }

    void RegisterHit()
    {
        isActive = false; // Blokujemy, żeby nie zaliczyło 2 razy

        if (audioSource && hitSound) audioSource.PlayOneShot(hitSound);
        if (manager != null) manager.AddScore();

        MoveToRotation(lieDownRotation);
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