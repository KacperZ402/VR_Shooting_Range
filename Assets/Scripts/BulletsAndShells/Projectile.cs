using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;
    private BulletPoolManager bulletPool;

    public float maxLifetime = 10.0f;
    private bool isLaunched = false;
    private Vector3 lastKnownVelocity;

    // --- Dane specyficzne dla tego pocisku ---
    private float dragCoefficient;
    private float minRicochetAngle;
    private int maxRicochets;
    private int currentRicochets = 0;
    private float currentPenetrationPower;

    private PhysicsMaterial projectileMaterial;

    [Header("Debug")]
    public bool showDebugImpact = false;
    public bool showDebugTrajectory = false;
    public float debugImpactLifetime = 5f;
    public float debugImpactSize = 0.05f;

    [Tooltip("Jak bardzo 'nierówne' mają być rykoszety. 0=idealnie, 0.1=10% chaosu.")]
    [Range(0, 0.5f)]
    public float ricochetRandomness = 0.1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        bulletPool = BulletPoolManager.Instance;

        if (bulletPool == null) Debug.LogError("Projectile nie może znaleźć BulletPoolManager!");

        // Upewnij się, że nie jest kinematyczny!
        rb.isKinematic = false;

        if (col.material != null)
        {
            projectileMaterial = new PhysicsMaterial(col.material.name + "_Instance");
            projectileMaterial.bounciness = col.material.bounciness;
            projectileMaterial.dynamicFriction = col.material.dynamicFriction;
            projectileMaterial.staticFriction = col.material.staticFriction;
            projectileMaterial.bounceCombine = col.material.bounceCombine;
            projectileMaterial.frictionCombine = col.material.frictionCombine;
            col.material = projectileMaterial;
        }
        else
        {
            Debug.LogError("Pocisk (Prefab) nie ma 'PhysicsMaterial' na swoim Colliderze!", this);
        }
    }

    void OnEnable()
    {
        Invoke(nameof(ReturnToPool), maxLifetime);
        isLaunched = false;
        currentRicochets = 0;
        lastKnownVelocity = Vector3.zero;
        currentPenetrationPower = 0;
    }

    void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
        StopAllCoroutines();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Launch(Vector3 initialVelocity, float drag, float ammoRicochetAngle, int ammoMaxRicochets, float ammoBounciness, float ammoFriction, float ammoPenetrationPower)
    {
        rb.linearVelocity = initialVelocity;
        lastKnownVelocity = initialVelocity;

        this.dragCoefficient = drag;
        this.minRicochetAngle = ammoRicochetAngle;
        this.maxRicochets = ammoMaxRicochets;
        this.currentRicochets = 0;
        this.currentPenetrationPower = ammoPenetrationPower;

        if (projectileMaterial != null)
        {
            projectileMaterial.bounciness = ammoBounciness;
            projectileMaterial.dynamicFriction = ammoFriction;
            projectileMaterial.staticFriction = ammoFriction;
        }
        isLaunched = true;
    }

    void FixedUpdate()
    {
        if (!isLaunched || rb.linearVelocity == Vector3.zero) return;

        // Używamy AddForce, bo Rigidbody znowu jest normalne
        Vector3 dragForce = -rb.linearVelocity.normalized * rb.linearVelocity.sqrMagnitude * this.dragCoefficient;
        rb.AddForce(dragForce, ForceMode.Force);

        if (showDebugTrajectory)
        {
            CreateDebugMarker(transform.position);
        }

        lastKnownVelocity = rb.linearVelocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isLaunched) return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 velocityBeforeImpact = lastKnownVelocity;

        if (showDebugImpact)
        {
            CreateDebugMarker(contact.point);
        }

        // --- 1. SPRAWDŹ RYKOSZET ---
        float impactAngle = Vector3.Angle(velocityBeforeImpact.normalized, -contact.normal);
        bool isRicochet = impactAngle > minRicochetAngle && currentRicochets < maxRicochets;

        if (isRicochet)
        {
            // ... (Logika rykoszetu, 'wygładzanie' itp.) ...
            Debug.Log($"[Projectile] RYKOSZET!");
            currentPenetrationPower *= 0.5f;
            return;
        }

        // --- 2. SPRAWDŹ PENETRACJĘ ---
        PenetrableMaterial surface = collision.gameObject.GetComponent<PenetrableMaterial>();

        if (surface != null)
        {
            if (currentPenetrationPower >= surface.stoppingPower)
            {
                Debug.Log($"[Projectile] PENETRACJA! ({collision.gameObject.name})");
                currentPenetrationPower -= surface.stoppingPower;

                // 🔹 🔹 🔹 KLUCZOWA ZMIANA JEST TUTAJ 🔹 🔹 🔹

                // 1. Oblicz nową prędkość
                Vector3 penetrationVelocity = velocityBeforeImpact * (1.0f - surface.dragOnPenetration);

                // 2. Uruchom Coroutine, która zrobi resztę
                StartCoroutine(PerformPenetration(collision.collider, penetrationVelocity));

                return; // Pozwól pociskowi lecieć dalej (w Coroutine)
            }
        }

        // --- 3. ZATRZYMANIE ---
        Debug.Log($"[Projectile] Trafiono: {collision.gameObject.name} (Stop)");
        ReturnToPool();
    }

    // 🔹 🔹 🔹 NOWA, POPRAWIONA COROUTINE 🔹 🔹 🔹
    private IEnumerator PerformPenetration(Collider wallCollider, Vector3 penetrationVelocity)
    {
        // 1. Wyłącz kolizję, aby pocisk nie "utknął"
        Physics.IgnoreCollision(col, wallCollider, true);

        // 2. Czekaj JEDNĄ klatkę fizyczną. 
        // W tym czasie fizyka zdążyła zatrzymać pocisk, ale my to zaraz naprawimy.
        yield return new WaitForFixedUpdate();

        // 3. TERAZ, w nowej klatce, siłą nadpisujemy prędkość.
        rb.linearVelocity = penetrationVelocity;

        // (Włączymy kolizję z powrotem trochę później, by na pewno opuścił obiekt)
        yield return new WaitForSeconds(0.1f);

        if (this.gameObject.activeInHierarchy && wallCollider != null)
        {
            Physics.IgnoreCollision(col, wallCollider, false);
        }
    }

    private void CreateDebugMarker(Vector3 position)
    {
        // ... (bez zmian) ...
        GameObject debugMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugMarker.transform.position = position;
        debugMarker.transform.localScale = Vector3.one * debugImpactSize;
        Collider sphereCollider = debugMarker.GetComponent<Collider>();
        if (sphereCollider != null) sphereCollider.enabled = false;
        Destroy(debugMarker, debugImpactLifetime);
    }

    void ReturnToPool()
    {
        // ... (bez zmian) ...
        isLaunched = false;
        StopAllCoroutines();
        if (bulletPool != null)
        {
            bulletPool.ReturnBullet(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}