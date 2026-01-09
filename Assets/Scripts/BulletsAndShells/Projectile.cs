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

    private float dragCoefficient;
    private float minRicochetAngle;
    private int maxRicochets;
    private int currentRicochets = 0;
    private float currentPenetrationPower;

    private PhysicsMaterial projectileMaterial;

    [Header("Default Values")]
    public GameObject defaultHolePrefab;
    public GameObject defaultParticles;
    public AudioClip defaultSound;

    [Header("Settings")]
    public float bulletHoleLifetime = 30f;
    [Tooltip("Korekta obrotu dziury (np. 0, 180, 0)")]
    public Vector3 holeRotationOffset = Vector3.zero;

    [Header("Debug")]
    public bool showDebugImpact = false;
    public bool showDebugTrajectory = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        bulletPool = BulletPoolManager.Instance;
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
        if (showDebugTrajectory) CreateDebugMarker(transform.position);
        lastKnownVelocity = rb.linearVelocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isLaunched) return;
        ContactPoint contact = collision.GetContact(0);
        Vector3 incomingVelocity = lastKnownVelocity;

        if (showDebugImpact) CreateDebugMarker(contact.point);

        MaterialSurface matSurface = collision.gameObject.GetComponent<MaterialSurface>();

        // --- 1. RYKOSZET ---
        float impactAngle = Vector3.Angle(incomingVelocity.normalized, -contact.normal);
        bool isRicochet = impactAngle > minRicochetAngle && currentRicochets < maxRicochets;

        if (isRicochet)
        {
            // 🔥 spawnHole = false (Tylko iskry i dźwięk)
            SpawnVisuals(contact, matSurface, spawnHole: false);

            currentPenetrationPower *= 0.5f;
            currentRicochets++;
            return;
        }

        // --- 2. TRAFIENIE / PRZEBICIE ---
        // 🔥 spawnHole = true (Stawiamy dziurę)
        SpawnVisuals(contact, matSurface, spawnHole: true);

        // Fizyka przebicia
        float resistance = (matSurface != null) ? matSurface.penetrationResistance : 1000f;
        float drag = (matSurface != null) ? matSurface.dragOnPenetration : 0.5f;

        if (currentPenetrationPower >= resistance)
        {
            currentPenetrationPower -= resistance;
            Vector3 penetrationVelocity = incomingVelocity * (1.0f - drag);
            StartCoroutine(PerformPenetration(collision.collider, penetrationVelocity));
        }
        else
        {
            ReturnToPool();
        }
    }

    private void SpawnVisuals(ContactPoint contact, MaterialSurface mat, bool spawnHole)
    {
        // Dobieramy prefaby
        GameObject holePrefab = (mat != null && mat.bulletHolePrefab != null) ? mat.bulletHolePrefab : defaultHolePrefab;
        GameObject partPrefab = (mat != null && mat.hitParticles != null) ? mat.hitParticles : defaultParticles;

        AudioClip clipToPlay = defaultSound;
        float vol = 1f;

        if (mat != null && mat.impactSounds != null && mat.impactSounds.Length > 0)
        {
            clipToPlay = mat.impactSounds[Random.Range(0, mat.impactSounds.Length)];
            vol = mat.volume;
        }

        // 1. Particle
        if (partPrefab != null)
            Instantiate(partPrefab, contact.point, Quaternion.LookRotation(contact.normal));

        // 2. Audio
        if (clipToPlay != null)
            AudioSource.PlayClipAtPoint(clipToPlay, contact.point, vol);

        // 3. Dziura (Tylko jeśli spawnHole == true)
        if (spawnHole && holePrefab != null)
        {
            // 🔥 CZYSTA GEOMETRIA:
            // 1. Ustawiamy rotację tak, by oś Z patrzyła prostopadle od ściany.
            Quaternion lookRotation = Quaternion.LookRotation(contact.normal);

            // 2. Dodajemy Twoją stałą korektę (dla Quada zazwyczaj (0, 180, 0) lub (0,0,0)).
            Quaternion manualOffset = Quaternion.Euler(holeRotationOffset);

            // 3. Łączymy to w całość. USUNIĘTO randomZ.
            Quaternion finalRotation = lookRotation * manualOffset;

            // 4. Offset od ściany (0.01f = 1cm), żeby nie znikały w teksturze.
            Vector3 pos = contact.point + (contact.normal * 0.001f);

            GameObject hole = Instantiate(holePrefab, pos, finalRotation);
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
        debugMarker.transform.localScale = Vector3.one * 0.05f;
        Destroy(debugMarker, 5f);
    }

    void ReturnToPool()
    {
        isLaunched = false;
        StopAllCoroutines();
        if (bulletPool != null) bulletPool.ReturnBullet(this.gameObject);
        else Destroy(gameObject);
    }
}