using UnityEngine;

/// <summary>
/// Umieœæ ten komponent na ka¿dym obiekcie, który ma reagowaæ na trafienia.
/// Przechowuje referencjê do materia³u balistycznego (.asset).
/// </summary>
public class BallisticSurface : MonoBehaviour
{
    [Header("Materia³ Balistyczny")]
    [Tooltip("Plik .asset materia³u, który definiuje w³aœciwoœci tej powierzchni.")]
    public BallisticMaterial material;
}