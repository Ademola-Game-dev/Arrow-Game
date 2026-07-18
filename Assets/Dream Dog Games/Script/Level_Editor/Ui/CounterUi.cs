using TMPro;
using UnityEngine;

/// <summary>
/// Displays the running elapsed time (MM:SS) from <see cref="Timer"/> in a UI label,
/// updating every frame.
/// </summary>
public class CounterUi : MonoBehaviour {

    [SerializeField] private TMP_Text counterText;

    private void Update() {
        if (counterText != null) {
            UpdateTotalTime();
        }
    }

    /// <summary>Formats the timer's elapsed value as MM:SS and displays it.</summary>
    private void UpdateTotalTime() {
        float t = Timer.Instance.GetCounterValue();
        int mins = (int)(t / 60);
        int secs = (int)(t % 60);
        counterText.text = $"{mins:00}:{secs:00}";
    }
}