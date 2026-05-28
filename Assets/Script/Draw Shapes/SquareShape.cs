// ── Square ────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class SquareShape : IGridShape {
    public string ShapeName => "Square";

    public bool IsPointInside(Vector2 point, Vector2 center, float size) {
        Vector2 d = point - center;
        return Mathf.Abs(d.x) < size && Mathf.Abs(d.y) < size;
    }

    public bool IsPointOnOutline(Vector2 point, Vector2 center, float size, float thickness) {
        Vector2 d = point - center;
        float ax = Mathf.Abs(d.x);
        float ay = Mathf.Abs(d.y);
        bool insideOuter = ax <= size + thickness && ay <= size + thickness;
        bool outsideInner = ax >= size - thickness || ay >= size - thickness;
        return insideOuter && outsideInner;
    }

    public List<Vector2> GetOutlinePoints(Vector2 center, float size, int resolution = 4) {
        return new List<Vector2>
        {
            center + new Vector2(-size, -size),
            center + new Vector2( size, -size),
            center + new Vector2( size,  size),
            center + new Vector2(-size,  size),
        };
    }
}