using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponAudio : MonoBehaviour
{
    [Header("Mechanika Broni")]
    public AudioClip fireSound;
    public AudioClip dryFireSound;
    public AudioClip magazineInSound;
    public AudioClip magazineOutSound;
    public AudioClip boltPullSound;
    public AudioClip boltReleaseSound;
    public AudioClip fireSelectorSound;

    [Header("Interakcja")]
    public AudioClip grabSound; // 🔹 NOWE: Dźwięk chwycenia broni

    private AudioSource _source;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
    }

    // --- METODY ---

    public void PlayFire()
    {
        if (fireSound == null) return;
        _source.pitch = Random.Range(0.95f, 1.05f);
        _source.PlayOneShot(fireSound);
    }

    public void PlayGrab() => PlaySound(grabSound); // 🔹 NOWE

    public void PlayDryFire() => PlaySound(dryFireSound);
    public void PlayMagIn() => PlaySound(magazineInSound);
    public void PlayMagOut() => PlaySound(magazineOutSound);
    public void PlayBoltPull() => PlaySound(boltPullSound);
    public void PlayBoltRelease() => PlaySound(boltReleaseSound);
    public void PlaySelector() => PlaySound(fireSelectorSound);

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        _source.pitch = 1.0f;
        _source.PlayOneShot(clip);
    }
}