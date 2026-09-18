using Godot;

public partial class PlayerHud : CanvasLayer
{
    private Label _timerLabel;
    private ProgressBar _hpBar;
    private Label _hpLabel;

    private Health _health;
    private double _elapsed = 0.0;

    public override void _Ready()
    {
        _timerLabel = GetNodeOrNull<Label>("TopBar/TimeLabel");
        _hpBar = GetNodeOrNull<ProgressBar>("BottomBar/Margin/HBox/HpBar");
        _hpLabel = GetNodeOrNull<Label>("BottomBar/Margin/HBox/HpLabel");

        ResetTimer();
        FindPlayerHealth();

        if (_timerLabel == null)
            GD.PushError("PlayerHud: TimeLabel не найден по пути TopBar/TimeLabel");
        if (_hpBar == null)
            GD.PushError("PlayerHud: HpBar не найден по пути BottomBar/Margin/HBox/HpBar");
        if (_hpLabel == null)
            GD.PushError("PlayerHud: HpLabel не найден по пути BottomBar/Margin/HBox/HpLabel");
    }

    public override void _ExitTree()
    {
        UnsubscribeHealth();
    }

    private void FindPlayerHealth()
    {
        var player = GetTree().GetFirstNodeInGroup("player");
        if (player is not Node playerNode)
            return;

        var health = playerNode.GetNodeOrNull<Health>("Health");
        if (health == null || !GodotObject.IsInstanceValid(health))
            return;

        if (_health == health)
            return;

        UnsubscribeHealth();
        _health = health;
        _health.HealthChanged += OnHealthChanged;
        _health.Died += OnDied;

        UpdateHp(_health.CurrentHP, _health.MaxHP);
    }

    private void UnsubscribeHealth()
    {
        if (_health != null && GodotObject.IsInstanceValid(_health))
        {
            _health.HealthChanged -= OnHealthChanged;
            _health.Died -= OnDied;
        }

        _health = null;
    }

    public override void _Process(double delta)
    {
        if (_health == null || !GodotObject.IsInstanceValid(_health))
        {
            FindPlayerHealth();
            return;
        }

        _elapsed += delta;

        if (_timerLabel != null)
            _timerLabel.Text = FormatTime(_elapsed);
    }

    private void OnHealthChanged(float currentHp, float maxHp)
    {
        UpdateHp(currentHp, maxHp);
    }

    private void OnDied()
    {
        if (_hpBar != null)
            _hpBar.Value = 0f;

        if (_hpLabel != null && _health != null)
            _hpLabel.Text = FormatHp(0f, _health.MaxHP);
    }

    private void UpdateHp(float currentHp, float maxHp)
    {
        if (_hpBar != null)
        {
            _hpBar.MaxValue = maxHp;
            _hpBar.Value = currentHp;
        }

        if (_hpLabel != null)
            _hpLabel.Text = FormatHp(currentHp, maxHp);
    }

    private static string FormatHp(float currentHp, float maxHp)
    {
        return $"{Mathf.RoundToInt(currentHp)} / {Mathf.RoundToInt(maxHp)}";
    }

    private static string FormatTime(double seconds)
    {
        int total = (int)seconds;
        int hours = total / 3600;
        int minutes = (total % 3600) / 60;
        int secs = total % 60;
        return $"{hours:00}:{minutes:00}:{secs:00}";
    }

    private void ResetTimer()
    {
        _elapsed = 0.0;

        if (_timerLabel != null)
            _timerLabel.Text = "00:00:00";
    }

    public void AddTime(double seconds)
    {
        if (seconds <= 0.0)
            return;

        _elapsed += seconds;

        if (_timerLabel != null)
            _timerLabel.Text = FormatTime(_elapsed);
    }
}