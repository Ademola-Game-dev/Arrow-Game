using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelEditUi : MonoBehaviour {
    [SerializeField] private Button finishSnakeBtn, cancelSnakeBtn, saveLevelBtn;
    [SerializeField] private GameObject levelEditPanel;
    [SerializeField] private TMP_Dropdown colorDropDown;
    [SerializeField] private Image colorPreviewImage;

    private void Start() {
        if (LevelEditManager.Instance != null) {
           if(LevelEditManager.Instance.IsInEditMode) {
                levelEditPanel.SetActive(true);

                finishSnakeBtn.onClick.AddListener(() =>
                {
                    LevelEditManager.Instance.FinishSnake();

                    finishSnakeBtn.interactable = false;
                    cancelSnakeBtn.interactable = false;
                    saveLevelBtn.interactable = true;
                });

                cancelSnakeBtn.onClick.AddListener(() => { 
                    LevelEditManager.Instance.CancelSnake();

                    finishSnakeBtn.interactable = false;
                    cancelSnakeBtn.interactable = false;
                    saveLevelBtn.interactable = SnakeCreator.Instance.SpawnedSnakes.Count > 0; // Only enable save if we have at least 1 snake, otherwise disable it
                });

                saveLevelBtn.onClick.AddListener(() => LevelEditManager.Instance.SaveLevel());

                finishSnakeBtn.interactable = false; // Initially disable finish button until we have at least 2 points
                cancelSnakeBtn.interactable = false; // Initially disable cancel button until we have at least 2 points
                saveLevelBtn.interactable = false; // Initially disable save button until we have at least 1 snake

                LevelEditManager.Instance.OnSnakeCreationStarted += (hasValidPointsForSnakes) => { 

                    finishSnakeBtn.interactable = hasValidPointsForSnakes; // Only allow finishing if we have at least 2 points to create a snake
                    cancelSnakeBtn.interactable = true; // Allow canceling as soon as we start creating a snake, even if we don't have 2 points yet, since the user might want to cancel before reaching 2 points
                    saveLevelBtn.interactable = false; // Save button should only be enabled when we have at least 1 snake, not during snake creation
                };

                LevelEditManager.Instance.OnSnakeSelected += (snake) =>
                {
                    int index = (int)snake.SnakeColor;
                    colorPreviewImage.color = GetColor(snake.SnakeColor);

                    if (index >= 0 && index < colorDropDown.options.Count) {
                        colorDropDown.SetValueWithoutNotify(index);
                        colorDropDown.RefreshShownValue();
                    }

                    colorDropDown.onValueChanged.RemoveAllListeners();

                    colorDropDown.onValueChanged.AddListener((index) =>
                    {
                        snake.SetColor((SnakeColor)index);
                        colorPreviewImage.color = GetColor(snake.SnakeColor);
                    });
                };
            }
           else {
                levelEditPanel.SetActive(false);
           }
        }

       
    }

    public Color GetColor(SnakeColor snakeColor) {
        return snakeColor switch {
            SnakeColor.Red => Color.red,
            SnakeColor.Green => Color.green,
            SnakeColor.Blue => Color.blue,
            SnakeColor.Yellow => Color.yellow,
            SnakeColor.Cyan => Color.cyan,
            SnakeColor.Magenta => Color.magenta,
            SnakeColor.Orange => Color.orange,
            SnakeColor.Purple => Color.purple,
            SnakeColor.Brown => Color.brown,
            SnakeColor.Violet => Color.violet,
            _ => Color.white 
        };
    }

    private void OnDestroy() {
        finishSnakeBtn.onClick.RemoveAllListeners();
        cancelSnakeBtn.onClick.RemoveAllListeners();
        saveLevelBtn.onClick.RemoveAllListeners();
    }
}
