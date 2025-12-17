using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ShootingRangeManager : MonoBehaviour
{
    [Header("Konfiguracja")]
    public List<ShootingTarget> allTargets;
    public float roundTime = 30f;
    public int targetsToActivate = 5;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;

    private int currentScore = 0;
    private float timeRemaining = 0f;
    private bool isGameRunning = false;

    void Start()
    {
        // 1. Na samym początku tylko przygotowujemy tarcze (reset)
        // Gra się NIE zaczyna sama.
        foreach (var target in allTargets)
        {
            target.Setup(this);
            target.FoldDown();
        }
        UpdateUI();
    }

    void Update()
    {
        // Odliczanie działa tylko, gdy flaga isGameRunning jest true
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

    // 🔥 TO JEST EVENT, KTÓRY MUSISZ WYWOŁAĆ PRZYCISKIEM
    public void StartGame()
    {
        if (isGameRunning) return; // Zabezpieczenie przed podwójnym kliknięciem

        Debug.Log("Gra wystartowała!");
        currentScore = 0;
        timeRemaining = roundTime;
        isGameRunning = true;

        // Reset wszystkich tarcz
        foreach (var target in allTargets)
        {
            target.FoldDown();
        }

        // Podnieś losowe tarcze
        ActivateRandomTargets();
    }

    public void AddScore()
    {
        if (!isGameRunning) return;
        currentScore++;
        UpdateUI();

        // Opcjonalnie: Jak zbijesz jedną, kolejna wstaje od razu?
        // ActivateOneRandomTarget(); 
    }

    void ActivateRandomTargets()
    {
        List<ShootingTarget> available = new List<ShootingTarget>(allTargets);
        int count = Mathf.Min(targetsToActivate, available.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, available.Count);
            available[randomIndex].PopUp();
            available.RemoveAt(randomIndex);
        }
    }

    /* Opcjonalna metoda do podnoszenia pojedynczej tarczy po trafieniu
    void ActivateOneRandomTarget()
    {
        // Znajdź tarcze, które leżą
        List<ShootingTarget> sleepingTargets = new List<ShootingTarget>();
        foreach(var t in allTargets) { if(!t.gameObject.activeSelf) sleepingTargets.Add(t); } // Uproszczone

        // To wymagałoby sprawdzania stanu w ShootingTarget, 
        // ale w prostej wersji wystarczy, że po prostu gra się kończy po czasie.
    }
    */

    void EndGame()
    {
        isGameRunning = false;
        timeRemaining = 0;
        UpdateUI();

        // Koniec - kładziemy wszystko
        foreach (var target in allTargets)
        {
            target.FoldDown();
        }
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"WYNIK: {currentScore}";
        if (timeText) timeText.text = $"CZAS: {Mathf.Ceil(timeRemaining)}s";
    }
}