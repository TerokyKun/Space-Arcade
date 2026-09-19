using Godot;

// Полоса опыта с тремя состояниями:
//  - серая база (по умолчанию);
//  - синяя заливка при получении XP (затухает к серой после IdleTime простоя);
//  - золотое мягкое мерцание, когда доступен апгрейд.
// Внутренний XP не сбрасывается — меняется только отображение.
public partial class XpBar : Control
{
    [Export] public Color BaseColor = new Color(0.15f, 0.16f, 0.18f, 1f);
    [Export] public Color FillColor = new Color(0.22f, 0.55f, 1f, 1f);
    [Export] public Color GoldColor = new Color(1f, 0.83f, 0.35f, 1f);

    // Секунды без нового опыта, после которых заливка возвращается к серой базе.
    [Export(PropertyHint.Range, "0,5,0.1")] public float IdleTime = 1.5f;
    // Длительность плавного затухания к серой базе.
    [Export(PropertyHint.Range, "0.1,3,0.1")] public float FadeTime = 1.0f;

    private float _targetProgress = 0f;
    private float _displayProgress = 0f;
    private float _idleTimer = 0f;
    private bool _upgradeAvailable = false;
    private float _shimmer = 0f;

    public void SetProgress(float value, float max)
    {
        _targetProgress = max > 0f ? Mathf.Clamp(value / max, 0f, 1f) : 0f;
        _idleTimer = IdleTime;
        _displayProgress = Mathf.Max(_displayProgress, _targetProgress);
        QueueRedraw();
    }

    public void SetUpgradeAvailable(bool available)
    {
        _upgradeAvailable = available;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        bool changed = false;

        _shimmer += d;

        if (_idleTimer > 0f)
        {
            _idleTimer -= d;
            if (_idleTimer <= 0f)
                changed = true;
        }
        else if (_displayProgress > 0f)
        {
            _displayProgress = Mathf.MoveToward(_displayProgress, 0f, d / Mathf.Max(0.05f, FadeTime));
            changed = true;
        }

        if (Mathf.Abs(_displayProgress - _targetProgress) > 0.001f && _displayProgress < _targetProgress)
        {
            _displayProgress = _targetProgress;
            changed = true;
        }

        if (changed)
            QueueRedraw();
    }

    public override void _Draw()
    {
        Rect2 box = new Rect2(Vector2.Zero, Size);
        if (box.Size.X <= 2f || box.Size.Y <= 2f)
            return;

        // 1) Серая база.
        DrawRect(box, BaseColor);

        // 2) Синяя заливка прогресса.
        float fillW = box.Size.X * _displayProgress;
        if (fillW > 1f)
            DrawRect(new Rect2(0f, 0f, fillW, box.Size.Y), FillColor);

        // 3) Золотое мягкое мерцание при доступном апгрейде.
        if (_upgradeAvailable)
        {
            float breathe = 0.55f + 0.25f * Mathf.Sin(_shimmer * 2.2f);
            DrawRect(box, new Color(GoldColor, 0.10f * breathe));

            float sweep = Mathf.PosMod(_shimmer * 0.45f, 1.15f);
            if (sweep < 1f)
            {
                float x = box.Size.X * sweep;
                DrawRect(new Rect2(Mathf.Max(0f, x - 14f), 0f, 30f, box.Size.Y), new Color(1f, 0.92f, 0.62f, 0.4f * breathe));
            }
        }

        DrawRect(box, new Color(1f, 1f, 1f, 0.12f), false, 1f);
    }
}