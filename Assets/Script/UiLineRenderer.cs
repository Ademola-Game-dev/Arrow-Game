using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : Graphic{
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
    [SerializeField] public Color highlightColor = Color.yellow;

    // Callback — SnakeCreator listens to this
    public System.Action<UILineRenderer> OnSnakeClicked;

    private Color _originalColor;


    protected override void Awake() {
        base.Awake();
        // Graphic requires a CanvasRenderer — Unity adds it but just in case:
        if (GetComponent<CanvasRenderer>() == null)
            gameObject.AddComponent<CanvasRenderer>();

        raycastTarget = true; // ← enables click detection on the mesh
        _originalColor = color;
    }

    protected override void OnPopulateMesh(VertexHelper vh) {
        vh.Clear();

        // ── Guard: need at least 2 DISTINCT points ────────────────────
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

    private void DrawHead(VertexHelper vh, Vector2 headPos, Vector2 nextPos) {
        float headRadius = lineWidth * headSize * 0.5f;

        Vector2 forward = (headPos - nextPos).normalized;

        // ── Safety: if forward is zero skip head entirely ─────────────
        if (forward == Vector2.zero) return;

        Vector2 right = new Vector2(-forward.y, forward.x);

        // ── Head circle ───────────────────────────────────────────────
        DrawCircle(vh, headPos, headRadius, headColor);

        // ── Snout bump ────────────────────────────────────────────────
        Vector2 snoutPos = headPos + forward * (headRadius * 0.5f);
        DrawCircle(vh, snoutPos, headRadius * 0.55f, headColor);

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

    private void DrawTongue(VertexHelper vh, Vector2 headPos,
                         Vector2 forward, Vector2 right, float headRadius) {
        Color tongueColor = new Color(0.9f, 0.1f, 0.2f);
        float tongueWidth = lineWidth * 0.12f;
        float stemLen = headRadius * tongueLength;        // ← uses your field
        float forkLen = stemLen * 0.4f;                // fork = 40% of stem
        float forkSpread = headRadius * 0.28f;

        Vector2 tongueBase = headPos + forward * headRadius * 0.85f;
        Vector2 tongueTip = tongueBase + forward * stemLen;

        // Main stem
        DrawThickLine(vh, tongueBase, tongueTip, tongueWidth, tongueColor);

        // Left fork
        DrawThickLine(vh, tongueTip,
                      tongueTip + forward * forkLen + right * forkSpread,
                      tongueWidth * 0.8f, tongueColor);

        // Right fork
        DrawThickLine(vh, tongueTip,
                      tongueTip + forward * forkLen - right * forkSpread,
                      tongueWidth * 0.8f, tongueColor);
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

    private void DrawThickLine(VertexHelper vh, Vector2 a, Vector2 b,
                               float width, Color c) {
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

    private void DrawCircle(VertexHelper vh, Vector2 center,
                            float radius, Color c) {
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

    // ── Vertex helper ─────────────────────────────────────────────────

    private void AddVert(VertexHelper vh, Vector2 pos, Color c) {
        UIVertex v = UIVertex.simpleVert;
        v.color = c;
        v.position = new Vector3(pos.x, pos.y, 0);
        vh.AddVert(v);
    }

    // ── Public API ────────────────────────────────────────────────────

    public void SetPoints(List<Vector2> pts) {
        Points = pts;
        SetVerticesDirty();
    }

    public void SetColor(Color c) {
        color = c;
        _originalColor = c;
        SetVerticesDirty();
    }

}