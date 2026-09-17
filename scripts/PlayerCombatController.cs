using Godot;

public partial class PlayerCombatController : Node
{
    [ExportGroup("Scene links")]
    [Export] public PackedScene RocketScene; // если не назначена — останется обычная пуля

    private Player _player;
    private PlayerUpgrades _upgrades;
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _player = GetParent<Player>();
        _upgrades = _player?.GetNodeOrNull<PlayerUpgrades>("PlayerUpgrades");

        _rng.Randomize();
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
        if (RocketScene == null || _player == null || _upgrades == null)
            return;

        HomingRocket rocket = RocketScene.Instantiate<HomingRocket>();

        Node parent = GetTree().CurrentScene ?? GetTree().Root;
        parent.CallDeferred(Node.MethodName.AddChild, rocket);

        float baseBulletDamage = 25f * _upgrades.BulletDamageMultiplier;
        float rocketDamage = baseBulletDamage * 3f;

        rocket.GlobalPosition = _player.GlobalPosition;
        rocket.Init(forward, rocketDamage, 650f, _player);
    }
}