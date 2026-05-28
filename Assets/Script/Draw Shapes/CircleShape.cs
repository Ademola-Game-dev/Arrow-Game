using UnityEngine;
using System.Collections.Generic;

// ── Circle ────────────────────────────────────────────────────────────────────
public class CircleShape : IGridShape {
    public string ShapeName => "Circle";

    public bool IsPointInside(Vector2 point, Vector2 center, float size) {
        return Vector2.Distance(point, center) < size;
    }

    public bool IsPointOnOutline(Vector2 point, Vector2 center, float size, float thickness) {
        float dist = Vector2.Distance(point, center);
        return dist >= size - thickness && dist <= size + thickness;
    }

    public List<Vector2> GetOutlinePoints(Vector2 center, float size, int resolution = 64) {
        var pts = new List<Vector2>();
        for (int i = 0; i < resolution; i++) {
            float a = Mathf.Deg2Rad * (360f / resolution * i);
            pts.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size);
        }
        return pts;
    }
}