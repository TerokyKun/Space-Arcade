using Godot;
using System;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D
{
    [Export] public float Acceleration = 700f;
    [Export] public float MaxSpeed = 100f;
    [Export] public float Friction = 500f;
    [Export] public float TurnSpeed = 5.0f;

    [Export] public float FireCooldown = 3.0f;
    [Export] public float StopDistance = 150f;

    [Export] public float BulletSpeed = 450f;
    [Export] public float BulletDamage = 10f;

    [Export] public float MaxHP = 100f;

    [Export] public PackedScene DropPickup;
    [Export] public int DropMinCount = 2;
    [Export] public int DropMaxCount = 6;
    [Export] public float DropScatterRadius = 14f;
    [Export] public float DropInitialImpulse = 28f;

    [Export] public EnemyConfig Config;

    private bool _ranged = false;
    private bool _isBoss = false;

    private Node2D _player;
    private Marker2D _muzzle;
    private float _fireTimer = 0f;
    private float _currentSpeed = 0f;

    private PackedScene _bulletScene;
    private Health _health;
    private ProgressBar _hpBar;

    private bool _dead = false;
    private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

    // ---- Статусы: горение (DoT) и замедление (Time Fracture) ----
    private readonly List<BurnInstance> _burns = new();
    private float _slowFactor = 1f;
    private float _slowTimer = 0f;

    private struct BurnInstance
    {
        public float Remaining;
        public float Interval;
        public float TickTimer;
        public float DamagePerTick;
        public Node Attacker;

        public BurnInstance(float damagePerSec, float duration, Node attacker)
        {
            Interval = 0.5f;
            Remaining = duration;
            TickTimer = 0.05f; // первый тик почти сразу
            DamagePerTick = damagePerSec * Interval;
            Attacker = attacker;
        }
    }

    public override void _Ready()
    {
        AddToGroup("enemy");
        ApplyConfig();
        ApplyDifficulty();

        _rng.Randomize();

        _muzzle = GetNodeOrNull<Marker2D>("Muzzle");
        _bulletScene = GD.Load<PackedScene>("res://scenes/gameplay/Bullet.tscn");
        _health = GetNodeOrNull<Health>("Health");
        _hpBar = GetNodeOrNull<ProgressBar>("ProgressBar");

        if (_health == null)
        {
            GD.PushError("Enemy: Health не найден");
            return;
        }

        _health.Team = Health.TeamType.Enemy;
        _health.MaxHP = MaxHP;
        _health.HealthChanged += OnHealthChanged;

        if (_hpBar != null)
        {
            _hpBar.MaxValue = _health.MaxHP;
            _hpBar.Value = _health.CurrentHP;
        }

        AcquirePlayer();
    }

    private void ApplyConfig()
    {
        if (Config == null)
            return;

        _ranged = Config.IsRanged;
        _isBoss = Config.IsBoss;

        Acceleration = Config.Acceleration;
        MaxSpeed = Config.MaxSpeed;
        Friction = Config.Friction;
        TurnSpeed = Config.TurnSpeed;
        FireCooldown = Config.FireCooldown;
        StopDistance = Config.StopDistance;
        BulletSpeed = Config.BulletSpeed;
        BulletDamage = Config.BulletDamage;
        MaxHP = Config.MaxHP;
        DropMinCount = Mathf.Max(1, Config.DropMin);
        DropMaxCount = Mathf.Max(DropMinCount, Config.DropMax);

        Sprite2D sprite = GetNodeOrNull<Sprite2D>("Sprite");
        if (sprite != null)
        {
            if (Config.Icon != null)
            {
                sprite.Texture = Config.Icon;
                sprite.Modulate = Colors.White;
            }
        }

        if (Config.ScaleMultiplier != 1f)
            Scale = Vector2.One * Config.ScaleMultiplier;

        if (_isBoss)
        {
            AddToGroup("boss");
            Name = "Boss";
        }
    }

    private void ApplyDifficulty()
    {
        var dm = DifficultyManager.Instance;
        if (dm == null)
            return;

        MaxHP *= dm.HpMultiplier;
        BulletDamage *= dm.DamageMultiplier;
        MaxSpeed *= dm.SpeedMultiplier;
        Acceleration *= dm.SpeedMultiplier;
    }

    public override void _ExitTree()
    {
        if (_health != null && GodotObject.IsInstanceValid(_health))
            _health.HealthChanged -= OnHealthChanged;
    }

    private void AcquirePlayer()
    {
        _player = null;

        var players = GetTree().GetNodesInGroup("player");
        foreach (var p in players)
        {
            if (p is Node2D n2d)
            {
                _player = n2d;
                break;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_dead)
            return;

        float d = (float)delta;

        if (_player == null || !GodotObject.IsInstanceValid(_player))
        {
            AcquirePlayer();
            return;
        }

        Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
        float distance = toPlayer.Length();

        if (distance < 0.001f)
            return;

        Vector2 dir = toPlayer.Normalized();
        float targetAngle = dir.Angle() + Mathf.Pi / 2f;
        Rotation = Mathf.LerpAngle(Rotation, targetAngle, TurnSpeed * d);

        Vector2 forward = Vector2.Up.Rotated(Rotation);

        float effectiveMaxSpeed = MaxSpeed * _slowFactor;

        if (_ranged && distance < StopDistance * 0.45f)
            _currentSpeed = Mathf.MoveToward(_currentSpeed, -effectiveMaxSpeed * 0.5f, Acceleration * d);
        else if (distance > StopDistance)
            _currentSpeed = Mathf.MoveToward(_currentSpeed, effectiveMaxSpeed, Acceleration * d);
        else
            _currentSpeed = Mathf.MoveToward(_currentSpeed, 0f, Friction * d);

        TickSlow(d);
        TickBurn(d);

        Velocity = forward * _currentSpeed;
        MoveAndSlide();

        _fireTimer -= d;

        if (_fireTimer <= 0f)
        {
            Shoot(forward);
            _fireTimer = FireCooldown;
        }
    }

    private void Shoot(Vector2 direction)
    {
        if (_bulletScene == null)
        {
            GD.PushError("Enemy: BulletScene не загружен");
            return;
        }

        var bullet = _bulletScene.Instantiate<Bullet>();
        GetTree().CurrentScene.AddChild(bullet);

        bullet.GlobalPosition = _muzzle != null ? _muzzle.GlobalPosition : GlobalPosition;
        bullet.Init(direction, BulletDamage, BulletSpeed, Bullet.BulletOwner.Enemy, this);
    }

    public int GetFaction() => 1;

    // ---- Статусы: горение и замедление ----

    public void ApplyBurn(float damagePerSec, float duration, int maxStacks, Node attacker)
    {
        if (_dead || damagePerSec <= 0f || duration <= 0f)
            return;

        maxStacks = Mathf.Max(1, maxStacks);

        // Один источник = один «костёр» (обновляем оставшееся время).
        for (int i = 0; i < _burns.Count; i++)
        {
            if (ReferenceEquals(_burns[i].Attacker, attacker))
            {
                BurnInstance existing = _burns[i];
                existing.Remaining = duration;
                existing.TickTimer = Mathf.Min(existing.TickTimer, 0.2f);
                _burns[i] = existing;
                return;
            }
        }

        if (_burns.Count >= maxStacks)
            return;

        _burns.Add(new BurnInstance(damagePerSec, duration, attacker));
    }

    private void TickBurn(float delta)
    {
        if (_burns.Count == 0)
            return;

        for (int i = _burns.Count - 1; i >= 0; i--)
        {
            BurnInstance burn = _burns[i];

            burn.Remaining -= delta;
            if (burn.Remaining <= 0f)
            {
                _burns.RemoveAt(i);
                continue;
            }

            burn.TickTimer -= delta;

            bool resetTimerAfterDamage = false;
            if (burn.TickTimer <= 0f)
            {
                burn.TickTimer = burn.Interval;
                resetTimerAfterDamage = true;
            }

            _burns[i] = burn;

            if (resetTimerAfterDamage)
            {
                TakeDamage(burn.DamagePerTick);
                if (_dead)
                    return;
            }
        }
    }

    public void ApplySlow(float factor, float duration)
    {
        _slowFactor = Mathf.Min(_slowFactor, Mathf.Clamp(factor, 0f, 1f));
        _slowTimer = Mathf.Max(_slowTimer, duration);
    }

    private void TickSlow(float delta)
    {
        if (_slowTimer <= 0f)
            return;

        _slowTimer -= delta;
        if (_slowTimer <= 0f)
            _slowFactor = 1f;
    }

    public void TakeDamage(float dmg)
    {
        if (_dead)
            return;

        if (_health != null && GodotObject.IsInstanceValid(_health))
            _health.ApplyDamage(dmg);

        if (_health != null && _health.CurrentHP <= 0f)
            Die();
    }

    private void Die()
    {
        if (_dead)
            return;

        _dead = true;
        GrantXpReward();
        SpawnDrops();
        QueueFree();
    }

    // XP за убийство конкретного типа (из конфига EnemyConfig.XpReward).
    private void GrantXpReward()
    {
        int xp = Config != null ? Config.XpReward : 0;
        if (xp <= 0)
            return;

        var lvl = GetTree().GetFirstNodeInGroup("lvl_ui") as Lvl;
        if (lvl != null && GodotObject.IsInstanceValid(lvl))
            lvl.AddGears(xp);
    }

    private void SpawnDrops()
    {
        if (DropPickup == null)
        {
            GD.PushWarning("Enemy: DropPickup не назначен в инспекторе.");
            return;
        }

        int count = _rng.RandiRange(DropMinCount, DropMaxCount);

        for (int i = 0; i < count; i++)
        {
            var drop = DropPickup.Instantiate<DropPickup>();

            Vector2 offset = new Vector2(
                _rng.RandfRange(-DropScatterRadius, DropScatterRadius),
                _rng.RandfRange(-DropScatterRadius, DropScatterRadius)
            );

            Vector2 direction = offset == Vector2.Zero
                ? Vector2.Right.Rotated(_rng.RandfRange(0f, Mathf.Tau))
                : offset.Normalized();

            drop.Initialize(GlobalPosition + offset, direction * DropInitialImpulse);

            // SpawnDrops вызывается из колбэка физики (BodyEntered → урон → сигнал Health),
            // поэтому добавление в дерево нужно откладывать, иначе Godot падает:
            // "Can't change this state while flushing queries".
            GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, drop);
        }
    }

    private void OnHealthChanged(float currentHp, float maxHp)
    {
        if (_hpBar != null && GodotObject.IsInstanceValid(_hpBar))
        {
            _hpBar.MaxValue = maxHp;
            _hpBar.Value = currentHp;
        }

        if (!_dead && currentHp <= 0f)
            Die();
    }
}