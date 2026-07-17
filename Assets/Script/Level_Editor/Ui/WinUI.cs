using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Displays the win panel and total elapsed time when the level is completed,
/// and handles restarting the current scene.
/// </summary>
public class WinUI : MonoBehaviour {

    [SerializeField] private GameObject winUIPanel;
    [SerializeField] private Button restartGameButton;
    [SerializeField] private TMP_Text totalTimeText;

    private void Awake() {
        restartGameButton.onClick.AddListener(RestartGame);
        winUIPanel.SetActive(false);
    }

    private void Start() {
        SnakeCreator.Instance.OnAllSnakesRemoved += HandleAllSnakesRemoved;
    }

    /// <summary>Reloads the currently active scene.</summary>
    private void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Called when the level is completed (all snakes cleared). Shows the win panel.</summary>
    private void HandleAllSnakesRemoved() {
        winUIPanel.SetActive(true);
        UpdateTotalTime();
    }

    private void OnDestroy() {
        if (SnakeCreator.Instance != null)
            SnakeCreator.Instance.OnAllSnakesRemoved -= HandleAllSnakesRemoved;

        restartGameButton.onClick.RemoveListener(RestartGame);
    }

    /// <summary>Formats the timer's elapsed value as MM:SS and displays it.</summary>
    private void UpdateTotalTime() {
        float t = Timer.Instance.GetCounterValue();
        int mins = (int)(t / 60);
        int secs = (int)(t % 60);
        totalTimeText.text = $"TOTAL TIME: {mins:00}:{secs:00}";
    }
}