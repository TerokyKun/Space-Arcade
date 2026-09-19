using Godot;

public partial class CaseManager : Node2D
{
    [Export] public PackedScene CaseZoneScene;

    // Формула стоимости: required_kills = BaseKills + CaseIndex * KillsGrowth
    // (20 → 30 → 40 → ...). Всё настраивается в инспекторе.
    [Export] public int BaseKills = 20;
    [Export] public int KillsGrowth = 10;

    [Export(PropertyHint.Range, "10,3000,10")]
    public float SpawnMinDistance = 430f;

    [Export(PropertyHint.Range, "10,3000,10")]
    public float SpawnMaxDistance = 600f;

    private Counter _counter;
    private CaseZone _currentCase;
    private int _caseIndex = 0;
    private int _caseKills = 0;

    private bool _spawnPending = false;

    public override void _Ready()
    {
        CaseZoneScene ??= GD.Load<PackedScene>("res://scenes/cases/CaseZone.tscn");

        _counter = GetTree().Root.GetNodeOrNull<Counter>("Game/Counter");
        if (_counter == null)
            GD.PushWarning("CaseManager: Counter не найден по пути Game/Counter");
        else
            _counter.EnemyKilled += OnEnemyKilled;

        if (CaseZoneScene == null)
            GD.PushError("CaseManager: CaseZone.tscn не найден");

        SpawnNextCase();
    }

    public override void _ExitTree()
    {
        if (_counter != null)
            _counter.EnemyKilled -= OnEnemyKilled;
    }

    public override void _Process(double delta)
    {
        // Спавн следующего кейса откладывается, чтобы не менять дерево
        // прямо в колбэке сигнала смерти (физический flush).
        if (_spawnPending)
        {
            _spawnPending = false;
            SpawnNextCase();
        }
    }

    // Единое событие смерти реального врага (из Counter): камни, хилки,
    // компаньоны и снаряды здесь не появляются. Каждый враг = ровно +1.
    private void OnEnemyKilled()
    {
        if (_currentCase == null)
            return;

        _caseKills += 1;
        _currentCase.SetKillProgress(_caseKills);
    }

    private void OnCaseOpened(CaseZone zone)
    {
        if (zone != _currentCase)
            return;

        _caseIndex += 1;
        _caseKills = 0;
        _currentCase = null;

        // Освобождаем саму зону отложенно (открытие случилось в физик-колбэке).
        if (GodotObject.IsInstanceValid(zone))
            zone.CallDeferred(Node.MethodName.QueueFree);

        _spawnPending = true;
    }

    private void SpawnNextCase()
    {
        if (CaseZoneScene == null)
            return;

        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        Vector2 center = player != null && GodotObject.IsInstanceValid(player)
            ? player.GlobalPosition
            : Vector2.Zero;

        float angle = (float)GD.RandRange(0.0, Mathf.Tau);
        float distance = (float)GD.RandRange(SpawnMinDistance, SpawnMaxDistance);
        Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

        var zone = CaseZoneScene.Instantiate<CaseZone>();
        Node parent = GetTree().CurrentScene ?? GetTree().Root;
        parent.AddChild(zone);
        zone.GlobalPosition = position;

        int requiredKills = BaseKills + _caseIndex * KillsGrowth;
        if (DifficultyManager.Instance != null)
            requiredKills += DifficultyManager.Instance.CaseKillBonus;
        zone.Configure(_caseIndex, requiredKills);
        zone.Opened += OnCaseOpened;

        _currentCase = zone;
        _caseKills = 0;
    }

    public void DebugAddKills(int kills)
    {
        if (kills <= 0)
            return;

        _caseKills += kills;
        _currentCase?.SetKillProgress(_caseKills);
    }

    public void DebugRespawnCase()
    {
        _caseIndex = 0;
        _caseKills = 0;

        if (_currentCase != null && GodotObject.IsInstanceValid(_currentCase))
            _currentCase.CallDeferred(Node.MethodName.QueueFree);

        _currentCase = null;
        SpawnNextCase();
    }
}