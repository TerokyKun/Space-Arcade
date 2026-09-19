using Godot;

public partial class RocketmanAbility : Node
{
    [ExportGroup("Scene links")]
    [Export] public PackedScene RocketScene;

    [ExportGroup("Settings")]
    [Export] public float RocketSpeed = 650f;
    [Export] public float RocketDamageMultiplier = 3f;

    private Player _player;
    private PlayerUpgrades _upgrades;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _player = GetParent<Player>();
        _upgrades = _player?.GetNodeOrNull<PlayerUpgrades>("PlayerUpgrades");

        if (_upgrades != null)
            _upgrades.Changed += OnUpgradesChanged;

        _rng.Randomize();
    }

    public override void _ExitTree()
    {
        if (_upgrades != null)
            _upgrades.Changed -= OnUpgradesChanged;
    }

    public bool TryHandleShot(Vector2 forward)
    {
        if (_player == null || _upgrades == null)
            return false;

        if (_upgrades.SelectedClass != PlayerClassType.Rocketman)
            return false;

        if (_rng.Randf() > _upgrades.RocketChance)
            return false;

        SpawnRocket(forward);
        return true;
    }

    private void SpawnRocket(Vector2 forward)
    {
        if (_player == null || _upgrades == null)
            return;

        float baseBulletDamage = 25f * _upgrades.BulletDamageMultiplier;
        float rocketDamage = baseBulletDamage * RocketDamageMultiplier;

        if (RocketScene != null)
        {
            HomingRocket rocket = RocketScene.Instantiate<HomingRocket>();

            Node parent = GetTree().CurrentScene ?? GetTree().Root;
            parent.CallDeferred(Node.MethodName.AddChild, rocket);

            rocket.GlobalPosition = _player.GlobalPosition;
            rocket.Init(forward, rocketDamage, RocketSpeed, _player);
            return;
        }

        if (_player.BulletScene == null)
            return;

        Bullet bullet = _player.BulletScene.Instantiate<Bullet>();

        Node bulletParent = GetTree().CurrentScene ?? GetTree().Root;
        bulletParent.CallDeferred(Node.MethodName.AddChild, bullet);

        bullet.GlobalPosition = _player.GlobalPosition;
        bullet.Init(forward, rocketDamage, RocketSpeed, Bullet.BulletOwner.Player, _player);
    }

    private void OnUpgradesChanged()
    {
        // пока ничего не делаем, но подписка есть для будущих динамических изменений
    }
}