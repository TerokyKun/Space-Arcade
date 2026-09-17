using Godot;

public partial class Asteroid : Area2D
{
    [Export] public float DamagePercent = 0.5f;
    [Export] public float Lifetime = 20f;

    private Vector2 _velocity = Vector2.Zero;
    private float _angularSpeed = 0f;
    private float _lifeTimer = 0f;
    private bool _hit = false;
    private bool _initialized = false;

    public override void _Ready()
    {
        AddToGroup("space_object");
        AddToGroup("asteroid");

        BodyEntered += OnBodyEntered;

        if (!_initialized)
            _lifeTimer = Lifetime;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public void Initialize(Vector2 spawnPosition, Vector2 velocity, float angularSpeed)
    {
        GlobalPosition = spawnPosition;
        _velocity = velocity;
        _angularSpeed = angularSpeed;
        _lifeTimer = Lifetime;
        _hit = false;
        _initialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized)
            return;

        float d = (float)delta;

        Position += _velocity * d;
        Rotation += _angularSpeed * d;

        _lifeTimer -= d;
        if (_lifeTimer <= 0f)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_hit || body == null)
            return;

        Health health = FindHealth(body);
        if (health == null)
            return;

        if (health.Team != Health.TeamType.Player && health.Team != Health.TeamType.Enemy)
            return;

        _hit = true;

        float damage = health.MaxHP  * DamagePercent;
        health.ApplyDamage(damage);

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