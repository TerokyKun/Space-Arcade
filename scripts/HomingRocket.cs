using Godot;

public partial class HomingRocket : Area2D
{
    [Export] public float TurnSpeed = 4.5f;
    [Export] public float Lifetime = 3.5f;
    [Export] public uint EnemyMask = 1;

    private Vector2 _direction = Vector2.Up;
    private float _damage = 10f;
    private float _speed = 650f;
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

        Node2D target = FindClosestEnemy();
        if (target != null)
        {
            Vector2 desired = (target.GlobalPosition - GlobalPosition).Normalized();
            _direction = _direction.Slerp(desired, TurnSpeed * d).Normalized();
        }

        GlobalPosition += _direction * _speed * d;
        Rotation = _direction.Angle() + Mathf.Pi / 2f;
    }

    private Node2D FindClosestEnemy()
    {
        Node2D best = null;
        float bestDist = float.MaxValue;

        foreach (Node node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Node2D enemy2D)
            {
                float dist = GlobalPosition.DistanceSquaredTo(enemy2D.GlobalPosition);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy2D;
                }
            }
        }

        return best;
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