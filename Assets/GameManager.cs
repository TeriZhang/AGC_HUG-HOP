using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject gameOverPanel;  // Panel that appears when game is finished
    [SerializeField] private TextMeshProUGUI finalTimeText;  // Shows player's final time
    [SerializeField] private TextMeshProUGUI[] leaderboardTexts;  // Array of 5 text elements for leaderboard
    [SerializeField] private Button restartButton;  // Restart button

    private float currentTime = 0f;
    private bool isTimerRunning = false;
    private DynamoDBManager dbManager;


    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Find the existing TimerText if not assigned
        if (timerText == null)
        {
            timerText = GameObject.Find("TimerText").GetComponent<TextMeshProUGUI>();
        }
    }

    void Start()
    {
        dbManager = FindObjectOfType<DynamoDBManager>();
        gameOverPanel.SetActive(false);  // Hide game over panel at start

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }


    }

    void Update()
    {
        if (isTimerRunning)
        {
            UpdateTimer();
        }

        // Reset level when R is pressed
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // Quit game when ESC is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    private void UpdateTimer()
    {
        currentTime += Time.deltaTime;
        DisplayTime();
    }

    private void DisplayTime()
    {
        float minutes = Mathf.FloorToInt(currentTime / 60);
        float seconds = Mathf.FloorToInt(currentTime % 60);
        float milliseconds = Mathf.FloorToInt((currentTime * 100) % 100);

        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    public void StartTimer()
    {
        if (!isTimerRunning)
        {
            isTimerRunning = true;
            currentTime = 0f;
        }
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public async void Finish()
    {
        StopTimer();

        // Save locally
        float finalTime = currentTime;
        PlayerPrefs.SetFloat("BestTime", finalTime);
        PlayerPrefs.Save();

        // Show game over panel
        gameOverPanel.SetActive(true);

        // Display final time
        string timeFormatted = FormatTime(finalTime);
        finalTimeText.text = $"Your Time: {timeFormatted}";

        // Save to DynamoDB
        if (dbManager != null)
        {
            string playerName = "Player" + UnityEngine.Random.Range(1, 1000);
            await dbManager.SaveScore(playerName, finalTime);

            // Fetch and display leaderboard
            var topScores = await dbManager.GetTopScores(5);  // Get top 5 scores
            DisplayLeaderboard(topScores);
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        float minutes = Mathf.FloorToInt(timeInSeconds / 60);
        float seconds = Mathf.FloorToInt(timeInSeconds % 60);
        float milliseconds = Mathf.FloorToInt((timeInSeconds * 100) % 100);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    private void DisplayLeaderboard(List<float> scores)
    {
        for (int i = 0; i < leaderboardTexts.Length; i++)
        {
            if (i < scores.Count)
            {
                string timeFormatted = FormatTime(scores[i]);
                leaderboardTexts[i].text = $"{i + 1}. {timeFormatted}";
            }
            else
            {
                leaderboardTexts[i].text = $"{i + 1}. ---";
            }
        }
    }

    public void RestartGame()
    {
        // Reload the current scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }


    public void QuitGame()
    {
        Application.Quit();
    }
}

