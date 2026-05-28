// ── Triangle ──────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class TriangleShape : IGridShape {
    public string ShapeName => "Triangle";

    public bool IsPointInside(Vector2 point, Vector2 center, float size) {
        // Equilateral triangle pointing up
        Vector2 p = point - center;
        float h = size * Mathf.Sqrt(3f) / 2f;

        Vector2 a = new Vector2(0, size * 0.577f * 2f);  // top
        Vector2 b = new Vector2(-size, -size * 0.577f);        // bottom-left
        Vector2 c = new Vector2(size, -size * 0.577f);        // bottom-right

        return SameSide(p, a, b, c)
            && SameSide(p, b, a, c)
            && SameSide(p, c, a, b);
    }

    public bool IsPointOnOutline(Vector2 point, Vector2 center, float size, float thickness) {
        return IsPointInside(point, center, size + thickness)
            && !IsPointInside(point, center, size - thickness);
    }

    public List<Vector2> GetOutlinePoints(Vector2 center, float size, int resolution = 3) {
        return new List<Vector2>
        {
            center + new Vector2(0,      size * 0.577f * 2f),
            center + new Vector2(-size, -size * 0.577f),
            center + new Vector2( size, -size * 0.577f),
        };
    }

    private bool SameSide(Vector2 p, Vector2 a, Vector2 b, Vector2 c) {
        float d1 = Cross(c - b, p - b);
        float d2 = Cross(c - b, a - b);
        return Mathf.Sign(d1) == Mathf.Sign(d2) || Mathf.Abs(d1) < 0.0001f;
    }

    private float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}