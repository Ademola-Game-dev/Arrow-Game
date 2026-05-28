using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour {

    public enum ShapeType { Circle, Square, Triangle, Diamond, Carrot }

    [Header("Grid Settings")]
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private ShapeType shapeType = ShapeType.Circle;
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 5;
    [SerializeField] private float sizeScale = 0.9f;  // relative to grid half-width
    [SerializeField] private float outlineThickness = 0.6f; // in local units
    private IGridShape _shape;

    public List<Vector2> GridPoints { get; private set; } = new();

    void Start() {
        _shape = CreateShape(shapeType);
        GridPoints = GenerateGrid(columns, rows);
    }

    public List<Vector2> GenerateGrid(int cols, int rows) {
        List<Vector2> points = new();

        if (gridParent == null) {
            Debug.LogWarning("[GridGenerator] gridParent is not assigned!");
            return points;
        }

        // Get the actual world size of the Rect
        float rectWidth = gridParent.rect.width;
        float rectHeight = gridParent.rect.height;

        // Spacing based on rect size divided by number of cells
        // cols-1 / rows-1 so points sit ON the edges too
        float xStep = cols > 1 ? rectWidth / (cols - 1) : 0f;
        float yStep = rows > 1 ? rectHeight / (rows - 1) : 0f;

        // Start from bottom-left corner of the Rect in local space
        float startX = -rectWidth / 2f;
        float startY = -rectHeight / 2f;

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < cols; x++) {
                float px = startX + x * xStep;
                float py = startY + y * yStep;
                points.Add(new Vector2(px, py));
            }
        }

        Debug.Log($"[GridGenerator] {points.Count} points inside Rect ({rectWidth}x{rectHeight})");
        return points;
    }

    // Recalculate if screen/rect changes at runtime
    [ContextMenu("Refresh Grid Points")]
    public void Refresh() {
        _shape = CreateShape(shapeType);
        GridPoints = GenerateGrid(columns, rows);
    }

    private static IGridShape CreateShape(ShapeType type) => type switch {
        ShapeType.Circle => new CircleShape(),
        ShapeType.Square => new SquareShape(),
        ShapeType.Triangle => new TriangleShape(),
        ShapeType.Diamond => new DiamondShape(),
        ShapeType.Carrot => new CarrotShape(),
        _ => new CircleShape(),
    };

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (gridParent == null) return;

        _shape ??= CreateShape(shapeType);

        float W = gridParent.rect.width;
        float H = gridParent.rect.height;
        float xStep = columns > 1 ? W / (columns - 1) : 0f;
        float yStep = rows > 1 ? H / (rows - 1) : 0f;
        float startX = -W / 2f;
        float startY = -H / 2f;
        float cellSize = Mathf.Min(xStep, yStep);
        float dotSize = cellSize * 0.35f;

        Vector2 center = Vector2.zero;
        float radius = Mathf.Min(W, H) / 2f * sizeScale;
        float thick = cellSize * outlineThickness;

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < columns; x++) {
                Vector2 local = new Vector2(startX + x * xStep, startY + y * yStep);
                Vector3 world = gridParent.TransformPoint(local);

                if (_shape.IsPointOnOutline(local, center, radius, thick)) {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(world, dotSize * 1.2f);
                }
                else if (_shape.IsPointInside(local, center, radius)) {
                    Gizmos.color = new Color(0f, 1f, 0.4f, 0.85f);
                    Gizmos.DrawSphere(world, dotSize);
                }
                else {
                    Gizmos.color = new Color(1f, 1f, 1f, 0.08f);
                    Gizmos.DrawSphere(world, dotSize * 0.4f);
                }
            }
        }

        // Smooth wire outline
        Gizmos.color = Color.yellow;
        var outline = _shape.GetOutlinePoints(center, radius);
        for (int i = 0; i < outline.Count; i++) {
            Vector3 a = gridParent.TransformPoint(outline[i]);
            Vector3 b = gridParent.TransformPoint(outline[(i + 1) % outline.Count]);
            Gizmos.DrawLine(a, b);
        }

        // Center cross
        Gizmos.color = Color.red;
        Vector3 c3 = gridParent.TransformPoint(center);
        Gizmos.DrawLine(c3 - Vector3.right * cellSize, c3 + Vector3.right * cellSize);
        Gizmos.DrawLine(c3 - Vector3.up * cellSize, c3 + Vector3.up * cellSize);
    }
#endif
}