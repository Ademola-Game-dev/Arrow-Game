// ── Diamond ───────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class DiamondShape : IGridShape {
    public string ShapeName => "Diamond";

    public bool IsPointInside(Vector2 point, Vector2 center, float size) {
        Vector2 d = point - center;
        return Mathf.Abs(d.x) + Mathf.Abs(d.y) < size; // Manhattan distance
    }

    public bool IsPointOnOutline(Vector2 point, Vector2 center, float size, float thickness) {
        Vector2 d = point - center;
        float m = Mathf.Abs(d.x) + Mathf.Abs(d.y);
        return m >= size - thickness && m <= size + thickness;
    }

    public List<Vector2> GetOutlinePoints(Vector2 center, float size, int resolution = 4) {
        return new List<Vector2>
        {
            center + new Vector2(0,  size),
            center + new Vector2( size, 0),
            center + new Vector2(0, -size),
            center + new Vector2(-size, 0),
        };
    }
}
