using Godot;

public partial class SpaceObjectSpawner : Node2D
{
    [Export] public NodePath FollowTargetPath { get; set; }
    [Export] public PackedScene AsteroidScene { get; set; }
    [Export] public PackedScene HealScene { get; set; }
    [Export(PropertyHint.Range, "0,1,0.01")] public float HealChance = 0.18f;

    [Export] public int MaxObjects = 12;
    [Export] public float SpawnInterval = 2.5f;
    [Export] public float MinSpawnDistance = 450f;
    [Export] public float MaxSpawnDistance = 950f;

    private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
    private Node2D _followTarget;
    private Timer _spawnTimer;

    public override void _Ready()
    {
        _rng.Randomize();
        ResolveTarget();

        GD.Print("SpaceObjectSpawner готов. Следим за игроком: " + (_followTarget != null));

        // Создаем таймер
        _spawnTimer = new Timer();
        _spawnTimer.WaitTime = SpawnInterval;
        _spawnTimer.OneShot = false;
        _spawnTimer.Autostart = true;
        AddChild(_spawnTimer);

        _spawnTimer.Timeout += OnSpawnTimeout;
    }

    private void OnSpawnTimeout()
    {
        TrySpawn();
    }

    private void ResolveTarget()
    {
        _followTarget = null;

        if (FollowTargetPath != null && !FollowTargetPath.IsEmpty)
        {
            _followTarget = GetNodeOrNull<Node2D>(FollowTargetPath);
            if (_followTarget != null)
                return;
        }

        foreach (Node node in GetTree().GetNodesInGroup("player"))
        {
            if (node is Node2D n2d)
            {
                _followTarget = n2d;
                GD.Print("SpaceObjectSpawner привязался к игроку: " + n2d.Name);
                return;
            }
        }
    }

    private Vector2 GetCenter()
    {
        if (_followTarget != null && GodotObject.IsInstanceValid(_followTarget))
            return _followTarget.GlobalPosition;

        return GlobalPosition;
    }

    private void TrySpawn()
    {
        int alive = GetTree().GetNodesInGroup("space_object").Count;
        GD.Print($"Сейчас объектов: {alive} / {MaxObjects}");

        if (alive >= MaxObjects)
            return;

        SpawnRandomObject();
    }

    private void SpawnRandomObject()
    {
        PackedScene chosenScene = ChooseScene();
        if (chosenScene == null)
            return;

        Vector2 center = GetCenter();
        float angle = _rng.RandfRange(0f, Mathf.Tau);
        float distance = _rng.RandfRange(MinSpawnDistance, MaxSpawnDistance);
        Vector2 spawnPos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

        Node instance = chosenScene.Instantiate();
        Node parent = GetTree().CurrentScene ?? GetTree().Root;
        parent.AddChild(instance);

        // Универсальная установка позиции для Node2D или Area2D
        if (instance is Node2D node2D)
        {
            node2D.GlobalPosition = spawnPos;
            node2D.Rotation = _rng.RandfRange(0f, Mathf.Tau);
            node2D.AddToGroup("space_object");
        }
        else if (instance is Area2D area)
        {
            area.GlobalPosition = spawnPos;
            area.Rotation = _rng.RandfRange(0f, Mathf.Tau);
            area.AddToGroup("space_object");
        }
        else
        {
            GD.Print("❌ Спавн объекта: не Node2D и не Area2D!");
        }

        GD.Print($"✅ Спавн объекта: {instance.Name} на {spawnPos}");
    }

    private PackedScene ChooseScene()
    {
        bool wantHeal = _rng.Randf() <= HealChance;

        if (wantHeal && HealScene != null)
        {
            GD.Print("Выбрана хилка для спавна");
            return HealScene;
        }

        if (AsteroidScene != null)
        {
            GD.Print("Выбран астероид для спавна");
            return AsteroidScene;
        }

        if (HealScene != null)
            return HealScene;

        return null;
    }
}