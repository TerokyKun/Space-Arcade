using Godot;
using System;

public partial class Bullet : CharacterBody2D
{
    public enum BulletOwner
    {
        Player = 0,
        Enemy = 1
    }

    [Export] public float LifeTime = 5f;

    private float _damage;
    private float _speed;
    private Vector2 _direction;

    private Sprite2D _sprite;
    private Node _shooter;

    public BulletOwner OwnerType;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

        // Пуля не должна участвовать в обычной физике
        CollisionLayer = 0;
        CollisionMask = 0;

        GetTree().CreateTimer(LifeTime).Timeout += QueueFree;
    }

    public void Init(Vector2 direction, float damage, float speed, BulletOwner owner, Node shooter)
    {
        _direction = direction.Normalized();
        _damage = damage;
        _speed = speed;
        OwnerType = owner;
        _shooter = shooter;

        Rotation = _direction.Angle();

        if (_sprite != null)
        {
            _sprite.Modulate = owner == BulletOwner.Player
                ? new Color(0f, 1f, 0f)
                : new Color(1f, 0f, 0f);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;
        Vector2 motion = _direction * _speed * d;

        if (motion == Vector2.Zero)
            return;

        Vector2 start = GlobalPosition;
        Vector2 end = start + motion;

        var spaceState = GetWorld2D().DirectSpaceState;

        var exclude = new Godot.Collections.Array<Rid>
        {
            GetRid()
        };

        if (_shooter != null && GodotObject.IsInstanceValid(_shooter) && _shooter is CollisionObject2D shooterBody)
            exclude.Add(shooterBody.GetRid());

        var query = PhysicsRayQueryParameters2D.Create(start, end);
        query.CollideWithBodies = true;
        query.CollideWithAreas = true;
        query.Exclude = exclude;

        var result = spaceState.IntersectRay(query);

        if (result.Count == 0)
        {
            GlobalPosition = end;
            return;
        }

        var collider = result["collider"].As<Node>();
        if (collider == null || !GodotObject.IsInstanceValid(collider))
        {
            GlobalPosition = end;
            return;
        }

        if (collider is Bullet)
        {
            QueueFree();
            return;
        }

        if (_shooter != null && GodotObject.IsInstanceValid(_shooter) && collider == _shooter)
        {
            GlobalPosition = end;
            return;
        }

        Health health = FindHealth(collider);

        if (health != null && GodotObject.IsInstanceValid(health))
        {
            if (IsSameTeam(health))
            {
                QueueFree();
                return;
            }

            health.ApplyDamage(_damage);
            QueueFree();
            return;
        }

        // Любая другая коллизия уничтожает пулю
        QueueFree();
    }

    private Health FindHealth(Node node)
    {
        Node current = node;

        while (current != null && GodotObject.IsInstanceValid(current))
        {
            Health health = current.GetNodeOrNull<Health>("Health");
            if (health != null && GodotObject.IsInstanceValid(health))
                return health;

            if (current is Health selfHealth)
                return selfHealth;

            current = current.GetParent();
        }

        return null;
    }

    private bool IsSameTeam(Health target)
    {
        if (OwnerType == BulletOwner.Player && target.Team == Health.TeamType.Player)
            return true;

        if (OwnerType == BulletOwner.Enemy && target.Team == Health.TeamType.Enemy)
            return true;

        return false;
    }
}