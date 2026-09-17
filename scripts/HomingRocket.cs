using Godot;

public partial class HomingRocket : Area2D
{
    [Export] public float TurnSpeed = 4.5f;
    [Export] public float Lifetime = 3.5f;
    // Слой врагов (4). Ракета сталкивается только с телами/областями врагов.
    [Export] public uint EnemyMask = 4u;

    private Vector2 _direction = Vector2.Up;
    private float _damage = 10f;
    private float _speed = 650f;
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

        Node2D target = FindClosestEnemy();
        if (target != null)
        {
            Vector2 toTarget = target.GlobalPosition - GlobalPosition;
            float dist = toTarget.Length();

            if (dist > 0.01f)
            {
                Vector2 desired = toTarget / dist;

                // Дуга: чем дальше враг, тем сильнее ракета заходит сбоку (вокруг препятствий),
                // у цели выправляется и бьёт чётко в ближайшего врага.
                float side = Mathf.Sign(_direction.X * toTarget.Y - _direction.Y * toTarget.X);
                Vector2 perp = new Vector2(-_direction.Y, _direction.X) * side;
                float arcAmount = Mathf.Min(1f, dist / 260f) * 0.65f;

                Vector2 steered = (desired + perp * arcAmount).Normalized();
                _direction = _direction.Slerp(steered, TurnSpeed * d).Normalized();
            }
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

        // Ракета игнорирует всё (камни, пикапы, игрока) — CollisionMask только слой врагов.
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