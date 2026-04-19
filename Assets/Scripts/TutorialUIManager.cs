using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance;

    [Header("Komponenty UI")]
    public GameObject windowRoot;   // G³ówny panel (¿eby go chowaæ/pokazywaæ)
    public RawImage screenImage;    // Ekran, na którym wyœwietlamy
    public TextMeshProUGUI titleText;

    [Header("Komponenty Video")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        // Na starcie ukrywamy okno
        CloseTutorial();

        // Podpinamy automatycznie AudioSource jeœli zapomnia³eœ
        if (videoPlayer && audioSource)
            videoPlayer.SetTargetAudioSource(0, audioSource);
    }

    public void ShowTutorial(string weaponName, VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Próba odpalenia tutoriala bez klipu wideo!");
            return;
        }

        // 1. Ustawiamy treœæ
        titleText.text = $"Obs³uga: {weaponName}";
        videoPlayer.clip = clip;

        // 2. Pozycjonowanie (Opcjonalne - resetuje pozycjê przed gracza)
        // MoveToPlayerView(); 

        // 3. Pokazujemy okno
        windowRoot.SetActive(true);

        // 4. Start wideo
        videoPlayer.Play();
        if (audioSource) audioSource.Play();
    }

    public void CloseTutorial()
    {
        videoPlayer.Stop();
        if (audioSource) audioSource.Stop();
        windowRoot.SetActive(false);

        // Czyœcimy klip z pamiêci (wa¿ne dla RAM!)
        videoPlayer.clip = null;
    }

    // Funkcja pomocnicza do przenoszenia okna przed twarz gracza (VR)
    public void MoveToPlayerView()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // Ustawiamy okno 1.5 metra przed graczem, na wysokoœci oczu
            transform.position = mainCam.transform.position + (mainCam.transform.forward * 1.5f);

            // Obracamy okno w stronê gracza (tylko w osi Y, ¿eby nie by³o dziwnie pochylone)
            Vector3 lookPos = transform.position - mainCam.transform.position;
            lookPos.y = 0;
            if (lookPos != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookPos);
        }
    }
}