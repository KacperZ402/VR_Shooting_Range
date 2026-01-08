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

    [Header("Visual Effects")]
    public GameObject bulletHolePrefab;
    public GameObject impactParticlePrefab;
    public float bulletHoleLifetime = 30f;

    [Header("Visual Effects - Korekta")]
    [Tooltip("Ustaw to raz, aby dziura patrzyła przodem. Zazwyczaj dla Quada to (0, 180, 0) lub (0,0,0).")]
    public Vector3 holeRotationOffset = Vector3.zero;

    [Header("Debug")]
    public bool showDebugImpact = false;
    public bool showDebugTrajectory = false;
    public float debugImpactLifetime = 5f;
    public float debugImpactSize = 0.05f;

    [Range(0, 0.5f)]
    public float ricochetRandomness = 0.1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        bulletPool = BulletPoolManager.Instance;

        if (bulletPool == null) Debug.LogError("Projectile nie może znaleźć BulletPoolManager!");

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

        // --- 1. RYKOSZET ---
        float impactAngle = Vector3.Angle(velocityBeforeImpact.normalized, -contact.normal);
        bool isRicochet = impactAngle > minRicochetAngle && currentRicochets < maxRicochets;

        if (isRicochet)
        {
            CreateImpactVisuals(contact, spawnHole: false, spawnParticles: true);
            currentPenetrationPower *= 0.5f;
            currentRicochets++;
            return;
        }

        PenetrableMaterial surface = collision.gameObject.GetComponent<PenetrableMaterial>();

        // --- 2. PENETRACJA ---
        if (surface != null)
        {
            if (currentPenetrationPower >= surface.stoppingPower)
            {
                CreateImpactVisuals(contact, spawnHole: true, spawnParticles: true);
                currentPenetrationPower -= surface.stoppingPower;
                Vector3 penetrationVelocity = velocityBeforeImpact * (1.0f - surface.dragOnPenetration);
                StartCoroutine(PerformPenetration(collision.collider, penetrationVelocity));
                return;
            }
        }

        // --- 3. STOP ---
        CreateImpactVisuals(contact, spawnHole: true, spawnParticles: true);
        ReturnToPool();
    }

    private void CreateImpactVisuals(ContactPoint contact, bool spawnHole, bool spawnParticles)
    {
        // 1. Particle
        if (spawnParticles && impactParticlePrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(contact.normal);
            GameObject particle = Instantiate(impactParticlePrefab, contact.point, rot);
            Destroy(particle, 2f);
        }

        // 2. Dziura po kuli
        if (spawnHole && bulletHolePrefab != null)
        {
            // Podstawa: Oś Z wzdłuż normalnej (prostopadle od ściany)
            Quaternion lookRotation = Quaternion.LookRotation(contact.normal);

            // Stała korekta z inspektora
            Quaternion manualOffset = Quaternion.Euler(holeRotationOffset);

            Quaternion finalRotation = lookRotation * manualOffset;

            // 🔥 PRZYWRÓCONY OFFSET (0.01f) wzdłuż normalnej
            Vector3 position = contact.point + (contact.normal * 0.01f);

            GameObject hole = Instantiate(bulletHolePrefab, position, finalRotation);

            hole.transform.SetParent(contact.otherCollider.transform);
            Destroy(hole, bulletHoleLifetime);
        }
    }
    private IEnumerator PerformPenetration(Collider wallCollider, Vector3 penetrationVelocity)
    {
        Physics.IgnoreCollision(col, wallCollider, true);
        yield return new WaitForFixedUpdate();
        rb.linearVelocity = penetrationVelocity;
        yield return new WaitForSeconds(0.1f);
        if (this.gameObject.activeInHierarchy && wallCollider != null)
        {
            Physics.IgnoreCollision(col, wallCollider, false);
        }
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