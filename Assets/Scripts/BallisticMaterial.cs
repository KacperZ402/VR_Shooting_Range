using UnityEngine;

[CreateAssetMenu(fileName = "NewBallisticMaterial", menuName = "Balistyka/Nowy Materia³ Balistyczny")]
public class BallisticMaterial : ScriptableObject
{
    [Header("W³aœciwoœci Rykoszetu")]
    [Tooltip("Jak bardzo ten materia³ sprzyja rykoszetom. (B³oto = 0.0, Drewno = 0.5, Stal = 2.0)")]
    public float ricochetFactor = 1.0f;

    [Header("Efekty Trafienia")]
    [Tooltip("Prefab efektu cz¹steczkowego (iskry, drzazgi), który pojawi siê w miejscu trafienia.")]
    public GameObject impactEffectPrefab;
}