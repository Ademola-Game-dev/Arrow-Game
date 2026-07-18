using UnityEngine;

/// <summary>
/// Simple stopwatch-style timer that tracks elapsed play time.
/// Call <see cref="StartCounter"/> to begin timing (resets to zero) and
/// <see cref="StopCounter"/> to pause it; read the current value via
/// <see cref="GetCounterValue"/>.
/// </summary>
public class Timer : MonoBehaviour {

    /// <summary>Global access point for the active Timer instance.</summary>
    public static Timer Instance { get; private set; }

    private float elapsedTime = 0f;
    private bool counterRunning = false;

    private void Awake() {
        Instance = this;
    }

    private void Update() {
        if (counterRunning)
            elapsedTime += Time.deltaTime;
    }

    /// <summary>Resets elapsed time to zero and starts counting.</summary>
    public void StartCounter() {
        elapsedTime = 0f;
        counterRunning = true;
    }

    /// <summary>Pauses counting without resetting the elapsed time.</summary>
    public void StopCounter() {
        counterRunning = false;
    }

    /// <summary>Returns the current elapsed time in seconds.</summary>
    public float GetCounterValue() {
        return elapsedTime;
    }
}