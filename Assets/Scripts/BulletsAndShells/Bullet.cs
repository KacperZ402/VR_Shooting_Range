using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Informacje Główne")]
    public string caliber = "5.56x45";
    public GameObject casingPrefab;

    // ... (Twoje inne zmienne: muzzleEnergy, mass, projectileCount itp.) ...
    public float muzzleEnergy = 1750f;
    public float mass = 0.004f;
    public float dragCoefficient = 0.0005f;
    public int projectileCount = 1;
    public float penetrationPower = 20f;
    public float ricochetAngle = 70f;
    public int maxRicochets = 1;
    public float ricochetBounciness = 0.2f;
    public float ricochetFriction = 0.3f;

    // 🔹 NOWE: Zapamiętywanie skali
    [HideInInspector] public Vector3 defaultScale;

    void Awake()
    {
        // Zapisz skalę, jaką prefab ma ustawioną w edytorze
        defaultScale = transform.localScale;
    }
}