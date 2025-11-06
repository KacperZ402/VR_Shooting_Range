using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [Header("Konfiguracja Puli")]
    public string poolTag = "Projectile"; // Upewnij si�, �e tag zgadza si� z ObjectPooler

    [Header("Parametry Balistyczne")]
    private Rigidbody rb;
    private float mass;
    private float dragCoefficient;

    [Header("Rykoszety")]
    [Tooltip("K�t (w stopniach od normalnej), poni�ej kt�rego nast�pi rykoszet.")]
    public float ricochetAngle = 20f; // K�t od powierzchni
    [Tooltip("Mno�nik pr�dko�ci po rykoszecie.")]
    public float ricochetSpeedLoss = 0.4f; // Traci 60% pr�dko�ci
    public int maxRicochets = 2;
    private int ricochetCount = 0;

    [Header("Czas �ycia")]
    public float maxLifetime = 5.0f; // Czas w sekundach, po kt�rym pocisk zniknie

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Metoda wywo�ywana przez bro�, aby "wystrzeli�" pocisk z puli.
    /// </summary>
    [System.Obsolete]
    public void Initialize(Vector3 initialVelocity, float bulletMass, float bulletDrag)
    {
        this.mass = bulletMass;
        this.dragCoefficient = bulletDrag;

        rb.mass = this.mass;
        rb.linearVelocity = initialVelocity; // Nadanie pr�dko�ci wylotowej

        ricochetCount = 0;

        // Rozpocznij odliczanie do autodestrukcji
        StopAllCoroutines();
        StartCoroutine(ReturnToPoolAfterTime(maxLifetime));
    }

    [System.Obsolete]
    void FixedUpdate()
    {
        // 1. Si�a Grawitacji (F = m * g)
        // Dzia�amy na Rigidbody, wi�c u�ywamy ForceMode.Acceleration (a = g)
        rb.AddForce(Physics.gravity, ForceMode.Acceleration);

        // 2. Si�a Oporu Powietrza (F_d = -v^2 * C_d)
        // Uproszczony wz�r F_d = -v.normalized * v.magnitude^2 * dragCoefficient
        Vector3 dragForce = -rb.linearVelocity.normalized * rb.linearVelocity.sqrMagnitude * dragCoefficient;

        // Dzia�amy na Rigidbody, wi�c u�ywamy ForceMode.Force (F)
        rb.AddForce(dragForce, ForceMode.Force);
    }

    void OnCollisionEnter(Collision collision)
    {
        // --- Logika Rykoszetu ---
        if (ricochetCount < maxRicochets)
        {
            ContactPoint contact = collision.GetContact(0);
            Vector3 normal = contact.normal;

            // K�t mi�dzy wektorem pr�dko�ci a normaln� powierzchni
            float impactAngle = Vector3.Angle(-rb.linearVelocity.normalized, normal);

            // K�t < 20 stopni (p�ytki) -> rykoszet
            if (impactAngle < ricochetAngle)
            {
                ricochetCount++;

                // Oblicz wektor odbicia i zastosuj utrat� pr�dko�ci
                Vector3 reflection = Vector3.Reflect(rb.linearVelocity, normal);
                rb.linearVelocity = reflection * (1.0f - ricochetSpeedLoss);

                // TODO: Odtw�rz d�wi�k rykoszetu i efekt cz�steczkowy w 'contact.point'
                return; // Nie niszcz pocisku, leci dalej
            }
        }

        // --- Logika Trafienia (Brak rykoszetu) ---

        // TODO: Tutaj logika obra�e� (np. collision.gameObject.GetComponent<Health>().TakeDamage())
        // TODO: Odtw�rz efekt trafienia (dziura po kuli) w 'contact.point'

        // Zwr�� pocisk do puli
        Deactivate();
    }

    private IEnumerator ReturnToPoolAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        Deactivate();
    }

    /// <summary>
    /// Deaktywuje pocisk i zwraca go do puli.
    /// </summary>
    private void Deactivate()
    {
        StopAllCoroutines();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        ObjectPooler.Instance.ReturnToPool(poolTag, this.gameObject);
    }
}