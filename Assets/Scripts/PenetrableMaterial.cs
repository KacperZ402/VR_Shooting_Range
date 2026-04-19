using UnityEngine;

public class MaterialSurface : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject bulletHolePrefab; // Zwyk³a dziura
    public GameObject hitParticles;     // Iskry/Kurz

    [Header("Audio")]
    public AudioClip[] impactSounds;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("physics")]
    public float penetrationResistance = 5f;
    [Range(0f, 1f)] public float dragOnPenetration = 0.2f;
}