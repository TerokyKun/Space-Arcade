using Godot;

public partial class CommanderShot : Area2D
{
    [Export] public float Lifetime = 2.5f;
    // Слой врагов (4): снаряд дрона задевает только врагов.
    [Export] public uint EnemyMask = 4u;

    private Vector2 _direction = Vector2.Up;
    private float _damage = 10f;
    private float _speed = 1000f;
    private Node2D _owner;
    private float _lifeTimer;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        CollisionMask = EnemyMask;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public void Init(Vector2 direction, float damage, float speed, Node2D owner)
    {
        _direction = direction.Normalized();
        _damage = damage;
        _speed = speed;
        _owner = owner;
        _lifeTimer = Lifetime;
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;
        _lifeTimer -= d;

        if (_lifeTimer <= 0f)
        {
            QueueFree();
            return;
        }

        GlobalPosition += _direction * _speed * d;
        Rotation = _direction.Angle() + Mathf.Pi / 2f;
    }

    private void OnBodyEntered(Node body)
    {
        if (body == _owner)
            return;

        // Снаряд дрона задевает только врагов (CollisionMask только слой врагов).
        if (body is Enemy enemy)
        {
            enemy.TakeDamage(_damage);
            QueueFree();
            return;
        }

        if (body.IsInGroup("enemy"))
        {
            body.Call("TakeDamage", _damage);
            QueueFree();
        }
    }
}