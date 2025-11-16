using UnityEngine;

public class PenetrableMaterial : MonoBehaviour
{
    [Header("Ustawienia Penetracji")]
    [Tooltip("Ile 'mocy' pocisku jest potrzebne, by przebiæ ten materia³. (np. 10)")]
    public float stoppingPower = 10f;

    [Tooltip("Jak bardzo materia³ spowalnia pocisk (0.1 = 10% spowolnienia, 0.9 = 90%)")]
    [Range(0, 1)]
    public float dragOnPenetration = 0.3f; // 30% spowolnienia
}