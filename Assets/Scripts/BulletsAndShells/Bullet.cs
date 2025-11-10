using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Informacje Główne")]
    [Tooltip("Kaliber naboju, np. 9mm, 5.56, .45ACP. Używane do sprawdzania magazynka.")]
    public string caliber = "5.56x45";

    [Header("Parametry Balistyczne (Dane Fabryczne)")]
    [Tooltip("Bazowa energia wylotowa pocisku w Dżulach (J).")]
    public float muzzleEnergy = 1750f;

    [Tooltip("Masa samego pocisku (nie całego naboju) w kilogramach (kg).")]
    public float mass = 0.004f; // 4 gramy dla 5.56

    [Tooltip("Współczynnik oporu (Cd * A * rho / 2). " +
             "Większa wartość = większy opór. Dla pocisku pistoletowego ok. 0.002, dla karabinowego ok. 0.0005")]
    public float dragCoefficient = 0.0005f;

    [Header("Typ Amunicji")]
    [Tooltip("Ilość pocisków wystrzeliwanych na raz (1 dla kuli, >1 dla śrutu)")]
    public int projectileCount = 1;

    [Header("Opcjonalne: Ładowanie magazynka")]
    [Tooltip("Ilość amunicji w jednym obiekcie (do ładowania magazynka)")]
    public int amount = 1;

}