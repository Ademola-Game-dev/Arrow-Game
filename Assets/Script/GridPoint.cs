using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridPoint : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
    public Vector2 LocalPosition;
    public UILineRenderer OccupiedBySnake;   // which snake owns this point
    public int SnakePointIndex;   // index in that snake's path

    private Image _image;

    void Awake() {
        _image = gameObject.AddComponent<Image>();
        _image.color = new Color(1, 1, 1, 0.15f); // dim by default
        _image.raycastTarget = true;
    }

    public void SetOccupied(UILineRenderer snake, int index) {
        OccupiedBySnake = snake;
        SnakePointIndex = index;
        _image.color = new Color(1f, 1f, 0f, 0.25f); // yellow tint = occupied
    }

    public void SetFree() {
        OccupiedBySnake = null;
        SnakePointIndex = -1;
        _image.color = new Color(1, 1, 1, 0.15f);
    }

    // ── Pointer Events ────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData) {
        if (OccupiedBySnake != null) {
            Debug.Log($"[GridPoint] Clicked occupied point → Snake: {OccupiedBySnake.name} " +
                      $"index: {SnakePointIndex} color: {OccupiedBySnake.color}");

            // Forward click to the snake
            OccupiedBySnake.OnSnakeClicked?.Invoke(OccupiedBySnake);
        }
        else {
            Debug.Log($"[GridPoint] Clicked free point at {LocalPosition}");
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        _image.color = OccupiedBySnake != null
            ? new Color(1f, 1f, 0f, 0.5f)   // brighter yellow on hover
            : new Color(1f, 1f, 1f, 0.35f); // brighter white on hover
    }

    public void OnPointerExit(PointerEventData eventData) {
        _image.color = OccupiedBySnake != null
            ? new Color(1f, 1f, 0f, 0.25f)
            : new Color(1f, 1f, 1f, 0.15f);
    }
}