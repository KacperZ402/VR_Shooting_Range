using UnityEngine;

public class MaterialSurface : MonoBehaviour
{
    [Header("Wizualia")]
    public GameObject bulletHolePrefab; // Zwyk³a dziura
    public GameObject hitParticles;     // Iskry/Kurz

    [Header("DŸwiêk")]
    public AudioClip[] impactSounds;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Fizyka")]
    public float penetrationResistance = 5f;
    [Range(0f, 1f)] public float dragOnPenetration = 0.2f;
}