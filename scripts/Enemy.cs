using Godot;
using System;

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

    private Node2D _player;
    private Marker2D _muzzle;
    private float _fireTimer = 0f;
    private float _currentSpeed = 0f;

    private PackedScene _bulletScene;
    private Health _health;
    private ProgressBar _hpBar;

    private bool _dead = false;
    private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        AddToGroup("enemy");

        _rng.Randomize();

        _muzzle = GetNodeOrNull<Marker2D>("Muzzle");
        _bulletScene = GD.Load<PackedScene>("res://scense/Bullet.tscn");
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

        if (distance > StopDistance)
            _currentSpeed = Mathf.MoveToward(_currentSpeed, MaxSpeed, Acceleration * d);
        else
            _currentSpeed = Mathf.MoveToward(_currentSpeed, 0f, Friction * d);

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
        SpawnDrops();
        QueueFree();
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
            GetTree().CurrentScene.AddChild(drop);

            Vector2 offset = new Vector2(
                _rng.RandfRange(-DropScatterRadius, DropScatterRadius),
                _rng.RandfRange(-DropScatterRadius, DropScatterRadius)
            );

            Vector2 direction = offset == Vector2.Zero
                ? Vector2.Right.Rotated(_rng.RandfRange(0f, Mathf.Tau))
                : offset.Normalized();

            drop.Initialize(GlobalPosition + offset, direction * DropInitialImpulse);
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