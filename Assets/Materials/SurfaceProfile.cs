using UnityEngine;

[CreateAssetMenu(fileName = "NewSurfaceProfile", menuName = "Shooting/Surface Profile")]
public class SurfaceProfile : ScriptableObject
{
    [Header("Wizualne")]
    public GameObject holePrefab;
    public GameObject particlePrefab;

    [Header("Audio")]
    [Tooltip("Dodaj kilka wariantów tego samego uderzenia, system wylosuje jeden.")]
    public AudioClip[] impactSounds;

    [Range(0f, 1f)]
    public float volume = 1f;
}