using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns, tracks, and removes snake instances for the active level.
/// Converts grid-space paths into UI-space <see cref="SnakeRenderer"/> visuals,
/// registers snakes with their occupied <see cref="GridPoint"/>s, and raises
/// <see cref="OnAllSnakesRemoved"/> once the board is cleared.
/// </summary>
public class SnakeCreator : MonoBehaviour {

    /// <summary>Global access point for the active SnakeCreator instance.</summary>
    public static SnakeCreator Instance { get; private set; }

    /// <summary>Raised when the last remaining snake has been removed (level complete).</summary>
    public event Action OnAllSnakesRemoved;

    [Header("References")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private RectTransform snakeContainer; // parent UI panel

    [Header("Snake Settings")]
    [SerializeField] private float snakeBodyWidth = 20f;

    [Header("Snake Materials")]
    [SerializeField] private Material snakeBodyMaterial;

    private readonly List<SnakeRenderer> _snakes = new();

    /// <summary>Read-only view of all currently spawned (non-preview) snakes.</summary>
    public IReadOnlyList<SnakeRenderer> SpawnedSnakes => _snakes;

    /// <summary>Colors used when no explicit color is provided (e.g. editor-created snakes).</summary>
    private static readonly Color[] Palette = {
        Color.red, Color.blue, Color.green, Color.magenta, Color.yellow, Color.cyan
    };

    private void Awake() {
        Instance = this;
    }

    // ── Level loading ─────────────────────────────────────────────────

    /// <summary>
    /// Spawns every snake defined in <paramref name="levelData"/>, mapping each
    /// snake's grid cells to their corresponding <see cref="GridPoint"/> positions.
    /// </summary>
    public void LoadLevel(SnakeLevelData levelData) {
        if (levelData == null) {
            Debug.LogWarning("[SnakeCreator] LoadLevel called with null levelData.");
            return;
        }

        foreach (var snake in levelData.snakes) {
            List<Vector2> path = new();

            foreach (var cell in snake.cells) {
                if (gridGenerator.CellMap.TryGetValue(cell, out GridPoint gp)) {
                    path.Add(gp.LocalPosition);
                }
            }

            SpawnSnake(path, snake.color);
        }
    }

    // ── Spawn ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="SnakeRenderer"/> along <paramref name="path"/> (given in
    /// grid-local space) and parents it under <see cref="snakeContainer"/>.
    /// Non-preview snakes are tracked in <see cref="_snakes"/> and registered with
    /// the grid points they occupy; preview snakes are auto-destroyed after 2 seconds
    /// and are not tracked or registered.
    /// </summary>
    /// <param name="path">Snake path in grid-local space, needs at least 2 distinct points.</param>
    /// <param name="color">Color to render the snake with.</param>
    /// <param name="isPreview">If true, spawns a temporary, untracked preview snake.</param>
    /// <returns>The spawned <see cref="SnakeRenderer"/>, or null if the path was invalid.</returns>
    private SnakeRenderer SpawnSnake(List<Vector2> path, Color color, bool isPreview = false) {

        if (path == null || path.Count < 2) {
            Debug.LogWarning("[SnakeCreator] path too short for snake spawn.");
            return null;
        }
        if (Vector2.Distance(path[0], path[1]) < 0.001f) {
            Debug.LogWarning("[SnakeCreator] path points too close for snake spawn.");
            return null;
        }

        GameObject go = new GameObject("Snake");
        go.transform.SetParent(snakeContainer, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        SnakeRenderer line = go.AddComponent<SnakeRenderer>();
        line.lineWidth = snakeBodyWidth;
        line.material = snakeBodyMaterial;

        // ── Convert grid local points → world → snake local ──────────
        List<Vector2> converted = new();
        foreach (var p in path) {
            Vector3 world = gridGenerator.GridParent.TransformPoint(p);
            Vector3 local = rt.InverseTransformPoint(world);
            converted.Add(new Vector2(local.x, local.y));
        }

        line.SetPoints(converted);
        line.SetColor(color);

        if (!isPreview) {
            _snakes.Add(line);

            // ── Register snake on each GridPoint ──────────────────────
            List<GridPoint> occupiedGp = new();

            for (int i = 0; i < path.Count; i++) {
                if (gridGenerator.PointMap.TryGetValue(path[i], out GridPoint gp)) {
                    gp.SetOccupied(line, i);
                    occupiedGp.Add(gp);
                }
            }

            line.SetOccupiedGridPoints(occupiedGp);
        }
        else {
            Destroy(line.gameObject, 2f);
        }

        return line;
    }

    /// <summary>Picks a random color from <see cref="Palette"/>.</summary>
    private Color RandomColor() => Palette[UnityEngine.Random.Range(0, Palette.Length)];

    // ── Editor API ────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a permanent snake from a list of grid points, used by the Level Editor.
    /// Uses <paramref name="snakeColor"/> if provided, otherwise a random palette color.
    /// </summary>
    public SnakeRenderer CreateSnakeFromEditor(List<GridPoint> points, Color? snakeColor = null) {
        List<Vector2> path = new();
        foreach (var p in points) path.Add(p.LocalPosition);
        return SpawnSnake(path, snakeColor ?? RandomColor());
    }

    /// <summary>Removes and destroys a snake created via the Level Editor.</summary>
    public void DeleteSnakeFromEditor(SnakeRenderer currentSelectedSnake) {
        _snakes.Remove(currentSelectedSnake);
        Destroy(currentSelectedSnake.gameObject);
    }

    /// <summary>
    /// Spawns a temporary, semi-transparent red preview snake (used while dragging
    /// out a new snake in the Level Editor). Not tracked, auto-destroyed after 2s.
    /// </summary>
    public void CreatePreviewSnakeFromEditor(List<GridPoint> points) {
        List<Vector2> path = new();
        foreach (var p in points) path.Add(p.LocalPosition);

        Color preview = new Color(1f, 0f, 0f, 0.5f);
        SpawnSnake(path, preview, true);
    }

    // ── Removal / completion ─────────────────────────────────────────

    /// <summary>
    /// Removes a snake from the tracked list (e.g. after the player clears it).
    /// Raises <see cref="OnAllSnakesRemoved"/> if this was the last remaining snake.
    /// </summary>
    public void RemoveSnakeFromList(SnakeRenderer snakeRenderer) {
        if (!_snakes.Contains(snakeRenderer)) {
            Debug.LogWarning("[SnakeCreator] Snake not found in list.");
            return;
        }

        _snakes.Remove(snakeRenderer);

        if (CheckCompletionLevel()) {
            Debug.Log("[SnakeCreator] All snakes removed.");
            OnAllSnakesRemoved?.Invoke();
        }
    }

    private bool CheckCompletionLevel() => _snakes.Count <= 0;

    /// <summary>Destroys and clears every currently tracked snake.</summary>
    public void DeleteAllSnakes() {
        foreach (var snake in _snakes) {
            Destroy(snake.gameObject);
        }
        _snakes.Clear();
    }
}