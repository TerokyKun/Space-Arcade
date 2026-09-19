using Godot;
using System;
using System.Collections.Generic;

// Автономный компаньон: самостоятельная сущность в мире со своим HP, регеном,
// мягким следованием за игроком, поиском цели и уникальной атакой.
// Не является «придатком» игрока: может погибнуть и быть снова получен через кейс.
public partial class CompanionRuntime : Area2D
{
    public const int CompanionCollisionLayer = 32;

    private CompanionDefinition _def;
    private Player _player;
    private Health _health;
    private Sprite2D _sprite;

    private float _attackCooldown = 0f;
    private float _searchTimer = 0f;
    private Node2D _target;

    private float _lastDamageTime = -999f;
    private float _lastHp = 0f;

    private float _orbitAngle = 0f;
    private float _turnVisual = 0f;

    private int _burstRemaining = 0;
    private float _burstTimer = 0f;

    private float _beamTimer = 0f;
    private Vector2 _beamEnd;

    private readonly RandomNumberGenerator _rng = new();

    private PackedScene _bulletScene;
    private bool _dead = false;

    private readonly List<CompanionRuntime> _siblingCache = new();
    private float _siblingRefresh = 0f;

    public int Slot { get; set; } = 0;
    public int TotalSlots { get; set; } = 1;

    public CompanionId CompanionId => _def?.Id ?? CompanionId.MedBot;
    public CompanionDefinition Definition => _def;
    public bool IsDead => _dead;
    public float CurrentHP => _health != null ? _health.CurrentHP : 0f;
    public float MaxHP => _health != null ? _health.MaxHP : 0f;

    public override void _Ready()
    {
        AddToGroup("companion");
        CollisionLayer = CompanionCollisionLayer;
        CollisionMask = 0;

        _rng.Randomize();

        _bulletScene = GD.Load<PackedScene>("res://scenes/gameplay/Bullet.tscn");
        _health = GetNodeOrNull<Health>("Health");
        _sprite = GetNodeOrNull<Sprite2D>("Sprite");

        if (_health == null)
        {
            GD.PushError("CompanionRuntime: Health не найден");
            SetPhysicsProcess(false);
            return;
        }

        _health.Team = Health.TeamType.Ally;
        _health.AutoFreeOwner = false;
        _health.HealthChanged += OnHealthChanged;
        _health.Died += OnDied;

        _lastHp = _health.CurrentHP;
    }

    public void Init(CompanionDefinition def, Player player)
    {
        _def = def;
        _player = player;

        if (_health != null)
            _health.SetMaxHealth(def.MaxHP, true);

        if (_sprite != null)
        {
            _sprite.Texture = def.Icon;
            _sprite.Modulate = GetTint(def.Id);
        }

        _lastHp = _health.CurrentHP;
        _orbitAngle = (Slot / (float)Math.Max(1, TotalSlots)) * Mathf.Tau;
    }

    public override void _ExitTree()
    {
        if (_health != null && GodotObject.IsInstanceValid(_health))
        {
            _health.HealthChanged -= OnHealthChanged;
            _health.Died -= OnDied;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_dead || _def == null)
            return;

        if (GameManager.IsGameOver)
            return;

        float d = (float)delta;

        UpdateMovement(d);
        UpdateRegen(d);
        TickBehavior(d);
    }

    // ---- Движение: не строем, а мягко по индивидуальной орбите вокруг игрока ----

    private void UpdateMovement(float d)
    {
        if (_player == null || !GodotObject.IsInstanceValid(_player))
        {
            _player = GetTree().GetFirstNodeInGroup("player") as Player;
            return;
        }

        _orbitAngle += d * (0.35f + (Slot % 3) * 0.12f);

        float radius = _def.FollowDistance * (0.75f + 0.25f * (TotalSlots > 1 ? (Slot % TotalSlots) / (float)(TotalSlots - 1) : 0.5f));
        Vector2 orbitCenter = new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * radius;
        Vector2 desired = _player.GlobalPosition + orbitCenter;

        desired = ApplySeparation(desired, radius);

        float minDistance = Mathf.Min(radius, 46f);
        Vector2 toPlayer = desired - _player.GlobalPosition;
        if (toPlayer.Length() < minDistance && toPlayer.Length() > 0.001f)
            desired = _player.GlobalPosition + toPlayer.Normalized() * minDistance;

        float smoothing = 1f - Mathf.Exp(-_def.MoveSmoothing * 0.45f * d);
        GlobalPosition = GlobalPosition.Lerp(desired, smoothing);

        _turnVisual += d * 2.4f;
    }

    private Vector2 ApplySeparation(Vector2 desired, float radius)
    {
        float min = Mathf.Max(30f, radius * 0.5f);

        _siblingRefresh -= 0.01f;
        if (_siblingRefresh <= 0f)
        {
            _siblingRefresh = 0.5f;
            _siblingCache.Clear();
            foreach (Node node in GetTree().GetNodesInGroup("companion"))
            {
                if (node is CompanionRuntime other && other != this && !other.IsDead)
                    _siblingCache.Add(other);
            }
        }

        for (int i = 0; i < _siblingCache.Count; i++)
        {
            CompanionRuntime other = _siblingCache[i];
            if (other == null || !GodotObject.IsInstanceValid(other) || other.IsDead)
                continue;

            Vector2 to = desired - other.GlobalPosition;
            float dist = to.Length();
            if (dist < min && dist > 0.001f)
                desired += to.Normalized() * min * (1f - dist / min) * 0.6f;
        }

        return desired;
    }

    // ---- Реген: с задержкой после последнего урона ----

    private void OnHealthChanged(float currentHp, float maxHp)
    {
        float oldHp = _lastHp;
        _lastHp = currentHp;

        if (currentHp < oldHp)
            _lastDamageTime = Time.GetTicksMsec() / 1000f;
    }

    private void UpdateRegen(float d)
    {
        if (_def.RegenRate <= 0f || _health == null)
            return;

        float now = Time.GetTicksMsec() / 1000f;
        if (now - _lastDamageTime < _def.RegenDelay)
            return;

        if (_health.CurrentHP >= _health.MaxHP)
            return;

        _health.Heal(_def.RegenRate * d);
    }

    // ---- Поведение по типу спутника ----

    private void TickBehavior(float d)
    {
        _attackCooldown -= d;
        _searchTimer -= d;
        _burstTimer -= d;

        if (_beamTimer > 0f)
        {
            _beamTimer -= d;
            QueueRedraw();
        }

        if (_def.Id == CompanionId.MedBot)
        {
            TickHealPlayer(d);
            return;
        }

        if (_def.Id == CompanionId.Guardian)
            return;

        if (_target == null || !GodotObject.IsInstanceValid(_target) || _target.IsQueuedForDeletion())
            _target = null;

        if (_searchTimer <= 0f)
        {
            _searchTimer = 0.35f;
            _target = FindNearestEnemy(GlobalPosition, _def.Range);
        }

        if (_target == null)
            return;

        if (_def.Id == CompanionId.FireCompanion && _attackCooldown <= 0f)
        {
            _attackCooldown = _def.Cooldown;
            DoFireLaser(_target);
        }

        if (_def.Id == CompanionId.CombatDrone)
        {
            if (_burstRemaining <= 0f && _attackCooldown <= 0f)
            {
                _attackCooldown = _def.Cooldown;
                _burstRemaining = Math.Max(1, _def.BurstCount);
                _burstTimer = 0f;
            }

            if (_burstRemaining > 0f && _burstTimer <= 0f)
            {
                FireDroneBullet(_target);
                _burstRemaining--;
                _burstTimer = _def.BurstGap;
            }
        }
    }

    private void TickHealPlayer(float d)
    {
        if (_attackCooldown > 0f)
            return;

        _attackCooldown = _def.Cooldown;

        if (_player == null || !GodotObject.IsInstanceValid(_player))
            return;

        var health = _player.GetNodeOrNull<Health>("Health");
        if (health == null || !GodotObject.IsInstanceValid(health))
            return;

        float amount = Mathf.Max(1f, health.MaxHP * _def.HealPercentPerTick);
        health.Heal(amount);
    }

    private void DoFireLaser(Node2D target)
    {
        if (target is Enemy enemy && GodotObject.IsInstanceValid(enemy))
            enemy.ApplyBurn(_def.BurnDamagePerSec, _def.BurnDuration, _def.BurnMaxStacks, this);

        _beamTimer = 0.28f;
        _beamEnd = target.GlobalPosition;
        QueueRedraw();
    }

    private void FireDroneBullet(Node2D target)
    {
        if (_bulletScene == null)
            return;

        Vector2 dir = (target.GlobalPosition - GlobalPosition).Normalized();
        if (dir == Vector2.Zero)
            return;

        dir = dir.Rotated(_rng.RandfRange(-_def.Spread, _def.Spread));

        Node parent = GetTree().CurrentScene ?? GetTree().Root;
        var bullet = _bulletScene.Instantiate<Bullet>();
        parent.AddChild(bullet);
        bullet.GlobalPosition = GlobalPosition + dir * 34f;
        bullet.Init(dir, _def.Damage, _def.ProjectileSpeed, Bullet.BulletOwner.Player, _player);
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

    private void OnDied()
    {
        if (_dead)
            return;

        _dead = true;
        SetPhysicsProcess(false);

        CompanionManager.Instance?.NotifyCompanionDied(this);
        PlayDeathEffect();
    }

    // Эффект гибели: вспышка и растворение, затем удаление.
    private void PlayDeathEffect()
    {
        Modulate = new Color(1f, 0.75f, 0.75f);

        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(this, "modulate:a", 0f, 0.5f);
        tween.TweenProperty(this, "scale", new Vector2(1.7f, 1.7f) * Scale.Length(), 0.5f);

        GetTree().CreateTimer(0.5f).Timeout += QueueFree;
    }

    // ---- Отладка ----

    public void DebugKill()
    {
        if (_health != null && GodotObject.IsInstanceValid(_health))
            _health.ApplyDamage(_health.MaxHP + 1000f);
    }

    public void DebugRestoreHp()
    {
        if (_health != null && GodotObject.IsInstanceValid(_health))
            _health.Heal(_health.MaxHP);
    }

    // ---- Визуал луча (лазер) ----

    public override void _Draw()
    {
        if (_beamTimer <= 0f || _target == null || !GodotObject.IsInstanceValid(_target))
            return;

        Vector2 start = ToLocal(GlobalPosition);
        Vector2 end = ToLocal(_beamEnd);

        DrawLine(start, end, new Color(1f, 0.45f, 0.12f, 0.25f + 0.7f * _beamTimer), 4f);
        DrawLine(start, end, new Color(1f, 0.85f, 0.45f, 0.2f + 0.6f * _beamTimer), 2f);
    }

    private static Color GetTint(CompanionId id) => id switch
    {
        CompanionId.MedBot => new Color(0.8f, 1f, 0.85f),
        CompanionId.Guardian => new Color(0.75f, 0.8f, 1f),
        CompanionId.FireCompanion => new Color(1f, 0.62f, 0.25f),
        CompanionId.CombatDrone => new Color(0.55f, 0.9f, 1f),
        _ => Colors.White
    };
}