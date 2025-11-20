using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Informacje Główne")]
    public string caliber = "12 Gauge";
    public GameObject casingPrefab;

    [HideInInspector] public Vector3 defaultScale;

    [Header("Parametry Balistyczne")]
    public float muzzleEnergy = 2500f;
    public float mass = 0.03f;
    public float dragCoefficient = 0.01f;

    [Header("Typ Amunicji (Śrut)")]
    public int projectileCount = 9; // Ile kulek wylatuje (dla strzelby np. 8-12)

    [Tooltip("Rozrzut w stopniach. 0 = laser, 3-5 = strzelba, 1-2 = pistolet.")]
    [Range(0f, 15f)]
    public float spreadAngle = 5.0f; // 🔹 NOWE: Kąt stożka rozrzutu

    [Header("Rykoszety")]
    public float penetrationPower = 15f;
    public float ricochetAngle = 60f;
    public int maxRicochets = 1;
    public float ricochetBounciness = 0.2f;
    public float ricochetFriction = 0.3f;

    void Awake()
    {
        defaultScale = transform.localScale;
    }
}