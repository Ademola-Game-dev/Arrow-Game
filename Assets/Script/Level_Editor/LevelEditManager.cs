using System.Collections.Generic;
using UnityEngine;

public class LevelEditManager : MonoBehaviour {
    public static LevelEditManager Instance { get; private set; }

    public bool IsInEditMode = true;

    private List<GridPoint> currentSnakeGridPoints = new();

    void Awake() {
        Instance = this;
    }

    public void HandleGridPointClick(GridPoint point) {
        if (!IsInEditMode)
            return;

        // Don't allow using occupied cells
        if (point.OccupiedSnake != null)
            return;

        currentSnakeGridPoints.Add(point);
    }

    bool IsAdjacent(GridPoint a, GridPoint b) {
        Vector2Int diff = b.GridCoordinate - a.GridCoordinate;

        return Mathf.Abs(diff.x) + Mathf.Abs(diff.y) == 1;
    }

    [ContextMenu("Finish Snake")]
    public void FinishSnake() {
    
        SnakeCreator.Instance.CreateSnakeFromEditor(currentSnakeGridPoints);

        currentSnakeGridPoints.Clear();
    }

    [ContextMenu("Cancel Snake")]
    public void CancelSnake() {
        foreach (var p in currentSnakeGridPoints)
            p.ResetColor();

        currentSnakeGridPoints.Clear();
    }
}