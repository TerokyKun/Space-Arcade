using Godot;

public partial class EnemySpawner : Node2D
{
    [Export] public PackedScene EnemyScene;
    [Export] public PackedScene AsteroidScene;
    [Export] public PackedScene HealPickupScene;
    [Export] public PackedScene DropPickupScene;

    [Export] public int MaxEnemies = 200;
    [Export] public int MaxAsteroids = 12;
    [Export] public int MaxPickups = 8;

    [Export] public float EnemySpawnInterval = 8f;
    [Export] public float AsteroidSpawnInterval = 6f;
    [Export] public float PickupSpawnInterval = 12f;

    [Export] public float EnemySpawnRadius = 500f;
    // Базовая дистанция астероидов: реальный спавн — кольцо вокруг игрока
    // с внутренним радиусом Не меньше радиуса обзора камеры + запас.
    [Export] public float AsteroidSpawnRadius = 900f;
    [Export] public float AsteroidOuterFactor = 1.7f;
    [Export] public float CameraMarginFactor = 1.25f;
    [Export] public float PickupSpawnRadius = 550f;

    [Export] public float AsteroidSpeedMin = 120f;
    [Export] public float AsteroidSpeedMax = 220f;

    private float _enemyTimer = 0f;
    private float _asteroidTimer = 0f;
    private float _pickupTimer = 0f;

    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private Counter _counter;

    public override void _Ready()
    {
        _rng.Randomize();
        AddToGroup("enemy_spawner");

        EnemyScene ??= GD.Load<PackedScene>("res://scenes/gameplay/Enemy.tscn");
        AsteroidScene ??= GD.Load<PackedScene>("res://scenes/gameplay/Asteroid.tscn");
        HealPickupScene ??= GD.Load<PackedScene>("res://scenes/gameplay/HealPickup.tscn");
        DropPickupScene ??= GD.Load<PackedScene>("res://scenes/gameplay/DropPickup.tscn");

        if (EnemyScene == null)
            GD.PushError("❌ Enemy.tscn НЕ НАЙДЕН");

        if (AsteroidScene == null)
            GD.PushError("❌ Asteroid.tscn НЕ НАЙДЕН");

        if (HealPickupScene == null)
            GD.PushError("❌ HealPickup.tscn НЕ НАЙДЕН");

        if (DropPickupScene == null)
            GD.PushError("❌ DropPickup.tscn НЕ НАЙДЕН");

        _counter = GetTree().Root.GetNodeOrNull<Counter>("Game/Counter");
        if (_counter == null)
            GD.PushWarning("⚠ Counter не найден");
    }

    public override void _Process(double delta)
    {
        if (GameManager.IsGameOver)
            return;

        float dt = (float)delta;
        var dm = DifficultyManager.Instance;

        float enemyInterval = EnemySpawnInterval * (dm?.IntervalMultiplier ?? 1f);

        _enemyTimer -= dt;
        _asteroidTimer -= dt;
        _pickupTimer -= dt;

        if (_enemyTimer <= 0f)
        {
            SpawnWave();
            _enemyTimer = enemyInterval;
        }

        if (_asteroidTimer <= 0f)
        {
            float minutes = dm?.Minutes ?? 0f;

            // Рампа плотности: в первые минуты редко, в конце — всё чаще (пачками).
            _asteroidTimer = Mathf.Max(1.2f, AsteroidSpawnInterval * (dm?.IntervalMultiplier ?? 1f));
            if (minutes < 1f)
                _asteroidTimer = Mathf.Max(_asteroidTimer, AsteroidSpawnInterval);

            int burst = 1;
            if (minutes >= 3f && _rng.Randf() < 0.25f)
                burst += 1;
            if (minutes >= 7f && _rng.Randf() < 0.4f)
                burst += 1;

            for (int i = 0; i < burst; i++)
                SpawnAsteroid();
        }

        if (_pickupTimer <= 0f)
        {
            SpawnHealPickup();
            _pickupTimer = PickupSpawnInterval;
        }
    }

    private void SpawnWave()
    {
        var dm = DifficultyManager.Instance;
        int count = 1 + (dm?.WaveBonus ?? 0);
        SpawnEnemies(count);
    }

    private Vector2 GetPlayerPosition()
    {
        Vector2 playerPos = Vector2.Zero;

        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0 && players[0] is Node2D player)
            playerPos = player.GlobalPosition;

        return playerPos;
    }

    private Vector2 GetSpawnPosition(Vector2 center, float radius)
    {
        float angle = _rng.RandfRange(0f, Mathf.Tau);
        float distance = _rng.RandfRange(radius * 0.85f, radius * 1.15f);
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return center + dir * distance;
    }

    // Точка в кольце, гарантированно за пределами видимой области игрока.
    private Vector2 GetAsteroidSpawnPosition(Vector2 center)
    {
        float inner = Mathf.Max(AsteroidSpawnRadius, GetVisibleHalfDiagonal() * CameraMarginFactor);
        float outer = inner * AsteroidOuterFactor;

        float angle = _rng.RandfRange(0f, Mathf.Tau);
        float distance = _rng.RandfRange(inner, outer);
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return center + dir * distance;
    }

    // Полудиагональ видимой области в мировых единицах (с учётом зума камеры).
    private float GetVisibleHalfDiagonal()
    {
        Vector2 visible = GetViewport().GetVisibleRect().Size;
        float halfDiag = visible.Length() * 0.5f;

        Camera2D camera = GetViewport().GetCamera2D();
        if (camera != null)
            halfDiag /= Mathf.Max(0.01f, camera.Zoom.X);

        return halfDiag;
    }

    public void SpawnEnemies(int count, bool force = false)
    {
        for (int i = 0; i < count; i++)
            SpawnEnemy(force);
    }

    public bool SpawnEnemy(bool force = false)
    {
        return SpawnEnemyWithConfig(PickEnemyConfig(), force);
    }

    public bool SpawnKind(EnemyKind kind, bool force = true)
    {
        return SpawnEnemyWithConfig(EnemyCatalog.Load(kind), force);
    }

    public bool SpawnBossNow(bool force = true)
    {
        bool spawned = SpawnEnemyWithConfig(EnemyCatalog.Load(EnemyKind.Boss), force);
        if (spawned)
            GD.Print($"👑 Босс появился (время: {DifficultyManager.Instance?.RunTime ?? 0f:0}с)");
        return spawned;
    }

    private bool SpawnEnemyWithConfig(EnemyConfig config, bool force = false)
    {
        if (EnemyScene == null)
            return false;

        if (!force)
        {
            var enemies = GetTree().GetNodesInGroup("enemy");
            if (enemies.Count >= MaxEnemies)
                return false;
        }

        Vector2 playerPos = GetPlayerPosition();
        Vector2 spawnPos = GetSpawnPosition(playerPos, EnemySpawnRadius);

        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.Config = config;
        GetTree().CurrentScene.AddChild(enemy);
        enemy.GlobalPosition = spawnPos;

        RegisterEnemy(enemy);
        return true;
    }

    private EnemyConfig PickEnemyConfig()
    {
        var dm = DifficultyManager.Instance;
        float eliteChance = dm?.EliteChance ?? 0.02f;
        float minutes = dm?.Minutes ?? 0f;

        float roll = _rng.Randf();
        if (roll < eliteChance)
            return EnemyCatalog.Load(EnemyKind.Elite);

        // Поэтапное введение типов по времени:
        // 0-2 мин  — маленькие (Fast) + немного средних (Normal);
        // 2-5 мин  — больше средних + дальнобойщики (Ranged);
        // 5+ мин   — появляются танки (Tank);
        // боссы — только по отдельному таймеру DifficultyManager.
        var kinds = new System.Collections.Generic.List<EnemyKind>();

        if (minutes < 2f)
        {
            kinds.Add(EnemyKind.Fast);
            kinds.Add(EnemyKind.Fast);
            kinds.Add(EnemyKind.Normal);
        }
        else if (minutes < 5f)
        {
            kinds.Add(EnemyKind.Fast);
            kinds.Add(EnemyKind.Normal);
            kinds.Add(EnemyKind.Normal);
            kinds.Add(EnemyKind.Ranged);
        }
        else
        {
            kinds.Add(EnemyKind.Fast);
            kinds.Add(EnemyKind.Normal);
            kinds.Add(EnemyKind.Normal);
            kinds.Add(EnemyKind.Ranged);
            kinds.Add(EnemyKind.Tank);
        }

        EnemyKind kind = kinds[_rng.RandiRange(0, kinds.Count - 1)];
        return EnemyCatalog.Load(kind);
    }

    public bool SpawnAsteroid(bool force = false)
    {
        if (AsteroidScene == null)
            return false;

        if (!force)
        {
            var asteroids = GetTree().GetNodesInGroup("asteroid");
            if (asteroids.Count >= MaxAsteroids)
                return false;
        }

        Vector2 playerPos = GetPlayerPosition();
        Vector2 spawnPos = GetAsteroidSpawnPosition(playerPos);

        Vector2 toPlayer = (playerPos - spawnPos).Normalized();
        Vector2 side = new Vector2(-toPlayer.Y, toPlayer.X);
        float speed = _rng.RandfRange(AsteroidSpeedMin, AsteroidSpeedMax);

        Vector2 velocity = (toPlayer + side * _rng.RandfRange(-0.35f, 0.35f)).Normalized() * speed;
        float angularSpeed = _rng.RandfRange(-1.5f, 1.5f);

        var asteroid = AsteroidScene.Instantiate<Asteroid>();
        GetTree().CurrentScene.AddChild(asteroid);
        asteroid.Initialize(spawnPos, velocity, angularSpeed);
        return true;
    }

    public bool SpawnHealPickup(bool force = false)
    {
        if (HealPickupScene == null)
            return false;

        if (!force)
        {
            var pickups = GetTree().GetNodesInGroup("heal_pickup");
            if (pickups.Count >= MaxPickups)
                return false;
        }

        Vector2 playerPos = GetPlayerPosition();
        Vector2 spawnPos = GetSpawnPosition(playerPos, PickupSpawnRadius);

        float angle = _rng.RandfRange(0f, Mathf.Tau);
        Vector2 driftDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        var pickup = HealPickupScene.Instantiate<HealPickup>();
        GetTree().CurrentScene.AddChild(pickup);
        pickup.Initialize(spawnPos, driftDir);
        return true;
    }

    public bool SpawnDropPickup()
    {
        if (DropPickupScene == null)
            return false;

        Vector2 playerPos = GetPlayerPosition();
        Vector2 spawnPos = GetSpawnPosition(playerPos, PickupSpawnRadius);

        float angle = _rng.RandfRange(0f, Mathf.Tau);
        Vector2 driftDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        var drop = DropPickupScene.Instantiate<DropPickup>();
        GetTree().CurrentScene.AddChild(drop);
        drop.Initialize(spawnPos, driftDir);
        return true;
    }

    private void RegisterEnemy(Enemy enemy)
    {
        if (_counter == null || enemy == null)
            return;

        var health = enemy.GetNodeOrNull<Health>("Health");
        if (health == null)
        {
            GD.PushWarning("Spawner: Health не найден");
            return;
        }

        _counter.RegisterEnemy(health);
    }
}