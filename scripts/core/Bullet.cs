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

    // Мифические модификаторы (Sngularity / Split Core / Time Fracture).
    private int _singularityLevel = 0;
    private int _splitLevel = 0;
    private float _slowChance = 0f;
    private float _slowFactor = 0f;
    private float _slowDuration = 0f;

    private readonly RandomNumberGenerator _rng = new();
    private PackedScene _bulletScene;

    public BulletOwner OwnerType;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        _rng.Randomize();

        // Пуля имеет собственный физический слой, но не участвует в обычном столкновении тел
        CollisionLayer = 16;
        CollisionMask = 0;

        GetTree().CreateTimer(LifeTime).Timeout += QueueFree;
    }

    public void ApplyMythic(int singularityLevel, int splitLevel, float slowChance, float slowFactor, float slowDuration)
    {
        _singularityLevel = Mathf.Max(0, singularityLevel);
        _splitLevel = Mathf.Max(0, splitLevel);
        _slowChance = Mathf.Clamp(slowChance, 0f, 1f);
        _slowFactor = Mathf.Clamp(slowFactor, 0f, 1f);
        _slowDuration = Mathf.Max(0f, slowDuration);
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

        // Сингулярность: снаряды игрока слегка притягиваются к ближайшему врагу.
        if (OwnerType == BulletOwner.Player && _singularityLevel > 0)
        {
            Node2D target = FindNearestEnemy(GlobalPosition, 800f);
            if (target != null)
            {
                Vector2 dirToTarget = (target.GlobalPosition - GlobalPosition).Normalized();
                _direction = _direction.Lerp(dirToTarget, Mathf.Clamp(16f * d, 0f, 1f)).Normalized();
            }
        }

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
        // Лучи попадают в окружение, игрока, врагов и другие снаряды.
        // Пикапы (хилки/дроп) физически не являются целями и не съедают пули.
        // Снаряды врагов дополнительно цепляют компаньонов (слой 32) —
        // спутники автономны и могут получать урон (но не от игрока).
        query.CollisionMask = (uint)(OwnerType == BulletOwner.Enemy ? (1 | 2 | 4 | 16 | CompanionRuntime.CompanionCollisionLayer) : (1 | 2 | 4 | 16));
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

            // Урон игроку идёт через Player.TakeDamage (учитывает неуязвимость телепорта),
            // урон врагу — через Enemy.TakeDamage (корректная смерть + дроп XP).
            if (health.Team == Health.TeamType.Player && collider is Player playerBody)
            {
                playerBody.TakeDamage(_damage);
            }
            else if (health.Team == Health.TeamType.Enemy && collider is Enemy enemyBody)
            {
                // Time Fracture: шанс замедлить врага при попадании.
                if (OwnerType == BulletOwner.Player && _slowChance > 0f && _rng.Randf() < _slowChance)
                    enemyBody.ApplySlow(_slowFactor, _slowDuration);

                enemyBody.TakeDamage(_damage);

                // Split Core: пуля игрока делится при попадании во врага.
                if (OwnerType == BulletOwner.Player && _splitLevel > 0)
                    SpawnSplitBullets(enemyBody.GlobalPosition);
            }
            else
            {
                health.ApplyDamage(_damage);
            }

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
        if (OwnerType == BulletOwner.Player && (target.Team == Health.TeamType.Player || target.Team == Health.TeamType.Ally))
            return true;

        if (OwnerType == BulletOwner.Enemy && target.Team == Health.TeamType.Enemy)
            return true;

        return false;
    }

    private void SpawnSplitBullets(Vector2 at)
    {
        if (_shooter == null || _damage < 3f)
            return;

        _bulletScene ??= GD.Load<PackedScene>("res://scenes/gameplay/Bullet.tscn");
        if (_bulletScene == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            Vector2 dir = _direction.Rotated(i == 0 ? 0.35f : -0.35f);

            var bullet = _bulletScene.Instantiate<Bullet>();
            Node parent = GetTree().CurrentScene ?? GetTree().Root;
            parent.AddChild(bullet);

            bullet.GlobalPosition = at;
            bullet.Scale = Scale * 0.85f;
            bullet.Init(dir, _damage * 0.6f, _speed, OwnerType, _shooter);
            bullet.ApplyMythic(_singularityLevel, Mathf.Max(0, _splitLevel - 1), _slowChance, _slowFactor, _slowDuration);
        }
    }

    private Node2D FindNearestEnemy(Vector2 from, float maxRange)
    {
        Node2D best = null;
        float bestSq = float.MaxValue;
        float maxSq = maxRange * maxRange;

        foreach (Node node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is not Node2D enemy2D)
                continue;

            if (!GodotObject.IsInstanceValid(enemy2D) || enemy2D.IsQueuedForDeletion())
                continue;

            float sq = from.DistanceSquaredTo(enemy2D.GlobalPosition);
            if (sq > maxSq)
                continue;

            if (sq < bestSq)
            {
                bestSq = sq;
                best = enemy2D;
            }
        }

        return best;
    }
}