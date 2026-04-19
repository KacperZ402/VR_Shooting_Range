using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ShootingRangeManager : MonoBehaviour
{
    public static ShootingRangeManager Instance;

    [Header("Listy Celów")]
    public List<ShootingTarget> staticTargets; // Cele do Reflex (stojące)
    public List<ShootingTarget> movingTargets; // Cele do MovingChallenge (jeżdżące)

    [Header("Ustawienia Gry")]
    public float roundTime = 30f;
    [Tooltip("Opóźnienie między zestrzeleniem a pojawieniem się następnego.")]
    public float respawnDelay = 0.5f;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI finalResultText;

    // --- ZMIENNE STANU ---
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int shotsFired = 0; // Licznik strzałów
    private float timeRemaining = 0f;
    private bool isGameRunning = false;
    private bool isMovingMode = false;

    private ShootingTarget lastActiveTarget;
    private List<ShootingTarget> activeTargetList;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    void Start()
    {
        // 🔥🔥🔥 TO JEST TO, CZEGO BRAKOWAŁO:
        // Musimy powiedzieć celom, że to my jesteśmy Managerem.
        foreach (var t in staticTargets) if (t) t.Setup(this);
        foreach (var t in movingTargets) if (t) t.Setup(this);

        // Na starcie wyłączamy wszystko
        ResetAllTargets();
        UpdateUI();
        if (finalResultText) finalResultText.text = "";
    }

    void Update()
    {
        if (isGameRunning)
        {
            timeRemaining -= Time.deltaTime;
            UpdateUI();

            if (timeRemaining <= 0)
            {
                EndGame();
            }
        }
    }

    // =========================================================
    // PRZYCISKI STARTU
    // =========================================================

    public void StartReflexGame()
    {
        Debug.Log("[ShootingRange] Start: REFLEX");
        isMovingMode = false;
        activeTargetList = staticTargets;
        StartGameInternal();
    }

    public void StartMovingChallenge()
    {
        Debug.Log("[ShootingRange] Start: MOVING");
        isMovingMode = true;
        activeTargetList = movingTargets;
        StartGameInternal();
    }

    // =========================================================
    // LOGIKA
    // =========================================================

    private void StartGameInternal()
    {
        if (isGameRunning) return;

        currentScore = 0;
        shotsFired = 0;
        timeRemaining = roundTime;
        isGameRunning = true;
        lastActiveTarget = null;
        if (finalResultText) finalResultText.text = "";

        ResetAllTargets(); // Czyścimy pole przed startem

        UpdateUI();
        ActivateNextTarget();
    }
    public void EndGame()
    {
        isGameRunning = false;
        // ... (reset celów itp.)

        // OBLICZANIE CELNOŚCI
        float accuracy = 0f;
        if (shotsFired > 0)
        {
            // Rzutowanie na float jest kluczowe, inaczej int/int utnie wynik do 0 lub 1
            accuracy = ((float)currentScore / (float)shotsFired) * 100f;
        }

        // Zabezpieczenie na wypadek, gdybyś trafił więcej razy niż strzelił (np. rykoszet, jeden pocisk zbił dwa cele)
        if (accuracy > 100f) accuracy = 100f;

        string message = $"Hits: {currentScore} / {shotsFired}\n" +
                         $"Avg Time: {roundTime/currentScore}s\n" +
                         $"Accurcy: {accuracy:F1}%"; // F1 to jedno miejsce po przecinku

        Debug.Log(message);
        if (finalResultText) finalResultText.text = message;
    }

    public void RegisterHit(ShootingTarget hitTarget)
    {
        if (!isGameRunning) return;

        // Jeśli trafiliśmy cel ruchomy, zatrzymujemy go
        if (isMovingMode)
        {
            var mover = hitTarget.GetComponent<MovingTarget>();
            if (mover) mover.isMoving = false;
        }

        currentScore++;
        UpdateUI();

        StartCoroutine(WaitAndSpawnNext());
    }

    public void RegisterShot() 
    {
        if (!isGameRunning) return;
        shotsFired++;
    }

    private IEnumerator WaitAndSpawnNext()
    {
        if (respawnDelay > 0) yield return new WaitForSeconds(respawnDelay);
        else yield return null;

        if (isGameRunning)
        {
            ActivateNextTarget();
        }
    }

    private void ActivateNextTarget()
    {
        if (activeTargetList == null || activeTargetList.Count == 0) return;

        ShootingTarget newTarget;

        if (activeTargetList.Count == 1)
        {
            newTarget = activeTargetList[0];
        }
        else
        {
            int attempts = 0;
            do
            {
                int randomIndex = Random.Range(0, activeTargetList.Count);
                newTarget = activeTargetList[randomIndex];
                attempts++;
            }
            while (newTarget == lastActiveTarget && attempts < 10);
        }

        lastActiveTarget = newTarget;
        newTarget.PopUp();

        // Jeśli tryb ruchomy, włączamy ruch
        if (isMovingMode)
        {
            var mover = newTarget.GetComponent<MovingTarget>();
            if (mover) mover.isMoving = true;
        }
    }

    private void ResetAllTargets()
    {
        foreach (var t in staticTargets) if (t) t.FoldDown();

        foreach (var t in movingTargets)
        {
            if (t)
            {
                t.FoldDown();
                var mover = t.GetComponent<MovingTarget>();
                if (mover) mover.isMoving = false;
            }
        }
    }
    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"ZDJĘTO: {currentScore}";
        if (timeText) timeText.text = $"{Mathf.Ceil(timeRemaining)}s";
    }
}