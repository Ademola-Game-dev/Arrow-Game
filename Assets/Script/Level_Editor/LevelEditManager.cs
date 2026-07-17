using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives all Level Editor interactions: drawing new snakes point-by-point,
/// selecting/deleting/swapping existing snakes, nudging the whole layout,
/// and saving the current board out to a <see cref="SnakeLevelData"/> asset.
/// </summary>
public class LevelEditManager : MonoBehaviour {

    /// <summary>Global access point for the active LevelEditManager instance.</summary>
    public static LevelEditManager Instance { get; private set; }

    /// <summary>
    /// Raised as the player starts drawing a new snake. The bool indicates whether
    /// at least two points have been placed (true) or only the first point (false).
    /// </summary>
    public event Action<bool> OnSnakeCreationStarted;

    /// <summary>Raised after a new snake has been successfully created via <see cref="FinishSnake"/>.</summary>
    public event Action OnSnakeCreated;

    /// <summary>Raised when an existing snake is clicked/selected.</summary>
    public event Action<SnakeRenderer> OnSnakeSelected;

    /// <summary>Raised after <see cref="NudgeLayout"/> successfully repositions all snakes.</summary>
    public event Action OnNudgeLayoutPerformed;

    /// <summary>Raised after <see cref="SwapHeadSnake"/> reverses the selected snake's direction.</summary>
    public event Action OnHeadSwapPerformed;

    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private SnakeCreator snakeCreator;

    [SerializeField]
    private bool canOverlapSnake;

    [SerializeField]
    private bool canGoDiagonal;


    public bool CanOverlapSnake {
        get => canOverlapSnake;
        set => canOverlapSnake = value;
    }

    public bool CanGoDiagonal {
        get => canGoDiagonal;
        set => canGoDiagonal = value;
    }

    /// <summary>Grid points placed so far for the snake currently being drawn.</summary>
    private List<GridPoint> currentSnakeGridPoints = new();

    /// <summary>The existing snake currently selected (via click), if any.</summary>
    private SnakeRenderer currentSelectedSnake = null;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ── Drawing ───────────────────────────────────────────────────────

    /// <summary>
    /// Handles a click on a grid point while in the editor. If the point already
    /// belongs to a snake, selects that snake instead of drawing. Otherwise adds the
    /// point to the in-progress path, filling in any intermediate points along a
    /// straight line from the previous point.
    /// </summary>
    public void HandleGridPointClick(GridPoint point) {

        // Clicking an occupied cell selects that snake instead of drawing.
        if (point.OccupiedSnake != null) {
            currentSelectedSnake = point.OccupiedSnake;
            currentSelectedSnake.StartHighlight();

            OnSnakeSelected?.Invoke(currentSelectedSnake);
            return;
        }

        currentSelectedSnake = null;
        currentSnakeGridPoints.Add(point);

        if (currentSnakeGridPoints.Count >= 2) {
            GridPoint previous = currentSnakeGridPoints[^2];
            GridPoint current = currentSnakeGridPoints[^1];

            List<GridPoint> between = gridGenerator.GetPointsBetween(previous, current);

            // Remove the just-added point so we can re-insert the full interpolated path.
            currentSnakeGridPoints.RemoveAt(currentSnakeGridPoints.Count - 1);

            foreach (GridPoint p in between) {
                if (currentSnakeGridPoints.Count == 0 || currentSnakeGridPoints[^1] != p)
                    currentSnakeGridPoints.Add(p);
            }

            OnSnakeCreationStarted?.Invoke(true);
        }
        else {
            OnSnakeCreationStarted?.Invoke(false);
        }
    }

    /// <summary>
    /// Finalizes the snake currently being drawn. Rejects it (showing a preview
    /// instead) if it overlaps another snake or moves diagonally, unless
    /// <see cref="CanOverlapSnake"/> / <see cref="CanGoDiagonal"/> allow it.
    /// </summary>
    public void FinishSnake() {
        if (IsSnakeCollidingWithSnake(currentSnakeGridPoints) && !CanOverlapSnake) {
            CreatePreviewAndClear();
            Debug.LogWarning("Cannot create snake on top of another snake!");
            return;
        }

        if (IsSnakeGoingDiagonal(currentSnakeGridPoints) && !CanGoDiagonal) {
            CreatePreviewAndClear();
            Debug.LogWarning("Cannot create diagonal snake!");
            return;
        }

        currentSnakeGridPoints.Reverse();
        snakeCreator.CreateSnakeFromEditor(currentSnakeGridPoints);
        currentSnakeGridPoints.Clear();
        OnSnakeCreated?.Invoke();
    }

    /// <summary>
    /// Creates a temporary preview snake from the current path and clears it, used when the path is invalid (overlapping or diagonal) and the user tries to finish it.
    /// </summary>
    private void CreatePreviewAndClear() {
        currentSnakeGridPoints.Reverse();

        snakeCreator.CreatePreviewSnakeFromEditor(
            currentSnakeGridPoints
        );

        currentSnakeGridPoints.Clear();
    }

    /// <summary>Returns true if any consecutive pair of points in the path moves diagonally (both x and y change by 1).</summary>
    private bool IsSnakeGoingDiagonal(List<GridPoint> currentSnakeGridPoints) {
        for (int i = 1; i < currentSnakeGridPoints.Count; i++) {
            GridPoint previous = currentSnakeGridPoints[i - 1];
            GridPoint current = currentSnakeGridPoints[i];

            if (Math.Abs(current.GridCoordinate.x - previous.GridCoordinate.x) == 1 &&
                Math.Abs(current.GridCoordinate.y - previous.GridCoordinate.y) == 1) {
                return true;
            }
        }
        return false;
    }

    /// <summary>Returns true if any point in the path already belongs to an existing snake.</summary>
    private bool IsSnakeCollidingWithSnake(List<GridPoint> currentSnakeGridPoints) {
        for (int i = 0; i < currentSnakeGridPoints.Count; i++) {
            if (currentSnakeGridPoints[i].OccupiedSnake != null) {
                Debug.LogWarning("Cannot create snake on top of another snake!");
                return true;
            }
        }
        return false;
    }

    /// <summary>Discards the snake currently being drawn and resets any highlighted points.</summary>
    public void CancelSnake() {
        foreach (var p in currentSnakeGridPoints)
            p.ResetColor();

        currentSnakeGridPoints.Clear();
    }

    // ── Selection / editing ──────────────────────────────────────────

    /// <summary>Deletes the currently selected snake, if any.</summary>
    public void DeleteSelectedSnake() {
        if (currentSelectedSnake == null) {
            Debug.LogWarning("No snake selected to delete!");
            return;
        }

        snakeCreator.DeleteSnakeFromEditor(currentSelectedSnake);
        currentSelectedSnake = null; // avoid dangling reference to a destroyed snake
    }

    /// <summary>
    /// Reverses the direction (head/tail) of the currently selected snake by
    /// deleting and recreating it with its points reversed.
    /// </summary>
    public void SwapHeadSnake() {
        if (currentSelectedSnake == null) {
            Debug.LogWarning("No snake selected to swap!");
            return;
        }

        List<GridPoint> points = new(currentSelectedSnake.OccupiedGridPoints);
        Color snakeColor = currentSelectedSnake.color;
        points.Reverse();

        snakeCreator.DeleteSnakeFromEditor(currentSelectedSnake);
        snakeCreator.CreateSnakeFromEditor(points, snakeColor);

        currentSnakeGridPoints.Clear();

        // Assumes the snake just created is the last entry in SpawnedSnakes.
        currentSelectedSnake = snakeCreator.SpawnedSnakes[^1];

        OnHeadSwapPerformed?.Invoke();
    }

    /// <summary>
    /// Shifts every snake on the board by <paramref name="offset"/> grid cells.
    /// Validates all new positions are in-bounds before moving anything (all-or-nothing);
    /// aborts with no changes if any snake would land outside the grid.
    /// </summary>
    public void NudgeLayout(Vector2Int offset) {
        var snapshot = new List<(SnakeRenderer snake, List<GridPoint> newPoints)>();

        // Pass 1: validate + collect new points for every snake.
        foreach (var snake in snakeCreator.SpawnedSnakes) {
            List<GridPoint> newPoints = new();

            foreach (var gp in snake.OccupiedGridPoints) {
                Vector2Int newCoord = gp.GridCoordinate + offset;

                if (!gridGenerator.CellMap.TryGetValue(newCoord, out GridPoint newGP)) {
                    Debug.LogWarning($"Nudge out of bounds at {newCoord}");
                    return;
                }

                newPoints.Add(newGP);
            }

            snapshot.Add((snake, newPoints));
        }

        // Pass 2: clear all old occupation first, so pass 3 doesn't collide with stale state.
        foreach (var (snake, _) in snapshot)
            foreach (var gp in snake.OccupiedGridPoints)
                gp.ClearIfOwnedBy(snake);

        // Pass 3: apply all new positions.
        foreach (var (snake, newPoints) in snapshot) {
            snake.OccupiedGridPoints = newPoints;
            snake.SetPoints(newPoints.ConvertAll(gp => gp.LocalPosition));

            foreach (var gp in newPoints)
                gp.OccupiedSnake = snake;
        }

        OnNudgeLayoutPerformed?.Invoke();
    }

    // ── Save ──────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="SnakeLevelData"/> asset from every currently spawned snake
    /// and prompts the user (Editor only) to save it to disk.
    /// </summary>
    public void SaveLevel() {
        SnakeLevelData levelData = ScriptableObject.CreateInstance<SnakeLevelData>();

        foreach (var snake in snakeCreator.SpawnedSnakes) {
            SnakeData snakeData = new SnakeData();
            snakeData.color = snake.color;

            foreach (var point in snake.OccupiedGridPoints) {
                snakeData.cells.Add(point.GridCoordinate);
            }

            levelData.snakes.Add(snakeData);
        }

#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
            "Save Level Data", "NewSnakeLevel", "asset", "New Snake Level Data");

        if (!string.IsNullOrEmpty(path)) {
            UnityEditor.AssetDatabase.CreateAsset(levelData, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"Level data saved to {path}");
        }


#else

    Debug.LogWarning(
        "Save Level is not supported in this build yet. Runtime saving will be added soon."
    );

    Destroy(levelData);

#endif
    }


    private void OnDestroy() {
        OnSnakeCreated = null;
        OnSnakeSelected = null;
        OnHeadSwapPerformed = null;
    }
}