using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates a rectangular grid of evenly spaced points inside <see cref="gridParent"/>,
/// optionally spawning a clickable <see cref="GridPoint"/> UI element at each position.
/// Exposes fast lookups from local position (<see cref="PointMap"/>) and grid coordinate
/// (<see cref="CellMap"/>) to the corresponding <see cref="GridPoint"/>.
/// </summary>
public class GridGenerator : MonoBehaviour {

    /// <summary>Global access point for the active GridGenerator instance.</summary>
    public static GridGenerator Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 5;

    /// <summary>The RectTransform all grid points and grid-spawned objects are parented under.</summary>
    public RectTransform GridParent => gridParent;

    /// <summary>All generated grid point positions, in gridParent-local space.</summary>
    public List<Vector2> GridPoints { get; private set; } = new();

    [Header("Grid Point Interaction")]
    [SerializeField] private bool spawnClickPoints = true;
    [SerializeField] private float pointClickSize = 20f; // size of hitbox in UI units

    /// <summary>Lookup: local position → GridPoint.</summary>
    public Dictionary<Vector2, GridPoint> PointMap { get; private set; } = new();

    /// <summary>Lookup: grid coordinate (column, row) → GridPoint.</summary>
    public Dictionary<Vector2Int, GridPoint> CellMap { get; private set; } = new();

    private void Awake() {
        Instance = this;
        GenerateAndClassify();
    }

    /// <summary>Regenerates the grid; callable from the editor context menu for quick iteration.</summary>
    [ContextMenu("Refresh Grid Points")]
    public void Refresh() {
        GenerateAndClassify();
    }

    // ── Core ──────────────────────────────────────────────────────────

    /// <summary>
    /// Clears any previously spawned grid points, computes an evenly spaced grid
    /// spanning <see cref="gridParent"/>'s rect, and (optionally) spawns a
    /// <see cref="GridPoint"/> UI object at each position.
    /// </summary>
    private void GenerateAndClassify() {
        if (gridParent == null) {
            Debug.LogWarning("[GridGenerator] gridParent not assigned!");
            return;
        }

        // Clear old point GameObjects
        foreach (var kv in PointMap)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        PointMap.Clear();
        CellMap.Clear();

        float W = gridParent.rect.width;
        float H = gridParent.rect.height;
        float xStep = columns > 1 ? W / (columns - 1) : 0f;
        float yStep = rows > 1 ? H / (rows - 1) : 0f;
        float startX = -W / 2f;
        float startY = -H / 2f;

        GridPoints.Clear();

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < columns; x++) {
                Vector2 point = new Vector2(startX + x * xStep, startY + y * yStep);
                GridPoints.Add(point);

                // ── Spawn clickable UI point ──────────────────────────
                if (spawnClickPoints)
                    SpawnGridPoint(point, new Vector2Int(x, y));
            }
        }

        gridParent.sizeDelta = Vector2.zero;
    }

    /// <summary>Instantiates a single clickable <see cref="GridPoint"/> at the given local position and grid coordinate.</summary>
    private void SpawnGridPoint(Vector2 localPos, Vector2Int coord) {
        GameObject go = new GameObject($"GP_{coord.x}_{coord.y}");
        go.transform.SetParent(gridParent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPos;
        rt.sizeDelta = new Vector2(pointClickSize, pointClickSize);

        GridPoint gp = go.AddComponent<GridPoint>();
        gp.LocalPosition = localPos;
        gp.GridCoordinate = coord;

        PointMap[localPos] = gp;
        CellMap[coord] = gp;
    }

    /// <summary>
    /// Returns every <see cref="GridPoint"/> along the straight line (horizontal,
    /// vertical, or diagonal) between two grid points, inclusive of both endpoints.
    /// Uses coordinate lerping rather than pathfinding, so it's only accurate for
    /// points that lie on an 8-directional line; non-aligned points will produce
    /// a stair-stepped rather than exact path.
    /// </summary>
    public List<GridPoint> GetPointsBetween(GridPoint point1, GridPoint point2) {
        List<GridPoint> result = new();

        Vector2Int start = point1.GridCoordinate;
        Vector2Int end = point2.GridCoordinate;

        int dx = end.x - start.x;
        int dy = end.y - start.y;

        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        if (steps == 0) {
            result.Add(point1);
            return result;
        }

        for (int i = 0; i <= steps; i++) {
            float t = i / (float)steps;

            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));

            Vector2Int coord = new(x, y);

            if (CellMap.TryGetValue(coord, out GridPoint gp)) {
                // Prevent duplicates caused by rounding
                if (result.Count == 0 || result[^1] != gp)
                    result.Add(gp);
            }
        }

        return result;
    }
}