using Godot;

public partial class TeleportAbility : Node
{
    [ExportGroup("Settings")]
    [Export] public float DefaultCooldown = 5f;
    [Export] public float DefaultInvulnerability = 2f;
    [Export] public float TeleportDistance = 220f;

    private Player _player;
    private PlayerUpgrades _upgrades;
    private float _cooldownTimer = 0f;

    public bool IsReady => _cooldownTimer <= 0f;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);
    public float CooldownDuration => _upgrades != null ? _upgrades.TeleportCooldown : DefaultCooldown;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _player = GetParent<Player>();
        _upgrades = _player?.GetNodeOrNull<PlayerUpgrades>("PlayerUpgrades");
    }

    public void Init(Player player)
    {
        _player = player;
        _upgrades ??= _player?.GetNodeOrNull<PlayerUpgrades>("PlayerUpgrades");
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= d;

        if (_player == null)
            return;

        // Способность активна только после выбора класса Телепортёр.
        bool isTeleporter = _upgrades != null && _upgrades.SelectedClass == PlayerClassType.Teleporter;
        if (!isTeleporter)
            return;

        if (Input.IsActionJustPressed("tp") && IsReady)
            TryTeleport();
    }

    private void TryTeleport()
    {
        if (_player == null)
            return;

        Vector2 forward = Vector2.Up.Rotated(_player.Rotation);
        _player.GlobalPosition += forward * TeleportDistance;

        float invuln = _upgrades != null ? _upgrades.TeleportInvulnerability : DefaultInvulnerability;
        float cooldown = _upgrades != null ? _upgrades.TeleportCooldown : DefaultCooldown;

        _player.GrantInvulnerability(invuln);
        _cooldownTimer = cooldown;
    }
}