using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class UniversalAudioPlayer : MonoBehaviour
{
    [Header("Ustawienia G³ówne")]
    [Tooltip("Lista dŸwiêków. Skrypt wylosuje jeden przy ka¿dym odtworzeniu.")]
    public AudioClip[] audioClips;

    [Range(0f, 1f)]
    public float baseVolume = 1.0f;

    [Tooltip("Losowoœæ tonu (Pitch). Nadaje naturalnoœci.")]
    public float pitchRandomness = 0.1f;

    [Header("Logika Kolizji")]
    [Tooltip("Czy ten obiekt ma wydawaæ dŸwiêk przy uderzeniu?")]
    public bool playOnCollision = true;

    [Tooltip("Minimalna si³a uderzenia, ¿eby zagraæ dŸwiêk.")]
    public float minImpactForce = 0.5f;

    [Tooltip("Minimalny czas miêdzy uderzeniami (anty-spam).")]
    public float collisionCooldown = 0.1f;

    private AudioSource _source;
    private float _lastPlayTime;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        // W VR zawsze chcemy dŸwiêku przestrzennego 3D
        _source.spatialBlend = 1.0f;
    }

    // --- 1. METODA PUBLICZNA (Dla Eventów: Grab, UI, Animacje) ---

    /// <summary>
    /// Wywo³aj tê funkcjê z Unity Event (np. Select Entered w XR Grab Interactable).
    /// </summary>
    public void PlayAudio()
    {
        PlayInternal(baseVolume);
    }

    // --- 2. METODA FIZYCZNA (Automatyczna) ---

    void OnCollisionEnter(Collision collision)
    {
        // Checkbox: Jeœli wy³¹czone, wychodzimy
        if (!playOnCollision) return;

        // Cooldown: Jeœli uderza za czêsto (np. toczy siê), wychodzimy
        if (Time.time < _lastPlayTime + collisionCooldown) return;

        // Si³a: Jeœli uderzenie jest za s³abe, wychodzimy
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minImpactForce) return;

        // Obliczamy g³oœnoœæ dynamicznie (im mocniej, tym g³oœniej)
        // Mno¿ymy si³ê przez 0.2 (przyk³adowo), ¿eby nie by³o za g³oœno, i ucinamy do baseVolume
        float dynamicVolume = Mathf.Clamp(impactForce * 0.2f, 0.1f, baseVolume);

        PlayInternal(dynamicVolume);
    }

    // --- LOGIKA WEWNÊTRZNA ---

    private void PlayInternal(float volume)
    {
        if (audioClips == null || audioClips.Length == 0) return;

        // 1. Losujemy klip
        AudioClip clip = audioClips[Random.Range(0, audioClips.Length)];
        if (clip == null) return;

        // 2. Losujemy Pitch (1.0 +/- randomness)
        _source.pitch = 1.0f + Random.Range(-pitchRandomness, pitchRandomness);

        // 3. Gramy (OneShot pozwala na nak³adanie siê dŸwiêków)
        _source.PlayOneShot(clip, volume);

        _lastPlayTime = Time.time;
    }
}