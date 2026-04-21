using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "VR/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab;

    [Header("Magazine Settings")]
    public GameObject magazinePrefab;
    public int poolSize = 1; // Pula dla broni
    public int magsToSpawn = 5; // Ile magów ma się pojawić przy broni
}