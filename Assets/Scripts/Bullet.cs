using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Informacje o naboju")]
    [Tooltip("Kaliber naboju, np. 9mm, 5.56, .45ACP")]
    public string caliber = "5.56x45";

    [Header("Opcjonalne: iloœæ amunicji w jednym obiekcie")]
    [Tooltip("Domyœlnie jeden nabój na obiekt")]
    public int amount = 1;
}