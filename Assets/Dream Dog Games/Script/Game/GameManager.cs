using UnityEngine;

namespace Game {

    /// <summary>
    /// Central controller for the snake puzzle game. Owns the singleton reference
    /// used by other systems, loads the assigned level on start, and drives the
    /// timer based on snake-clear events from <see cref="SnakeCreator"/>.
    /// </summary>
    public class GameManager : MonoBehaviour {

        /// <summary>Global access point for the active GameManager instance.</summary>
        public static GameManager Instance { get; private set; }

        [Header("Level Data")]
        [Tooltip("Snake Level Data that defines the game level.")]
        [SerializeField] private SnakeLevelData snakeLevelData;

        private void Awake() {
            // Guard against duplicate instances (e.g. leftover manager from a reloaded scene).
            if (Instance != null && Instance != this) {
                Debug.LogWarning("Duplicate GameManager detected; destroying the new instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start() {
            if (snakeLevelData != null) {
                SnakeCreator.Instance.LoadLevel(snakeLevelData);
                Timer.Instance.StartCounter();
            }
            else {
                Debug.LogError("SnakeLevelData is not assigned in the GameManager.");
            }

            SnakeCreator.Instance.OnAllSnakesRemoved += SnakeCreator_Instance_OnAllSnakesRemoved;
        }

        /// <summary>
        /// Called when every snake on the board has been removed (level complete).
        /// Stops the level timer.
        /// </summary>
        private void SnakeCreator_Instance_OnAllSnakesRemoved() {
            Timer.Instance.StopCounter();
        }

        private void OnDestroy() {
            SnakeCreator.Instance.OnAllSnakesRemoved -= SnakeCreator_Instance_OnAllSnakesRemoved;
        }
    }
}