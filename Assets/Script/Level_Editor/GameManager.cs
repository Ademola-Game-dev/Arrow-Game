using System;
using UnityEngine;

namespace LevelEditor {

    /// <summary>Which mode the scene is currently running in.</summary>
    public enum GameMode {
        /// <summary>Normal gameplay: load a level and start the timer.</summary>
        PlayMode,
        /// <summary>Level Editor: load an existing level for editing, or start blank.</summary>
        LevelEditorMode
    }

    /// <summary>
    /// Entry point for the Level Editor scene. Depending on <see cref="CurrentGameMode"/>,
    /// either starts a normal timed play session or loads a level (or a blank board)
    /// for editing and notifies listeners via <see cref="OnLevelLoadedWithCustomLevel"/>.
    /// </summary>
    public class GameManager : MonoBehaviour {

        /// <summary>Global access point for the active GameManager instance.</summary>
        public static GameManager Instance { get; private set; }

        /// <summary>
        /// Raised in Level Editor Mode once startup completes. The bool indicates
        /// whether a predefined level was loaded (true) or the editor started blank (false).
        /// </summary>
        public event Action<bool> OnLevelLoadedWithCustomLevel;

        [SerializeField] private GameMode currentGameMode = GameMode.PlayMode;

        [Header("Level Data")]
        [SerializeField] private SnakeLevelData snakeLevelData;

        /// <summary>The mode this scene is currently running in.</summary>
        public GameMode CurrentGameMode => currentGameMode;

        private void Awake() {
            Instance = this;
        }

        private void Start() {
            if (currentGameMode == GameMode.PlayMode) {
                InitializePlayMode();
            }
            else if (currentGameMode == GameMode.LevelEditorMode) {
                InitializeLevelEditorMode();
            }
        }

        /// <summary>Loads the assigned level and starts the timer; subscribes to the win condition.</summary>
        private void InitializePlayMode() {
            if (snakeLevelData != null) {
                SnakeCreator.Instance.LoadLevel(snakeLevelData);
                Timer.Instance.StartCounter();
            }
            else {
                Debug.LogError("SnakeLevelData is not assigned in the GameManager.");
            }

            SnakeCreator.Instance.OnAllSnakesRemoved += SnakeCreator_Instance_OnAllSnakesRemoved;
        }

        /// <summary>Loads the assigned level for editing, or starts with a blank board if none is assigned.</summary>
        private void InitializeLevelEditorMode() {
            if (snakeLevelData != null) {
                SnakeCreator.Instance.LoadLevel(snakeLevelData);
                OnLevelLoadedWithCustomLevel?.Invoke(true);
            }
            else {
                Debug.Log("Starting Level Editor Mode without a predefined level. You can create a new level.");
                OnLevelLoadedWithCustomLevel?.Invoke(false);
            }
        }

        /// <summary>Called when the level is completed (Play Mode only). Stops the timer.</summary>
        private void SnakeCreator_Instance_OnAllSnakesRemoved() {
            Timer.Instance.StopCounter();
        }

        private void OnDestroy() {
            if (SnakeCreator.Instance != null)
                SnakeCreator.Instance.OnAllSnakesRemoved -= SnakeCreator_Instance_OnAllSnakesRemoved;
        }
    }
}