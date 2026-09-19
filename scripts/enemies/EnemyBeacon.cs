using Godot;

public partial class EnemyBeacon : Control
{
    [Export] public Texture2D EnemyIcon;
    [Export] public Texture2D BossIcon;

    // Минимальный размер/прозрачность, когда враг почти вошёл в кадр.
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float MinScale = 0.5f;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float MinAlpha = 0.35f;

    // Лёгкая пульсация размера, чтобы маркер читался на любом фоне.
    [Export(PropertyHint.Range, "0,0.2,0.01")] public float PulseAmount = 0.06f;
    [Export(PropertyHint.Range, "0.5,6,0.1")] public float PulseSpeed = 3f;

    private TextureRect _icon;
    private Label _count;

    private float _targetProgress = 1f;
    private float _targetBaseScale = 1f;
    private float _currentScale = 1f;
    private float _currentAlpha = 0f;
    private float _pulseT = 0f;
    private bool _hidden = true;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _icon = GetNodeOrNull<TextureRect>("Icon");
        _count = GetNodeOrNull<Label>("Count");

        Visible = false;
        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    // progress: 1 — враг далеко за кадром (максимальный размер и непрозрачность),
    //           0 — враг у границы экрана (маленький, полупрозрачный).
    // Угол задаётся в координатах экрана, маркер ставится по ЦЕНТРУ в точку screenPos.
    public void ShowAt(Vector2 screenPos, float angleRad, int count, bool boss, float progress)
    {
        GlobalPosition = screenPos - PivotOffset;
        // Иконка смотрит "вверх"; чтобы указать на врага, доворачиваем её на 90°.
        Rotation = angleRad + Mathf.Pi / 2f;

        _targetProgress = Mathf.Clamp(progress, 0f, 1f);
        _targetBaseScale = ScaleForCount(count, boss);
        _hidden = false;
        Visible = true;

        if (_icon != null)
            _icon.Texture = boss ? BossIcon : EnemyIcon;

        if (_count != null)
        {
            _count.Text = count.ToString();
            _count.Visible = count >= 2;
        }
    }

    public void HideBeacon()
    {
        _hidden = true;
    }

    private static float ScaleForCount(int count, bool boss)
    {
        float scale = count >= 10 ? 1.0f : count >= 5 ? 0.9f : count >= 3 ? 0.78f : 0.65f;
        return boss ? scale * 1.25f : scale;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;

        if (_hidden)
        {
            // Плавное исчезновение после того, как враг вошёл в кадр / погиб.
            _currentAlpha = Mathf.MoveToward(_currentAlpha, 0f, 8f * d);
            UpdateAlpha();

            if (_currentAlpha <= 0.005f)
                Visible = false;
            return;
        }

        _pulseT += d;

        // Размер: базовый (от количества врагов) × усадка у границы × лёгкая пульсация.
        float baseSize = _targetBaseScale * Mathf.Lerp(MinScale, 1f, _targetProgress);
        float pulse = 1f + PulseAmount * Mathf.Sin(_pulseT * PulseSpeed);
        float targetScale = baseSize * pulse;

        _currentScale = Mathf.MoveToward(_currentScale, targetScale, 4f * d);
        Scale = new Vector2(_currentScale, _currentScale);

        // Прозрачность: уменьшается по мере приближения врага к кадру.
        float targetAlpha = Mathf.Lerp(MinAlpha, 1f, _targetProgress);
        _currentAlpha = Mathf.MoveToward(_currentAlpha, targetAlpha, 4f * d);
        UpdateAlpha();

        if (_currentAlpha <= 0.01f)
            Visible = false;

        // Счётчик всегда держим вертикально, чтобы читался при любой ориентации стрелки.
        if (_count != null && _count.Visible)
        {
            _count.PivotOffset = _count.Size * 0.5f;
            _count.Rotation = -Rotation;
        }
    }

    private void UpdateAlpha()
    {
        Color c = Modulate;
        c.A = Mathf.Clamp(_currentAlpha, 0f, 1f);
        Modulate = c;
    }
}