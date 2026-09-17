using Godot;

public partial class CommanderShot : Area2D
{
    [Export] public float Lifetime = 2.5f;

    private Vector2 _direction = Vector2.Up;
    private float _damage = 10f;
    private float _speed = 1000f;
    private Node2D _owner;
    private float _lifeTimer;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
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

        if (body.IsInGroup("enemy"))
        {
            body.Call("TakeDamage", _damage);
            QueueFree();
            return;
        }

        if (body.HasMethod("TakeDamage"))
        {
            body.Call("TakeDamage", _damage);
            QueueFree();
        }
    }
}