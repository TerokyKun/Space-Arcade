using Godot;

public partial class HealPickup : Area2D
{
    [Export] public float HealAmount = 25f;

    [Export] public float DriftSpeed = 18f;
    [Export] public float FloatAmplitude = 8f;
    [Export] public float FloatFrequency = 2.0f;
    [Export] public float RotateSpeed = 1.2f;
    [Export] public float Lifetime = 18f;

    private float _time = 0f;
    private Vector2 _basePosition;
    private Vector2 _driftVelocity = Vector2.Zero;
    private float _lifetimeTimer = 0f;
    private bool _initialized = false;

    public override void _Ready()
    {
        AddToGroup("space_object");
        AddToGroup("pickup");

        BodyEntered += OnBodyEntered;

        if (!_initialized)
        {
            _basePosition = GlobalPosition;
            _lifetimeTimer = Lifetime;
            _initialized = true;
        }
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public void Initialize(Vector2 spawnPosition, Vector2 driftDirection)
    {
        GlobalPosition = spawnPosition;
        _basePosition = spawnPosition;
        _driftVelocity = driftDirection.Normalized() * DriftSpeed;
        _lifetimeTimer = Lifetime;
        _time = 0f;
        _initialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized)
            return;

        float d = (float)delta;

        _time += d;
        _basePosition += _driftVelocity * d;

        Vector2 wobble = new Vector2(0f, Mathf.Sin(_time * FloatFrequency) * FloatAmplitude);
        GlobalPosition = _basePosition + wobble;

        Rotation += RotateSpeed * d;

        _lifetimeTimer -= d;
        if (_lifetimeTimer <= 0f)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body == null || !body.IsInGroup("player"))
            return;

        Health health = FindHealth(body);
        if (health == null || health.Team != Health.TeamType.Player)
            return;

        health.Heal(HealAmount);
        QueueFree();
    }

    private Health FindHealth(Node2D body)
    {
        Health health = body.GetNodeOrNull<Health>("Health");
        if (health != null)
            return health;

        Node parent = body.GetParent();
        while (parent != null)
        {
            health = parent.GetNodeOrNull<Health>("Health");
            if (health != null)
                return health;

            parent = parent.GetParent();
        }

        return null;
    }
}