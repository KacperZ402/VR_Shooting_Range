using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private BulletPoolManager bulletPool; // Referencja do puli pocisków

    [Tooltip("Czas życia pocisku w sekundach (zabezpieczenie)")]
    public float maxLifetime = 10.0f;

    private bool isLaunched = false;
    private float dragCoefficient; // 🔹 ZMIANA: Przechowuje opór nadany przy starcie

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bulletPool = BulletPoolManager.Instance;

        if (bulletPool == null)
        {
            Debug.LogError("Projectile nie może znaleźć BulletPoolManager!");
        }
    }

    void OnEnable()
    {
        Invoke(nameof(ReturnToPool), maxLifetime);
        isLaunched = false;
    }

    void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Wystrzeliwuje pocisk. Wywoływane przez WeaponController.
    /// </summary>
    // 🔹 ZMIANA: Przyjmuje teraz 'drag' z naboju
    public void Launch(Vector3 initialVelocity, float drag)
    {
        rb.linearVelocity = initialVelocity;
        this.dragCoefficient = drag; // Zapisuje opór dla tej instancji
        isLaunched = true;
    }

    void FixedUpdate()
    {
        if (!isLaunched || rb.linearVelocity == Vector3.zero) return;

        // Obracanie pocisku w kierunku lotu
        transform.rotation = Quaternion.LookRotation(rb.linearVelocity);

        // 🔹 ZMIANA: Używa zapisanego 'dragCoefficient'
        Vector3 dragForce = -rb.linearVelocity.normalized * rb.linearVelocity.sqrMagnitude * this.dragCoefficient;

        rb.AddForce(dragForce, ForceMode.Force);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isLaunched) return;

        // TODO: Dodać logikę obrażeń, rykoszetów, efektów trafienia
        Debug.Log($"[Projectile] Trafiono: {collision.gameObject.name}");

        ReturnToPool();
    }

    void ReturnToPool()
    {
        isLaunched = false;
        if (bulletPool != null)
        {
            bulletPool.ReturnBullet(this.gameObject);
        }
        else
        {
            Destroy(gameObject); // Wyjście awaryjne
        }
    }
}