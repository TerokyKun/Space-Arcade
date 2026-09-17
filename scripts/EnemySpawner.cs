using Godot;

public partial class EnemySpawner : Node2D
{
    [Export] public PackedScene EnemyScene;
    [Export] public PackedScene AsteroidScene;
    [Export] public PackedScene HealPickupScene;

    [Export] public int MaxEnemies = 200;
    [Export] public int MaxAsteroids = 12;
    [Export] public int MaxPickups = 8;

    [Export] public float EnemySpawnInterval = 8f;
    [Export] public float AsteroidSpawnInterval = 7f;
    [Export] public float PickupSpawnInterval = 12f;

    [Export] public float EnemySpawnRadius = 500f;
    [Export] public float AsteroidSpawnRadius = 650f;
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

        EnemyScene ??= GD.Load<PackedScene>("res://scense/Enemy.tscn");
        AsteroidScene ??= GD.Load<PackedScene>("res://scense/Asteroid.tscn");
        HealPickupScene ??= GD.Load<PackedScene>("res://scense/HealPickup.tscn");

        if (EnemyScene == null)
            GD.PushError("❌ Enemy.tscn НЕ НАЙДЕН");

        if (AsteroidScene == null)
            GD.PushError("❌ Asteroid.tscn НЕ НАЙДЕН");

        if (HealPickupScene == null)
            GD.PushError("❌ HealPickup.tscn НЕ НАЙДЕН");

        _counter = GetTree().Root.GetNodeOrNull<Counter>("Game/Counter");
        if (_counter == null)
            GD.PushWarning("⚠ Counter не найден");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        _enemyTimer -= dt;
        _asteroidTimer -= dt;
        _pickupTimer -= dt;

        if (_enemyTimer <= 0f)
        {
            TrySpawnEnemy();
            _enemyTimer = EnemySpawnInterval;
        }

        if (_asteroidTimer <= 0f)
        {
            TrySpawnAsteroid();
            _asteroidTimer = AsteroidSpawnInterval;
        }

        if (_pickupTimer <= 0f)
        {
            TrySpawnHealPickup();
            _pickupTimer = PickupSpawnInterval;
        }
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

    private void TrySpawnEnemy()
    {
        var enemies = GetTree().GetNodesInGroup("enemy");
        if (enemies.Count >= MaxEnemies)
            return;

        if (EnemyScene == null)
            return;

        Vector2 playerPos = GetPlayerPosition();
        Vector2 spawnPos = GetSpawnPosition(playerPos, EnemySpawnRadius);

        var enemy = EnemyScene.Instantiate<Enemy>();
        GetTree().CurrentScene.AddChild(enemy);
        enemy.GlobalPosition = spawnPos;

        RegisterEnemy(enemy);
    }

    private void TrySpawnAsteroid()
    {
        var asteroids = GetTree().GetNodesInGroup("asteroid");
        if (asteroids.Count >= MaxAsteroids)
            return;

        if (AsteroidScene == null)
            return;

        Vector2 playerPos = GetPlayerPosition();
        Vector2 spawnPos = GetSpawnPosition(playerPos, AsteroidSpawnRadius);

        Vector2 toPlayer = (playerPos - spawnPos).Normalized();
        Vector2 side = new Vector2(-toPlayer.Y, toPlayer.X);
        float speed = _rng.RandfRange(AsteroidSpeedMin, AsteroidSpeedMax);

        Vector2 velocity = (toPlayer + side * _rng.RandfRange(-0.35f, 0.35f)).Normalized() * speed;
        float angularSpeed = _rng.RandfRange(-1.5f, 1.5f);

        var asteroid = AsteroidScene.Instantiate<Asteroid>();
        GetTree().CurrentScene.AddChild(asteroid);
        asteroid.Initialize(spawnPos, velocity, angularSpeed);
    }

    private void TrySpawnHealPickup()
    {
        var pickups = GetTree().GetNodesInGroup("pickup");
        if (pickups.Count >= MaxPickups)
            return;

        if (HealPickupScene == null)
            return;

        Vector2 playerPos = GetPlayerPosition();
        Vector2 spawnPos = GetSpawnPosition(playerPos, PickupSpawnRadius);

        float angle = _rng.RandfRange(0f, Mathf.Tau);
        Vector2 driftDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        var pickup = HealPickupScene.Instantiate<HealPickup>();
        GetTree().CurrentScene.AddChild(pickup);
        pickup.Initialize(spawnPos, driftDir);
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