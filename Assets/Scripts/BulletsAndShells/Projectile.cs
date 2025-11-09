using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Bullet))]
public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private Bullet bulletData;

    [HideInInspector]
    public string poolKey; // Klucz do puli (ustawiany przez PoolManager)

    [Tooltip("Czas życia pocisku w sekundach (zabezpieczenie)")]
    public float maxLifetime = 10.0f;

    private bool isLaunched = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bulletData = GetComponent<Bullet>();

        if (bulletData == null)
        {
            Debug.LogError("Komponent Projectile nie może znaleźć komponentu Bullet!");
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

        // 🔹 POPRAWKA: Używamy linearVelocity
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Wystrzeliwuje pocisk. Wywoływane przez WeaponController.
    /// </summary>
    public void Launch(Vector3 initialVelocity)
    {
        // 🔹 POPRAWKA: Używamy linearVelocity
        rb.linearVelocity = initialVelocity;
        isLaunched = true;
    }

    void FixedUpdate()
    {
        if (!isLaunched) return;

        // 🔹 POPRAWKA: Używamy linearVelocity
        if (rb.linearVelocity != Vector3.zero)
        {
            // 🔹 POPRAWKA: Używamy linearVelocity
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }

        // 🔹 POPRAWKA: Używamy linearVelocity
        // Używamy linearVelocity.normalized i linearVelocity.sqrMagnitude
        Vector3 dragForce = -rb.linearVelocity.normalized * rb.linearVelocity.sqrMagnitude * bulletData.dragCoefficient;

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
        // Sprawdź, czy Instance jeszcze istnieje (np. przy zamykaniu sceny)
        if (BulletPoolManager.Instance != null)
        {
            BulletPoolManager.Instance.ReturnBullet(this.gameObject, poolKey);
        }
    }
}