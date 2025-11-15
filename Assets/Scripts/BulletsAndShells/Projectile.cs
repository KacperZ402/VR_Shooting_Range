using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;
    private BulletPoolManager bulletPool;

    public float maxLifetime = 10.0f;
    private bool isLaunched = false;

    // --- Dane specyficzne dla tego pocisku (ustawiane przy Launch) ---
    private float dragCoefficient;
    private float minRicochetAngle;
    private int maxRicochets;
    private int currentRicochets = 0;

    private PhysicsMaterial projectileMaterial;

    // 🔹 🔹 🔹 NOWA ZMIENNA DO STABILIZACJI 🔹 🔹 🔹
    private Vector3 lastKnownVelocity;

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
        lastKnownVelocity = Vector3.zero; // Reset
    }

    void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Launch(Vector3 initialVelocity, float drag, float ammoRicochetAngle, int ammoMaxRicochets, float ammoBounciness, float ammoFriction)
    {
        rb.linearVelocity = initialVelocity;
        lastKnownVelocity = initialVelocity; // 🔹 Zapisz prędkość początkową

        this.dragCoefficient = drag;
        this.minRicochetAngle = ammoRicochetAngle;
        this.maxRicochets = ammoMaxRicochets;
        this.currentRicochets = 0;

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

        transform.rotation = Quaternion.LookRotation(rb.linearVelocity);

        Vector3 dragForce = -rb.linearVelocity.normalized * rb.linearVelocity.sqrMagnitude * this.dragCoefficient;
        rb.AddForce(dragForce, ForceMode.Force);

        if (showDebugTrajectory)
        {
            CreateDebugMarker(transform.position);
        }

        // 🔹 🔹 🔹 ZAPISUJEMY "CZYSTĄ" PRĘDKOŚĆ 🔹 🔹 🔹
        // Zapisujemy prędkość na końcu klatki fizyki, ZANIM dojdzie do kolizji
        lastKnownVelocity = rb.linearVelocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isLaunched) return;

        ContactPoint contact = collision.GetContact(0);

        // 🔹 🔹 🔹 UŻYWAMY "CZYSTEJ" PRĘDKOŚCI 🔹 🔹 🔹
        // Nie czytamy 'rb.linearVelocity', które jest niestabilne podczas kolizji
        Vector3 velocityBeforeImpact = lastKnownVelocity;

        if (showDebugImpact)
        {
            CreateDebugMarker(contact.point);
        }

        // --- 🔹 LOGIKA KĄTA (Z MINUSEM - 0-90°) 🔹 ---
        // Używamy minusa, bo teraz prędkość jest stabilna.
        // 0° = czołowo, 90° = płasko
        float impactAngle = Vector3.Angle(velocityBeforeImpact.normalized, -contact.normal);

        // 2. Sprawdź, czy kwalifikuje się do rykoszetu
        // Twój warunek 'if (impactAngle > 65)' jest poprawny
        bool isRicochet = impactAngle > minRicochetAngle && currentRicochets < maxRicochets;

        if (isRicochet)
        {
            // JEST RYKOSZET
            currentRicochets++;
            Debug.Log($"[Projectile] RYKOSZET! ({currentRicochets}/{maxRicochets}), Kąt: {impactAngle:F1}°");

            // LOGIKA "WYGŁADZANIA"
            if (ricochetRandomness > 0)
            {
                Vector3 velocityAfterBounce = rb.linearVelocity; // Tu już czytamy nową prędkość
                Vector3 randomKick = Random.insideUnitSphere * velocityAfterBounce.magnitude * ricochetRandomness;
                rb.AddForce(randomKick, ForceMode.VelocityChange);
            }

            return; // Pozwól lecieć dalej
        }

        // --- BRAK RYKOSZETU (Trafienie bezpośrednie) ---
        Debug.Log($"[Projectile] Trafiono: {collision.gameObject.name} (Kąt: {impactAngle:F1}° - Stop)");

        ReturnToPool();
    }

    private void CreateDebugMarker(Vector3 position)
    {
        GameObject debugMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugMarker.transform.position = position;
        debugMarker.transform.localScale = Vector3.one * debugImpactSize;
        Collider sphereCollider = debugMarker.GetComponent<Collider>();
        if (sphereCollider != null) sphereCollider.enabled = false;
        Destroy(debugMarker, debugImpactLifetime);
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