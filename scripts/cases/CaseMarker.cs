using Godot;

// Направленный маркер кейса: появляется, когда кейс за кадром (~1.5 экрана),
// скрывается, когда кейс в видимой области камеры, и гасится с удалением.
// Рисуется как радарные дуги (три дуги + точка направления).
public partial class CaseMarker : Control
{
    [Export(PropertyHint.Range, "0.5,4,0.1")] public float ActivateRangeScreens = 1.5f;

    // На каком расстоянии за краем экрана маркер полностью прозрачен.
    [Export(PropertyHint.Range, "100,2000,50")] public float FadeRangePx = 400f;

    [Export] public Color ArcColor = new Color(0.3f, 0.85f, 1f);
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float MinAlpha = 0.25f;

    private const float UpdateInterval = 1f / 15f; // ~15 Гц
    private float _accumulator = 0f;
    private float _pulseT = 0f;

    private bool _shown = false;
    private float _currentAlpha = 0f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Size = new Vector2(120f, 120f);
        PivotOffset = Size * 0.5f;
        Visible = false;
        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        _pulseT += d;
        _accumulator += d;

        if (_accumulator < UpdateInterval)
            return;

        _accumulator = 0f;

        Node2D caseZone = GetTree().GetFirstNodeInGroup("case_zone") as Node2D;
        if (caseZone == null || caseZone.IsQueuedForDeletion())
        {
            HideMarker();
            return;
        }

        Rect2 visiblePx = GetViewport().GetVisibleRect();
        if (visiblePx.Size.X <= 1f || visiblePx.Size.Y <= 1f)
            return;

        // Мировая позиция кейса → координаты экрана (с учётом камеры).
        Vector2 casePx = caseZone.GetGlobalTransformWithCanvas().Origin;
        float edgeDist = SignedDistanceToRect(visiblePx, casePx);

        // Кейс в кадре — маркер не нужен.
        if (edgeDist <= 0f)
        {
            HideMarker();
            return;
        }

        // За пределами ~1.5 экрана маркер гасится.
        float maxDimPx = Mathf.Max(visiblePx.Size.X, visiblePx.Size.Y);
        if (casePx.DistanceTo(visiblePx.GetCenter()) > maxDimPx * ActivateRangeScreens)
        {
            HideMarker();
            return;
        }

        ShowAt(visiblePx, casePx, edgeDist);
    }

    private void ShowAt(Rect2 visiblePx, Vector2 casePx, float edgeDist)
    {
        Vector2 centerPx = visiblePx.GetCenter();
        Vector2 diff = casePx - centerPx;
        float length = diff.Length();
        if (length < 0.001f)
        {
            HideMarker();
            return;
        }

        Vector2 unit = diff / length;
        float angle = Mathf.Atan2(diff.Y, diff.X);

        float halfW = visiblePx.Size.X * 0.5f;
        float halfH = visiblePx.Size.Y * 0.5f;
        const float inset = 60f;
        float availW = Mathf.Max(1f, halfW - inset);
        float availH = Mathf.Max(1f, halfH - inset);

        float sx = availW / Mathf.Abs(unit.X);
        float sy = availH / Mathf.Abs(unit.Y);
        Vector2 posPx = centerPx + unit * Mathf.Min(sx, sy);

        GlobalPosition = posPx - PivotOffset;
        Rotation = angle;

        // Прозрачность: от полной (кейс у края экрана) до MinAlpha (кейс далеко).
        float progress = Mathf.Clamp(edgeDist / Mathf.Max(1f, FadeRangePx), 0f, 1f);
        float targetAlpha = Mathf.Lerp(1f, MinAlpha, progress);

        _shown = true;
        Visible = true;
        _currentAlpha = Mathf.MoveToward(_currentAlpha, targetAlpha, 3f * 0.066f);
        UpdateAlpha();
        QueueRedraw();
    }

    private void HideMarker()
    {
        if (!_shown && _currentAlpha <= 0.01f)
            return;

        _shown = false;
        _currentAlpha = Mathf.MoveToward(_currentAlpha, 0f, 3f * 0.066f);
        UpdateAlpha();

        if (_currentAlpha <= 0.01f)
            Visible = false;
    }

    private void UpdateAlpha()
    {
        Color c = Modulate;
        c.A = Mathf.Clamp(_currentAlpha, 0f, 1f);
        Modulate = c;
    }

    public override void _Draw()
    {
        if (!_shown)
            return;

        Vector2 center = Size * 0.5f;
        float pulse = 0.85f + 0.15f * Mathf.Sin(_pulseT * 3f);

        float[] radii = { 20f * pulse, 34f * pulse, 48f * pulse };
        float[] widths = { 3.5f, 2.5f, 1.5f };

        // Дуги рисуются в локальных координатах; Control доворачивается на
        // направление к кейсу, поэтому "смотрит" по +X (0°).
        for (int i = 0; i < radii.Length; i++)
        {
            Color c = ArcColor;
            c.A = 0.9f - i * 0.25f;
            DrawArc(center, radii[i], -1.05f, 1.05f, 24, c, widths[i], true);
        }

        // Точка направления на оси дуг.
        Color dot = ArcColor;
        dot.A = 1f;
        DrawCircle(center + new Vector2(radii[0] + 6f, 0f), 4f, dot);
    }

    private static float SignedDistanceToRect(Rect2 rect, Vector2 p)
    {
        float dx = Mathf.Max(0f,
            Mathf.Max(rect.Position.X - p.X, p.X - (rect.Position.X + rect.Size.X)));
        float dy = Mathf.Max(0f,
            Mathf.Max(rect.Position.Y - p.Y, p.Y - (rect.Position.Y + rect.Size.Y)));

        if (dx > 0f || dy > 0f)
            return new Vector2(dx, dy).Length();

        float inX = Mathf.Min(p.X - rect.Position.X,
            rect.Position.X + rect.Size.X - p.X);
        float inY = Mathf.Min(p.Y - rect.Position.Y,
            rect.Position.Y + rect.Size.Y - p.Y);
        return -Mathf.Min(inX, inY);
    }
}