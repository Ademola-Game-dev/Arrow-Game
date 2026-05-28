using UnityEngine;
using System.Collections.Generic;

public interface IGridShape {
    // Is this grid point inside the shape?
    bool IsPointInside(Vector2 point, Vector2 center, float size);

    // Is this grid point on the outline ring?
    bool IsPointOnOutline(Vector2 point, Vector2 center, float size, float thickness);

    // Optional: smooth wire overlay points for Gizmos
    List<Vector2> GetOutlinePoints(Vector2 center, float size, int resolution = 64);

    // Display name
    string ShapeName { get; }
}