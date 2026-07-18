using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using LevelEditor;

/// <summary>
/// Wires up all Level Editor UI controls (snake drawing/deletion/swap, layout nudging,
/// color selection, help panel) to <see cref="LevelEditManager"/> and toggles the
/// editor UI on/off based on <see cref="GameManager.CurrentGameMode"/>. In Play Mode,
/// simply hides the editor panels and shows the countdown text instead.
/// </summary>
public class LevelEditUi : MonoBehaviour {

    [SerializeField] private Button finishSnakeBtn, cancelSnakeBtn, saveLevelBtn, deleteSnakeBtn, swapHeadBtn;
    [SerializeField] private Button nudgeLeftBtn, nudgeRightBtn, nudgeUpBtn, nudgeDownBtn;
    [SerializeField] private Button helpBtn, helpUiExitBtn;
    [SerializeField] private GameObject helpUiParent;
    [SerializeField] private Toggle canOverlapSnake, canGoDiagonal;
    [SerializeField] private GameObject levelEditPanel, levelEditPanel2, levelEditPanel3;
    [SerializeField] private TMP_Dropdown colorDropDown;
    [SerializeField] private Image colorPreviewImage;
    [SerializeField] private GameObject countDownText;

    // Colors Unity's built-in Color struct doesn't provide.
    private static readonly Color OrangeColor = new Color(1f, 0.5f, 0f);
    private static readonly Color PurpleColor = new Color(0.5f, 0f, 0.5f);
    private static readonly Color BrownColor = new Color(0.6f, 0.4f, 0.2f);
    private static readonly Color VioletColor = new Color(0.56f, 0f, 1f);

    private bool _isLevelEditorMode;

    // Named handlers so they can be individually unsubscribed in OnDestroy.
    private Action<bool> _onSnakeCreationStarted;
    private Action<SnakeRenderer> _onSnakeSelected;
    private Action _onNudgeLayoutPerformed;
    private Action _onSnakeCreated;
    private Action _onHeadSwapPerformed;
    private Action<bool> _onLevelLoadedWithCustomLevel;
    private Action _onAllSnakesRemoved;
    private UnityAction<bool> _onCanOverlapChanged;
    private UnityAction<bool> _onCanGoDiagonalChanged;

    private void Start() {
        if (GameManager.Instance == null) return;

        _isLevelEditorMode = GameManager.Instance.CurrentGameMode == GameMode.LevelEditorMode;

        if (_isLevelEditorMode) {
            InitializeLevelEditorUi();
        }
        else {
            InitializePlayModeUi();
        }

        helpUiParent.SetActive(false);
    }

    // ── Setup ─────────────────────────────────────────────────────────

    /// <summary>Shows editor panels, wires up all editor controls, and subscribes to LevelEditManager/GameManager events.</summary>
    private void InitializeLevelEditorUi() {
        levelEditPanel.SetActive(true);
        levelEditPanel2.SetActive(true);
        levelEditPanel3.SetActive(true);
        countDownText.SetActive(false);

        finishSnakeBtn.onClick.AddListener(HandleFinishSnakeClicked);
        cancelSnakeBtn.onClick.AddListener(HandleCancelSnakeClicked);
        saveLevelBtn.onClick.AddListener(HandleSaveLevelClicked);
        deleteSnakeBtn.onClick.AddListener(HandleDeleteSnakeClicked);
        swapHeadBtn.onClick.AddListener(HandleSwapHeadClicked);

        nudgeLeftBtn.onClick.AddListener(HandleNudgeLeftClicked);
        nudgeRightBtn.onClick.AddListener(HandleNudgeRightClicked);
        nudgeUpBtn.onClick.AddListener(HandleNudgeUpClicked);
        nudgeDownBtn.onClick.AddListener(HandleNudgeDownClicked);

        helpBtn.onClick.AddListener(HandleHelpClicked);
        helpUiExitBtn.onClick.AddListener(HandleHelpExitClicked);

        canOverlapSnake.isOn = LevelEditManager.Instance.CanOverlapSnake;
        _onCanOverlapChanged = value => LevelEditManager.Instance.CanOverlapSnake = value;
        canOverlapSnake.onValueChanged.AddListener(_onCanOverlapChanged);

        canGoDiagonal.isOn = LevelEditManager.Instance.CanGoDiagonal;
        _onCanGoDiagonalChanged = value => LevelEditManager.Instance.CanGoDiagonal = value;
        canGoDiagonal.onValueChanged.AddListener(_onCanGoDiagonalChanged);

        // Initial button states: nothing drawn or selected yet.
        finishSnakeBtn.interactable = false;
        cancelSnakeBtn.interactable = false;
        saveLevelBtn.interactable = false;
        deleteSnakeBtn.interactable = false;
        swapHeadBtn.interactable = false;

        nudgeLeftBtn.interactable = false;
        nudgeRightBtn.interactable = false;
        nudgeUpBtn.interactable = false;
        nudgeDownBtn.interactable = false;

        _onSnakeCreationStarted = HandleSnakeCreationStarted;
        LevelEditManager.Instance.OnSnakeCreationStarted += _onSnakeCreationStarted;

        _onSnakeSelected = HandleSnakeSelected;
        LevelEditManager.Instance.OnSnakeSelected += _onSnakeSelected;

        _onNudgeLayoutPerformed = () => saveLevelBtn.interactable = true;
        LevelEditManager.Instance.OnNudgeLayoutPerformed += _onNudgeLayoutPerformed;

        _onSnakeCreated = HandleSnakeCreated;
        LevelEditManager.Instance.OnSnakeCreated += _onSnakeCreated;

        _onHeadSwapPerformed = () => saveLevelBtn.interactable = true;
        LevelEditManager.Instance.OnHeadSwapPerformed += _onHeadSwapPerformed;

        _onLevelLoadedWithCustomLevel = HandleLevelLoadedWithCustomLevel;
        GameManager.Instance.OnLevelLoadedWithCustomLevel += _onLevelLoadedWithCustomLevel;
    }

    /// <summary>Hides editor panels, shows the countdown text, and listens for the win condition.</summary>
    private void InitializePlayModeUi() {
        levelEditPanel.SetActive(false);
        levelEditPanel2.SetActive(false);
        levelEditPanel3.SetActive(false);
        countDownText.SetActive(true);

        _onAllSnakesRemoved = () => Debug.Log("Show Win Panel");
        SnakeCreator.Instance.OnAllSnakesRemoved += _onAllSnakesRemoved;
    }

    // ── Button handlers ───────────────────────────────────────────────

    private void HandleFinishSnakeClicked() {
        LevelEditManager.Instance.FinishSnake();

        finishSnakeBtn.interactable = false;
        cancelSnakeBtn.interactable = false;
        saveLevelBtn.interactable = true;
    }

    private void HandleCancelSnakeClicked() {
        LevelEditManager.Instance.CancelSnake();

        finishSnakeBtn.interactable = false;
        cancelSnakeBtn.interactable = false;
        saveLevelBtn.interactable = SnakeCreator.Instance.SpawnedSnakes.Count > 0;
        deleteSnakeBtn.interactable = false;
    }

    private void HandleSaveLevelClicked() => LevelEditManager.Instance.SaveLevel();

    private void HandleDeleteSnakeClicked() {
        LevelEditManager.Instance.DeleteSelectedSnake();

        saveLevelBtn.interactable = SnakeCreator.Instance.SpawnedSnakes.Count > 0;
        deleteSnakeBtn.interactable = false;
        swapHeadBtn.interactable = false;
    }

    private void HandleSwapHeadClicked() => LevelEditManager.Instance.SwapHeadSnake();

    private void HandleNudgeLeftClicked() => LevelEditManager.Instance.NudgeLayout(Vector2Int.left);
    private void HandleNudgeRightClicked() => LevelEditManager.Instance.NudgeLayout(Vector2Int.right);
    private void HandleNudgeUpClicked() => LevelEditManager.Instance.NudgeLayout(Vector2Int.up);
    private void HandleNudgeDownClicked() => LevelEditManager.Instance.NudgeLayout(Vector2Int.down);

    private void HandleHelpClicked() {
        helpUiParent.SetActive(true);

        levelEditPanel.SetActive(false);
        levelEditPanel2.SetActive(false);
        levelEditPanel3.SetActive(false);
    }

    private void HandleHelpExitClicked() {
        helpUiParent.SetActive(false);

        levelEditPanel.SetActive(true);
        levelEditPanel2.SetActive(true);
        levelEditPanel3.SetActive(true);
    }

    // ── LevelEditManager / GameManager event handlers ───────────────────

    private void HandleSnakeCreationStarted(bool hasValidPointsForSnakes) {
        finishSnakeBtn.interactable = hasValidPointsForSnakes;
        cancelSnakeBtn.interactable = true;
        saveLevelBtn.interactable = false;
        deleteSnakeBtn.interactable = false;
    }

    private void HandleSnakeSelected(SnakeRenderer snake) {
        deleteSnakeBtn.interactable = true;
        swapHeadBtn.interactable = true;

        int index = (int)snake.SnakeColor;
        colorPreviewImage.color = GetColor(snake.SnakeColor);

        if (index >= 0 && index < colorDropDown.options.Count) {
            colorDropDown.SetValueWithoutNotify(index);
            colorDropDown.RefreshShownValue();
        }

        // Rebind the dropdown to the newly selected snake each time.
        colorDropDown.onValueChanged.RemoveAllListeners();
        colorDropDown.onValueChanged.AddListener(newIndex => HandleColorDropdownChanged(snake, newIndex));
    }

    private void HandleColorDropdownChanged(SnakeRenderer snake, int index) {
        if (snake != null) {
            snake.SetColor((SnakeColor)index);
            colorPreviewImage.color = GetColor(snake.SnakeColor);
            saveLevelBtn.interactable = true;
        }
        else {
            Debug.LogWarning("No snake selected to change color.");
            colorPreviewImage.color = Color.white;
        }
    }

    private void HandleSnakeCreated() {
        nudgeLeftBtn.interactable = true;
        nudgeRightBtn.interactable = true;
        nudgeUpBtn.interactable = true;
        nudgeDownBtn.interactable = true;
    }

    private void HandleLevelLoadedWithCustomLevel(bool hasCustomLevel) {
        nudgeLeftBtn.interactable = hasCustomLevel;
        nudgeRightBtn.interactable = hasCustomLevel;
        nudgeUpBtn.interactable = hasCustomLevel;
        nudgeDownBtn.interactable = hasCustomLevel;
    }

    // ── Utility ───────────────────────────────────────────────────────

    /// <summary>Maps a <see cref="SnakeColor"/> to its display <see cref="Color"/>.</summary>
    public Color GetColor(SnakeColor snakeColor) {
        return snakeColor switch {
            SnakeColor.Red => Color.red,
            SnakeColor.Green => Color.green,
            SnakeColor.Blue => Color.blue,
            SnakeColor.Yellow => Color.yellow,
            SnakeColor.Cyan => Color.cyan,
            SnakeColor.Magenta => Color.magenta,
            SnakeColor.Orange => OrangeColor,
            SnakeColor.Purple => PurpleColor,
            SnakeColor.Brown => BrownColor,
            SnakeColor.Violet => VioletColor,
            _ => Color.white
        };
    }

    private void OnDestroy() {
        finishSnakeBtn.onClick.RemoveListener(HandleFinishSnakeClicked);
        cancelSnakeBtn.onClick.RemoveListener(HandleCancelSnakeClicked);
        saveLevelBtn.onClick.RemoveListener(HandleSaveLevelClicked);
        deleteSnakeBtn.onClick.RemoveListener(HandleDeleteSnakeClicked);
        swapHeadBtn.onClick.RemoveListener(HandleSwapHeadClicked);

        nudgeLeftBtn.onClick.RemoveListener(HandleNudgeLeftClicked);
        nudgeRightBtn.onClick.RemoveListener(HandleNudgeRightClicked);
        nudgeUpBtn.onClick.RemoveListener(HandleNudgeUpClicked);
        nudgeDownBtn.onClick.RemoveListener(HandleNudgeDownClicked);

        helpBtn.onClick.RemoveListener(HandleHelpClicked);
        helpUiExitBtn.onClick.RemoveListener(HandleHelpExitClicked);

        if (_onCanOverlapChanged != null) canOverlapSnake.onValueChanged.RemoveListener(_onCanOverlapChanged);
        if (_onCanGoDiagonalChanged != null) canGoDiagonal.onValueChanged.RemoveListener(_onCanGoDiagonalChanged);

        colorDropDown.onValueChanged.RemoveAllListeners();

        if (LevelEditManager.Instance != null) {
            if (_onSnakeCreationStarted != null) LevelEditManager.Instance.OnSnakeCreationStarted -= _onSnakeCreationStarted;
            if (_onSnakeSelected != null) LevelEditManager.Instance.OnSnakeSelected -= _onSnakeSelected;
            if (_onNudgeLayoutPerformed != null) LevelEditManager.Instance.OnNudgeLayoutPerformed -= _onNudgeLayoutPerformed;
            if (_onSnakeCreated != null) LevelEditManager.Instance.OnSnakeCreated -= _onSnakeCreated;
            if (_onHeadSwapPerformed != null) LevelEditManager.Instance.OnHeadSwapPerformed -= _onHeadSwapPerformed;
        }

        if (GameManager.Instance != null && _onLevelLoadedWithCustomLevel != null)
            GameManager.Instance.OnLevelLoadedWithCustomLevel -= _onLevelLoadedWithCustomLevel;

        if (SnakeCreator.Instance != null && _onAllSnakesRemoved != null)
            SnakeCreator.Instance.OnAllSnakesRemoved -= _onAllSnakesRemoved;
    }
}