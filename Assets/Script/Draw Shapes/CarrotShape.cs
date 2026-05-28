// ── Carrot (organic polygon approximation) ────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class CarrotShape : IGridShape {
    public string ShapeName => "Carrot";

    // Pre-built normalized carrot polygon (UV space -1..1)
    // Tall narrow triangle body + small bumps at top for leaves
    private static readonly Vector2[] CarrotPoly = new Vector2[]
    {
        new Vector2( 0.00f,  1.00f),  // leaf tip center
        new Vector2( 0.15f,  0.80f),  // leaf right
        new Vector2( 0.30f,  0.90f),  // leaf bump right
        new Vector2( 0.25f,  0.60f),  // shoulder right
        new Vector2( 0.20f,  0.20f),  // body upper right
        new Vector2( 0.10f, -0.30f),  // body mid right
        new Vector2( 0.04f, -0.70f),  // body lower right
        new Vector2( 0.00f, -1.00f),  // tip bottom
        new Vector2(-0.04f, -0.70f),  // body lower left
        new Vector2(-0.10f, -0.30f),  // body mid left
        new Vector2(-0.20f,  0.20f),  // body upper left
        new Vector2(-0.25f,  0.60f),  // shoulder left
        new Vector2(-0.30f,  0.90f),  // leaf bump left
        new Vector2(-0.15f,  0.80f),  // leaf left
    };

    public bool IsPointInside(Vector2 point, Vector2 center, float size) {
        Vector2 local = (point - center) / size;
        return PointInPolygon(local, CarrotPoly);
    }

    public bool IsPointOnOutline(Vector2 point, Vector2 center, float size, float thickness) {
        return IsPointInside(point, center, size + thickness)
            && !IsPointInside(point, center, size - thickness);
    }

    public List<Vector2> GetOutlinePoints(Vector2 center, float size, int resolution = 0) {
        var pts = new List<Vector2>();
        foreach (var v in CarrotPoly)
            pts.Add(center + v * size);
        return pts;
    }

    // Ray-casting point-in-polygon
    private bool PointInPolygon(Vector2 p, Vector2[] poly) {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++) {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
            j = i;
        }
        return inside;
    }
}