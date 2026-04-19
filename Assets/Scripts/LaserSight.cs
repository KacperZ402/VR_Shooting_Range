using UnityEngine;

public class LaserSight : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("Początek lasera (lufa lub emiter boczny)")]
    public Transform firePoint;
    public LineRenderer lineRenderer;

    [Header("Laser Dot")]
    public GameObject laserDotPrefab;
    private GameObject dotInstance;

    [Header("Settings")]
    public bool startActive = true; // Czy laser ma świecić od razu po starcie?
    public float maxDistance = 100f;
    public LayerMask hitMask;
    public float dotOffset = 0.01f;

    [Header("Audio (optional)")]
    public AudioClip clickSound;
    private AudioSource audioSource;

    void Start()
    {
        // 1. Setup LineRenderera
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        // 2. Setup Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && clickSound != null)
        {
            // Jak nie ma AudioSource a dałeś dźwięk, to dodajemy go automatycznie
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // Dźwięk 3D
        }

        // 3. Setup Kropki
        if (laserDotPrefab != null)
        {
            dotInstance = Instantiate(laserDotPrefab);
            dotInstance.SetActive(false);
            if (dotInstance.GetComponent<Collider>())
                Destroy(dotInstance.GetComponent<Collider>());
        }

        // 4. Ustawienie stanu początkowego
        this.enabled = startActive;
        // Wymuszamy wywołanie OnDisable jeśli startujemy wyłączeni, żeby ukryć linię
        if (!startActive) OnDisable();
    }

    // 🔥 TĘ FUNKCJĘ PODPINASZ POD PRZYCISK KONTROLERA
    public void Toggle()
    {
        // Odwracamy stan (jak włączony to wyłącz, jak wyłączony to włącz)
        this.enabled = !this.enabled;

        // Dźwięk kliknięcia
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    // Wykonuje się automatycznie, gdy włączysz skrypt (lub wywołasz Toggle na true)
    void OnEnable()
    {
        if (lineRenderer != null) lineRenderer.enabled = true;
    }

    // Wykonuje się automatycznie, gdy wyłączysz skrypt (lub wywołasz Toggle na false)
    void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;

        // Chowamy kropkę
        if (dotInstance != null) dotInstance.SetActive(false);
    }

    void Update()
    {
        if (firePoint == null || lineRenderer == null) return;

        lineRenderer.SetPosition(0, firePoint.position);

        Ray ray = new Ray(firePoint.position, firePoint.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, hitMask))
        {
            // Trafienie
            lineRenderer.SetPosition(1, hit.point);

            if (dotInstance != null)
            {
                if (!dotInstance.activeSelf) dotInstance.SetActive(true);
                dotInstance.transform.position = hit.point + (hit.normal * dotOffset);
                dotInstance.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
        }
        else
        {
            // Pudło
            lineRenderer.SetPosition(1, firePoint.position + (firePoint.forward * maxDistance));

            if (dotInstance != null && dotInstance.activeSelf)
                dotInstance.SetActive(false);
        }
    }
}