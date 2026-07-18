using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Named colors a snake can be assigned in the Level Editor.</summary>
public enum SnakeColor {
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Cyan,
    Orange,
    Magenta,
    Brown,
    Violet,
}

/// <summary>
/// Custom <see cref="Graphic"/> that renders a snake as a thick rounded line with a
/// stylized head (eyes + tongue), and handles snake-specific gameplay behavior:
/// moving off-screen when cleared, colliding with grid occupancy while moving,
/// and a brief highlight-fade when selected in the editor.
/// </summary>
public class SnakeRenderer : Graphic {

    /// <summary>True while a movement coroutine (off-screen or collide-and-return) is in progress.</summary>
    public bool IsDoingMove { get; private set; } = false;

    [SerializeField] public List<Vector2> Points = new();
    [SerializeField] public float lineWidth = 5f;
    [SerializeField][Range(3, 12)] public int joinSegments = 10;

    [Header("Snake Head")]
    [SerializeField] public bool showHead = true;
    [SerializeField] public float headSize = 1.6f;  // multiplier of lineWidth
    [SerializeField] public Color headColor = Color.red;
    [SerializeField] public int eyeSegments = 8;
    [SerializeField] public float tongueLength = 0.4f;

    [Header("Click")]
    [SerializeField] public Color highlightColor = Color.white;
    [SerializeField] public float highlightDuration = 0.3f; // seconds to fade back
    private Color _originalColor;

    private Coroutine _highlightCoroutine;
    private GridPoint _headGridPoint;

    [Header("Debug")]
    private Vector2 _debugHeadPos;
    private Vector2 _debugDir;

    [Header("Movement")]
    private readonly float moveSpeed = 7000f;
    private readonly float maxMoveDistance = 3000f; // how far to move off screen

    private List<GridPoint> towarsGridPoints;

    /// <summary>Grid points currently occupied by this snake's body.</summary>
    public List<GridPoint> OccupiedGridPoints = new();

    private List<Vector2> originalPoints;

    /// <summary>The logical color category of this snake (kept in sync with <see cref="Graphic.color"/>).</summary>
    public SnakeColor SnakeColor;

    // Colors Unity's built-in Color struct doesn't provide. Values here must match
    // LevelEditUi.GetColor exactly — both files map SnakeColor <-> Color independently,
    // and GetSnakeColor below reverses the mapping by comparing against these constants.
    private static readonly Color OrangeColor = new Color(1f, 0.5f, 0f);
    private static readonly Color PurpleColor = new Color(0.5f, 0f, 0.5f);
    private static readonly Color BrownColor = new Color(0.6f, 0.4f, 0.2f);
    private static readonly Color VioletColor = new Color(0.56f, 0f, 1f);

    protected override void Awake() {
        base.Awake();
        // Graphic requires a CanvasRenderer — Unity adds it but just in case:
        if (GetComponent<CanvasRenderer>() == null)
            gameObject.AddComponent<CanvasRenderer>();

        raycastTarget = false; // enables click detection on the mesh
        _originalColor = color;
    }

    // ── Movement ──────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to move the snake off-screen (e.g. after it's cleared). If the path
    /// ahead is blocked by another snake, instead moves forward until it hits the
    /// blocker and then returns to its original position.
    /// </summary>
    public void MoveSnakeOffScreen() {
        Debug.Log($"[UILineRenderer] Moving snake off screen: {name}");

        towarsGridPoints = GetTowardsGridPoints();

        bool canGoOutOffScreen = true;
        List<GridPoint> collideBeforeGridPoints = new();

        foreach (var gp in towarsGridPoints) {
            Debug.Log($"Point {gp.LocalPosition} Occupied:{gp.IsOccupied() && gp.OccupiedSnake != this}");

            if (gp.IsOccupied() && gp.OccupiedSnake != this) {
                canGoOutOffScreen = false;
                break;
            }

            collideBeforeGridPoints.Add(gp);
        }

        Debug.Log($"Can go off screen: {canGoOutOffScreen}");

        if (canGoOutOffScreen) {
            StartCoroutine(MoveOffScreenCoroutine());

            // ── Un-register snake on each GridPoint ──────────────────────
            foreach (var gp in OccupiedGridPoints)
                gp.ClearIfOwnedBy(this);
        }
        else {
            // ── Blocked: move forward up to the blocker, then return to original position ──
            originalPoints = new List<Vector2>(Points);
            StartCoroutine(MoveCollideBeforeGridPointsCoroutine(collideBeforeGridPoints));
        }
    }

    /// <summary>Moves the head forward along <paramref name="path"/>, then reverses back to the starting position.</summary>
    private IEnumerator MoveCollideBeforeGridPointsCoroutine(List<GridPoint> path) {
        if (path.Count == 0)
            yield break;

        IsDoingMove = true;

        Vector2 startHeadPos = Points[0];

        // Move forward along the path.
        foreach (var gp in path) {
            Vector2 target = gp.LocalPosition;

            while (Vector2.Distance(Points[0], target) > 5f) {
                List<Vector2> oldPositions = new List<Vector2>(Points);

                Points[0] = Vector2.MoveTowards(Points[0], target, moveSpeed * Time.deltaTime);

                for (int i = 1; i < Points.Count; i++)
                    Points[i] = oldPositions[i - 1];

                SetVerticesDirty();
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.2f);

        // Reverse back to the original position.
        while (Vector2.Distance(Points[0], startHeadPos) > 5f) {
            for (int i = 0; i < Points.Count; i++) {
                Points[i] = Vector2.MoveTowards(Points[i], originalPoints[i], moveSpeed * Time.deltaTime);
            }

            SetVerticesDirty();
            yield return null;
        }

        Points = new List<Vector2>(originalPoints);
        SetVerticesDirty();

        IsDoingMove = false;
    }

    /// <summary>Moves the whole snake off-screen in a straight line, then removes it from tracking and deactivates it.</summary>
    private IEnumerator MoveOffScreenCoroutine() {
        Vector2 dir = (Points[0] - Points[1]).normalized;
        Vector2 startPos = Points[0];

        while (true) {
            float travelled = Vector2.Distance(startPos, Points[0]);

            if (travelled >= maxMoveDistance) {
                SnakeCreator.Instance.RemoveSnakeFromList(this);
                gameObject.SetActive(false);
                yield break;
            }

            List<Vector2> oldPositions = new List<Vector2>(Points);

            Points[0] += moveSpeed * Time.deltaTime * dir;

            for (int i = 1; i < Points.Count; i++)
                Points[i] = oldPositions[i - 1];

            SetVerticesDirty();
            yield return null;
        }
    }

    /// <summary>Finds and caches the <see cref="GridPoint"/> nearest to the snake's current head position.</summary>
    private void SetHeadGridPoint() {
        float bestDist = float.MaxValue;
        Vector2 headPos = Points[0];

        foreach (var kv in GridGenerator.Instance.PointMap) {
            float d = Vector2.Distance(headPos, kv.Key);

            if (d < bestDist) {
                bestDist = d;
                _headGridPoint = kv.Value;
            }
        }
    }

    /// <summary>
    /// Returns every grid point straight ahead of the snake's head (in its current
    /// facing direction), sorted nearest-first. Used to check the path is clear
    /// before moving off-screen.
    /// </summary>
    private List<GridPoint> GetTowardsGridPoints() {
        List<GridPoint> result = new();

        SetHeadGridPoint();

        if (_headGridPoint == null)
            return result;

        Vector2 dir = (Points[0] - Points[1]).normalized;
        Vector2 headPos = _headGridPoint.LocalPosition;

        _debugDir = dir;
        _debugHeadPos = headPos;

        foreach (var kvp in GridGenerator.Instance.PointMap) {
            Vector2 pos = kvp.Key;

            if (dir == Vector2.right && Mathf.Approximately(pos.y, headPos.y) && pos.x > headPos.x)
                result.Add(kvp.Value);
            else if (dir == Vector2.left && Mathf.Approximately(pos.y, headPos.y) && pos.x < headPos.x)
                result.Add(kvp.Value);
            else if (dir == Vector2.up && Mathf.Approximately(pos.x, headPos.x) && pos.y > headPos.y)
                result.Add(kvp.Value);
            else if (dir == Vector2.down && Mathf.Approximately(pos.x, headPos.x) && pos.y < headPos.y)
                result.Add(kvp.Value);
        }

        result.Sort((a, b) => {
            float da = Vector2.Distance(a.LocalPosition, headPos);
            float db = Vector2.Distance(b.LocalPosition, headPos);
            return da.CompareTo(db);
        });

        return result;
    }

    // ── Mesh generation ───────────────────────────────────────────────

    protected override void OnPopulateMesh(VertexHelper vh) {
        vh.Clear();

        // ── Guard: need at least 2 distinct points ────────────────────
        if (Points == null || Points.Count < 2) return;
        if (Vector2.Distance(Points[0], Points[1]) < 0.001f) return;

        // ── Body segments ─────────────────────────────────────────────
        for (int i = 0; i < Points.Count - 1; i++)
            DrawSegment(vh, Points[i], Points[i + 1]);

        // ── Corner joins ──────────────────────────────────────────────
        for (int i = 1; i < Points.Count - 1; i++)
            DrawCircle(vh, Points[i], lineWidth * 0.5f, color);

        // ── Tail cap ──────────────────────────────────────────────────
        DrawCircle(vh, Points[Points.Count - 1], lineWidth * 0.5f, color);

        // ── Snake head ────────────────────────────────────────────────
        if (showHead)
            DrawHead(vh, Points[0], Points[1]);
    }

    // ── Head ──────────────────────────────────────────────────────────

    /// <summary>Draws the head circle, snout, eyes with pupils, and tongue at the given head position.</summary>
    private void DrawHead(VertexHelper vh, Vector2 headPos, Vector2 nextPos) {
        float headRadius = lineWidth * headSize * 0.5f;
        Vector2 forward = (headPos - nextPos).normalized;

        // ── Safety: if forward is zero, skip head entirely ────────────
        if (forward == Vector2.zero) return;

        Vector2 right = new Vector2(-forward.y, forward.x);

        // ── Head circle ───────────────────────────────────────────────
        DrawCircle(vh, headPos, headRadius, color);

        // ── Snout bump ────────────────────────────────────────────────
        Vector2 snoutPos = headPos + forward * (headRadius * 0.5f);
        DrawCircle(vh, snoutPos, headRadius * 0.55f, color);

        // ── Eyes ──────────────────────────────────────────────────────
        float eyeRadius = headRadius * 0.28f;
        float eyeOffset = headRadius * 0.42f;
        float eyeForward = headRadius * 0.25f;

        Vector2 leftEye = headPos + right * eyeOffset + forward * eyeForward;
        Vector2 rightEye = headPos - right * eyeOffset + forward * eyeForward;

        DrawCircle(vh, leftEye, eyeRadius, Color.white);
        DrawCircle(vh, rightEye, eyeRadius, Color.white);

        float pupilRadius = eyeRadius * 0.55f;
        Vector2 pupilPush = forward * (eyeRadius * 0.2f);

        DrawCircle(vh, leftEye + pupilPush, pupilRadius, Color.black);
        DrawCircle(vh, rightEye + pupilPush, pupilRadius, Color.black);

        // ── Tongue ────────────────────────────────────────────────────
        DrawTongue(vh, headPos, forward, right, headRadius);
    }

    /// <summary>Draws a forked tongue extending forward from the head.</summary>
    private void DrawTongue(VertexHelper vh, Vector2 headPos, Vector2 forward, Vector2 right, float headRadius) {
        Color tongueColor = new Color(0.9f, 0.1f, 0.2f);
        float tongueWidth = lineWidth * 0.12f;
        float stemLen = headRadius * tongueLength;
        float forkLen = stemLen * 0.4f;
        float forkSpread = headRadius * 0.28f;

        Vector2 tongueBase = headPos + forward * headRadius * 0.85f;
        Vector2 tongueTip = tongueBase + forward * stemLen;

        DrawThickLine(vh, tongueBase, tongueTip, tongueWidth, tongueColor);
        DrawThickLine(vh, tongueTip, tongueTip + forward * forkLen + right * forkSpread, tongueWidth * 0.8f, tongueColor);
        DrawThickLine(vh, tongueTip, tongueTip + forward * forkLen - right * forkSpread, tongueWidth * 0.8f, tongueColor);
    }

    // ── Primitives ────────────────────────────────────────────────────

    private void DrawSegment(VertexHelper vh, Vector2 a, Vector2 b) {
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (lineWidth * 0.5f);
        int idx = vh.currentVertCount;

        AddVert(vh, a - perp, color);
        AddVert(vh, a + perp, color);
        AddVert(vh, b + perp, color);
        AddVert(vh, b - perp, color);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx);
    }

    private void DrawThickLine(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c) {
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (width * 0.5f);
        int idx = vh.currentVertCount;

        AddVert(vh, a - perp, c);
        AddVert(vh, a + perp, c);
        AddVert(vh, b + perp, c);
        AddVert(vh, b - perp, c);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx);
    }

    private void DrawCircle(VertexHelper vh, Vector2 center, float radius, Color c) {
        int idx = vh.currentVertCount;
        AddVert(vh, center, c);

        for (int i = 0; i <= joinSegments; i++) {
            float angle = 2f * Mathf.PI * i / joinSegments;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            AddVert(vh, center + offset, c);
        }

        for (int i = 0; i < joinSegments; i++)
            vh.AddTriangle(idx, idx + i + 1, idx + i + 2);
    }

    private void AddVert(VertexHelper vh, Vector2 pos, Color c) {
        UIVertex v = UIVertex.simpleVert;
        v.color = c;
        v.position = new Vector3(pos.x, pos.y, 0);
        vh.AddVert(v);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>Replaces the snake's path with a copy of <paramref name="pts"/> and redraws.</summary>
    public void SetPoints(List<Vector2> pts) {
        Points = new List<Vector2>(pts);
        SetVerticesDirty();
    }

    /// <summary>Sets the grid points this snake currently occupies.</summary>
    public void SetOccupiedGridPoints(List<GridPoint> points) {
        OccupiedGridPoints = points;
    }

    /// <summary>Sets an arbitrary render color and derives the closest matching <see cref="SnakeColor"/>.</summary>
    public void SetColor(Color c) {
        color = c;
        SnakeColor = GetSnakeColor(color);
        _originalColor = c;
        SetVerticesDirty();
    }

    /// <summary>Sets the snake's color from a named <see cref="SnakeColor"/>.</summary>
    public void SetColor(SnakeColor snakeColor) {
        SnakeColor = snakeColor;

        color = snakeColor switch {
            SnakeColor.Red => Color.red,
            SnakeColor.Green => Color.green,
            SnakeColor.Blue => Color.blue,
            SnakeColor.Yellow => Color.yellow,
            SnakeColor.Cyan => Color.cyan,
            SnakeColor.Magenta => Color.magenta,
            SnakeColor.Orange => OrangeColor,
            SnakeColor.Purple => PurpleColor,
            SnakeColor.Brown => BrownColor,
            SnakeColor.Violet => VioletColor,
            _ => Color.white
        };

        _originalColor = color;
        SetVerticesDirty();
    }

    /// <summary>Reverse-maps a <see cref="Color"/> back to its <see cref="SnakeColor"/> by exact-value comparison.</summary>
    private SnakeColor GetSnakeColor(Color c) {
        if (c == Color.red) return SnakeColor.Red;
        if (c == Color.green) return SnakeColor.Green;
        if (c == Color.blue) return SnakeColor.Blue;
        if (c == Color.yellow) return SnakeColor.Yellow;
        if (c == Color.cyan) return SnakeColor.Cyan;
        if (c == Color.magenta) return SnakeColor.Magenta;
        if (c == PurpleColor) return SnakeColor.Purple;
        if (c == OrangeColor) return SnakeColor.Orange;
        if (c == VioletColor) return SnakeColor.Violet;
        if (c == BrownColor) return SnakeColor.Brown;

        Debug.LogWarning($"Unknown color selected: {c}");
        return SnakeColor.Red; // fallback
    }

    // ── Highlight ─────────────────────────────────────────────────────

    /// <summary>Briefly flashes the snake to <see cref="highlightColor"/>, then fades back to its normal color.</summary>
    public void StartHighlight() {
        if (_highlightCoroutine != null)
            StopCoroutine(_highlightCoroutine);

        _highlightCoroutine = StartCoroutine(HighlightFade());
    }

    private IEnumerator HighlightFade() {
        color = highlightColor;
        SetVerticesDirty();

        float elapsed = 0f;
        while (elapsed < highlightDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / highlightDuration;
            color = Color.Lerp(highlightColor, _originalColor, t);
            SetVerticesDirty();
            yield return null;
        }

        color = _originalColor;
        SetVerticesDirty();
    }

    private void OnDrawGizmos() {
        if (_headGridPoint == null)
            return;

        Gizmos.color = Color.red;

        Vector3 start = transform.TransformPoint(_debugHeadPos);
        Vector3 end = transform.TransformPoint(_debugHeadPos + _debugDir * 200f);

        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(start, 5f);
    }
}