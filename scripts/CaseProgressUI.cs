using Godot;

public partial class CaseProgressUI : Control
{
    // Дальше этого расстояния (мировые единицы между игроком и кейсом)
    // панель уменьшается и тускнеет, чтобы не засорять экран.
    [Export] public float CompactDistance = 1100f;

    [Export(PropertyHint.Range, "0.1,1,0.05")] public float CompactScale = 0.8f;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float CompactAlpha = 0.55f;

    private Label _title;
    private Label _progress;
    private Label _ready;

    private Node2D _player;
    private float _currentScale = 1f;
    private float _currentAlpha = 1f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        _title = GetNodeOrNull<Label>("Panel/Margin/VBox/Title");
        _progress = GetNodeOrNull<Label>("Panel/Margin/VBox/Progress");
        _ready = GetNodeOrNull<Label>("Panel/Margin/VBox/Ready");

        // Масштаб вокруг центра панели (иначе она бы уменьшалась от левого верхнего угла).
        PivotOffset = Size * 0.5f;

        if (_title == null)
            GD.PushError("CaseProgressUI: Title не найден");
        if (_progress == null)
            GD.PushError("CaseProgressUI: Progress не найден");
        if (_ready == null)
            GD.PushError("CaseProgressUI: Ready не найден");
    }

    public void ShowProgress(int kills, int required, bool ready, bool locked)
    {
        Visible = true;

        if (_title != null)
            _title.Text = "КЕЙС";

        if (_progress != null)
            _progress.Text = $"Убито врагов: {kills} / {required}";

        if (_ready != null)
            _ready.Visible = ready;
    }

    public void ShowOpened()
    {
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        float d = (float)delta;

        ReflectPlayer();

        bool compact = IsCompact();

        float targetScale = compact ? CompactScale : 1f;
        float targetAlpha = compact ? CompactAlpha : 1f;

        _currentScale = Mathf.MoveToward(_currentScale, targetScale, 3f * d);
        _currentAlpha = Mathf.MoveToward(_currentAlpha, targetAlpha, 3f * d);

        Scale = new Vector2(_currentScale, _currentScale);

        Color c = Modulate;
        c.A = _currentAlpha;
        Modulate = c;
    }

    private void ReflectPlayer()
    {
        if (_player == null || !GodotObject.IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
    }

    private bool IsCompact()
    {
        if (_player == null)
            return false;

        float distance = _player.GlobalPosition.DistanceTo(GlobalPosition);
        return distance > CompactDistance;
    }
}