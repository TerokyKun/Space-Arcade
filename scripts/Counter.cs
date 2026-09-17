using Godot;

public partial class Counter : CanvasLayer
{
    private Label _scoreLabel;
    private int _score = 0;

    private float _timer = 0f;
    private const float TickTime = 1.5f;

    public override void _Ready()
    {
        // Не Always. Иначе будет работать на паузе.
        ProcessMode = ProcessModeEnum.Inherit;

        _scoreLabel = GetNodeOrNull<Label>("ScoreLabel");

        if (_scoreLabel == null)
        {
            GD.PushError("Counter: ScoreLabel не найден");
            return;
        }

        UpdateUI();
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;

        _timer += d;

        if (_timer >= TickTime)
        {
            _timer = 0f;
            AddScore(1);
        }
    }

    public void RegisterEnemy(Health health)
    {
        if (health == null)
            return;

        if (health.Team == Health.TeamType.Enemy)
        {
            health.Died += OnEnemyDied;
        }
    }

    private void OnEnemyDied()
    {
        AddScore(25);
    }

    public void AddScore(int amount)
    {
        _score += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        _scoreLabel.Text = $"Score: {_score}";
    }
}