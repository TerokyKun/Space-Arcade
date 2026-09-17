using Godot;

public partial class Player : CharacterBody2D
{
    [ExportGroup("Base stats")]
    [Export] public float BaseAcceleration = 1200f;
    [Export] public float BaseMaxSpeed = 400f;
    [Export] public float BaseReverseMaxSpeed = 220f;
    [Export] public float BaseFriction = 900f;
    [Export] public float RotationSpeed = 3.0f;
    [Export] public float BaseFireCooldown = 0.2f;

    private float _fireTimer = 0f;
    private float _currentSpeed = 0f;
    private float _invulnerabilityTimer = 0f;

    private Marker2D _muzzle;
    private PackedScene _bulletScene;
    private Health _health;
    private Menu _menu;

    private PlayerUpgrades _upgrades;
    private TeleportAbility _teleportAbility;
    private CommanderDroneManager _droneManager;
    private RocketmanAbility _rocketmanAbility;

    private float _acceleration;
    private float _maxSpeed;
    private float _reverseMaxSpeed;
    private float _friction;
    private float _fireCooldown;

    public PackedScene BulletScene => _bulletScene;

    public override void _Ready()
    {
        AddToGroup("player");

        _muzzle = GetNodeOrNull<Marker2D>("Muzzle");
        _bulletScene = GD.Load<PackedScene>("res://scense/Bullet.tscn");
        _health = GetNodeOrNull<Health>("Health");

        _menu = GetTree().Root.GetNodeOrNull<Menu>("Game/Menu");
        _upgrades = GetNodeOrNull<PlayerUpgrades>("PlayerUpgrades");

        _teleportAbility = GetNodeOrNull<TeleportAbility>("TeleportAbility");
        _droneManager = GetNodeOrNull<CommanderDroneManager>("CommanderDroneManager");
        _rocketmanAbility = GetNodeOrNull<RocketmanAbility>("RocketmanAbility");

        if (_health != null)
        {
            _health.Team = Health.TeamType.Player;
            _health.Died += OnDied;
        }

        if (_upgrades != null)
            _upgrades.Changed += OnUpgradesChanged;

        if (_teleportAbility != null)
            _teleportAbility.Init(this);

        ApplyUpgradesToStats();

        if (_menu == null)
            GD.PushError("Player: Menu не найден по пути Game/Menu");

        if (_muzzle == null)
            GD.PushError("Player: Muzzle не найден");

        if (_bulletScene == null)
            GD.PushError("Player: Bullet.tscn не найден");

        if (_upgrades == null)
            GD.PushWarning("Player: PlayerUpgrades не найден");

        if (_teleportAbility == null)
            GD.PushWarning("Player: TeleportAbility не найден");

        if (_droneManager == null)
            GD.PushWarning("Player: CommanderDroneManager не найден");

        if (_rocketmanAbility == null)
            GD.PushWarning("Player: RocketmanAbility не найден");
    }

    public override void _ExitTree()
    {
        if (_health != null && GodotObject.IsInstanceValid(_health))
            _health.Died -= OnDied;

        if (_upgrades != null)
            _upgrades.Changed -= OnUpgradesChanged;
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;

        if (_invulnerabilityTimer > 0f)
            _invulnerabilityTimer -= d;

        float turn = Input.GetAxis("rotate-left", "rotate-right");
        Rotation += turn * RotationSpeed * d;

        Vector2 forward = Vector2.Up.Rotated(Rotation);
        float throttle = Input.GetAxis("down", "up");

        if (!Mathf.IsZeroApprox(throttle))
        {
            float targetSpeed = throttle > 0f ? _maxSpeed : -_reverseMaxSpeed;
            _currentSpeed = Mathf.MoveToward(_currentSpeed, targetSpeed, _acceleration * d);
        }
        else
        {
            _currentSpeed = Mathf.MoveToward(_currentSpeed, 0f, _friction * d);
        }

        Velocity = forward * _currentSpeed;
        MoveAndSlide();

        _fireTimer -= d;

        if ((Input.IsActionPressed("shot") || Input.IsKeyPressed(Key.Space)) && _fireTimer <= 0f)
        {
            bool specialShotSpawned = _rocketmanAbility != null && _rocketmanAbility.TryHandleShot(forward);

            if (!specialShotSpawned)
                Shoot(forward);

            _fireTimer = _fireCooldown;
        }
    }

    private void ApplyUpgradesToStats()
    {
        if (_upgrades == null)
        {
            _acceleration = BaseAcceleration;
            _maxSpeed = BaseMaxSpeed;
            _reverseMaxSpeed = BaseReverseMaxSpeed;
            _friction = BaseFriction;
            _fireCooldown = BaseFireCooldown;
            return;
        }

        _acceleration = BaseAcceleration;
        _maxSpeed = BaseMaxSpeed * _upgrades.MoveSpeedMultiplier;
        _reverseMaxSpeed = BaseReverseMaxSpeed * _upgrades.MoveSpeedMultiplier;
        _friction = BaseFriction;
        _fireCooldown = BaseFireCooldown / Mathf.Max(0.01f, _upgrades.FireRateMultiplier);
    }

    private void OnUpgradesChanged()
    {
        ApplyUpgradesToStats();
        GD.Print("Апгрейд применён");
    }

    private void Shoot(Vector2 direction)
    {
        if (_bulletScene == null || _muzzle == null)
            return;

        Bullet bullet = _bulletScene.Instantiate<Bullet>();

        Node parent = GetTree().CurrentScene ?? GetTree().Root;
        parent.AddChild(bullet);

        float damage = 25f * (_upgrades != null ? _upgrades.BulletDamageMultiplier : 1f);

        bullet.GlobalPosition = _muzzle.GlobalPosition;
        bullet.Init(direction, damage, 900f, Bullet.BulletOwner.Player, this);
    }

    public void GrantInvulnerability(float seconds)
    {
        _invulnerabilityTimer = Mathf.Max(_invulnerabilityTimer, seconds);
    }

    public void TakeDamage(float dmg)
    {
        if (_invulnerabilityTimer > 0f)
            return;

        if (_health != null)
            _health.ApplyDamage(dmg);
    }

    private void OnDied()
    {
        Velocity = Vector2.Zero;
        SetPhysicsProcess(false);
        SetProcess(false);
        _menu?.ShowOnDeath();
    }
}