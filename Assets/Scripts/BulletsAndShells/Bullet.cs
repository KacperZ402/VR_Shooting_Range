using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Informacje Główne")]
    public string caliber = "5.56x45";
    public GameObject casingPrefab;

    [Header("Parametry Balistyczne (Dane Fabryczne)")]
    public float muzzleEnergy = 1750f;
    public float mass = 0.004f;
    public float dragCoefficient = 0.0005f;

    [Header("Typ Amunicji")]
    public int projectileCount = 1;

    // 🔹 🔹 🔹 SEKCJA DLA RYKOSZETÓW 🔹 🔹 🔹
    [Header("Balistyka Terminalna")]
    public float penetrationPower = 20f;
    [Tooltip("Minimalny kąt uderzenia (mierzony od normalnej), aby doszło do rykoszetu. 0=czołowo, 90=ślizg. Dobre wartości to 60-75.")]
    [Range(0, 90)]
    public float ricochetAngle = 70f;

    [Tooltip("Maksymalna liczba rykoszetów, zanim pocisk się zatrzyma.")]
    public int maxRicochets = 1;

    [Tooltip("Dynamiczna 'sprężystość' pocisku (0-1). Jak dużo energii odzyska przy odbiciu. Niskie wartości (0.1-0.3) są realistyczne.")]
    [Range(0, 1)]
    public float ricochetBounciness = 0.2f;

    [Tooltip("Dynamiczne 'tarcie' pocisku (0-1). Jak bardzo zwolni przy ślizgu.")]
    [Range(0, 1)]
    public float ricochetFriction = 0.3f;

    [Header("Opcjonalne: Ładowanie magazynka")]
    public int amount = 1;

}