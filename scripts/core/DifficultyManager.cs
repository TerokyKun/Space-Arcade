using Godot;

public partial class DifficultyManager : Node
{
    public static DifficultyManager Instance { get; private set; }

    [Export(PropertyHint.Range, "1,900,1")] public float BossInterval = 300f;

    [Export(PropertyHint.Range, "0,0.5,0.01")] public float HpPerMinute = 0.12f;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float DamagePerMinute = 0.10f;
    [Export(PropertyHint.Range, "0,0.2,0.01")] public float SpeedPerMinute = 0.03f;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float IntervalShrinkPerMinute = 0.03f;

    [Export(PropertyHint.Range, "0,0.5,0.01")] public float EliteChanceBase = 0.02f;
    [Export(PropertyHint.Range, "0,0.02,0.001")] public float EliteChancePerMinute = 0.008f;

    [Export(PropertyHint.Range, "0,30,1")] public int WaveMinutesPerExtra = 2;
    [Export(PropertyHint.Range, "0,100,1")] public int CaseKillsPerMinute = 1;

    public float RunTime { get; private set; }
    public bool BossDue => BossesExpected > 0;

    public float Minutes => RunTime / 60f;
    public float HpMultiplier => 1f + Mathf.Min(3f, Minutes * HpPerMinute);
    public float DamageMultiplier => 1f + Mathf.Min(2.5f, Minutes * DamagePerMinute);
    public float SpeedMultiplier => 1f + Mathf.Min(0.6f, Minutes * SpeedPerMinute);
    public float IntervalMultiplier => Mathf.Max(0.4f, 1f - Minutes * IntervalShrinkPerMinute);
    public float EliteChance => Mathf.Min(0.18f, EliteChanceBase + Minutes * EliteChancePerMinute);
    public int WaveBonus => Mathf.Min(8, (int)(Minutes / Mathf.Max(1, WaveMinutesPerExtra)));
    public int CaseKillBonus => Mathf.Min(60, (int)(Minutes * CaseKillsPerMinute));

    private int BossesExpected => Mathf.Max(0, (int)(RunTime / Mathf.Max(1f, BossInterval)));
    private int _bossesSpawned = 0;

    private EnemySpawner _spawner;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup("difficulty_manager");
        _spawner = GetTree().GetFirstNodeInGroup("enemy_spawner") as EnemySpawner;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void _Process(double delta)
    {
        if (GameManager.IsGameOver)
            return;

        if (!GetTree().Paused)
            RunTime += (float)delta;
        CheckBoss();
    }

    public void AddTime(float seconds)
    {
        if (seconds > 0f)
            RunTime += seconds;
        CheckBoss();
    }

    private void CheckBoss()
    {
        if (_bossesSpawned >= BossesExpected)
            return;

        while (_bossesSpawned < BossesExpected)
        {
            _bossesSpawned++;
            SpawnBossNow();
        }
    }

    private void SpawnBossNow()
    {
        if (_spawner == null)
            _spawner = GetTree().GetFirstNodeInGroup("enemy_spawner") as EnemySpawner;

        if (_spawner != null && GodotObject.IsInstanceValid(_spawner))
            _spawner.SpawnBossNow();
    }
}