using Godot;

public partial class AbilityHud : CanvasLayer
{
    private Label _classLabel;
    private Label _teleportLabel;
    private Label _rocketLabel;
    private Label _commanderLabel;

    public override void _Ready()
    {
        _classLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/ClassLabel");
        _teleportLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/TeleportLabel");
        _rocketLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/RocketLabel");
        _commanderLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/CommanderLabel");
    }

    public override void _Process(double delta)
    {
        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player == null)
            return;

        var upgrades = player.GetNodeOrNull<PlayerUpgrades>("PlayerUpgrades");
        if (upgrades == null)
            return;

        if (_classLabel != null)
            _classLabel.Text = upgrades.HasClass
                ? $"Класс: {upgrades.SelectedClass}"
                : "Класс: не выбран";

        if (_teleportLabel != null)
        {
            bool enabled = upgrades.SelectedClass == PlayerClassType.Teleporter;
            _teleportLabel.Visible = enabled;

            if (enabled)
            {
                var teleport = player.GetNodeOrNull<TeleportAbility>("TeleportAbility");
                if (teleport != null)
                {
                    _teleportLabel.Text = teleport.IsReady
                        ? $"Телепорт: ГОТОВ (Shift)"
                        : $"Телепорт: {teleport.CooldownRemaining:0.0} сек";
                }
            }
        }

        if (_rocketLabel != null)
        {
            bool enabled = upgrades.SelectedClass == PlayerClassType.Rocketman;
            _rocketLabel.Visible = enabled;

            if (enabled)
            {
                float chance = Mathf.Clamp(upgrades.RocketChance * 100f, 0f, 100f);
                float damageMult = upgrades.BulletDamageMultiplier;
                _rocketLabel.Text = $"Ракетчик: шанс {chance:0}% | урон ×3 × {damageMult:0.##}";
            }
        }

        if (_commanderLabel != null)
        {
            bool enabled = upgrades.SelectedClass == PlayerClassType.Commander;
            _commanderLabel.Visible = enabled;

            if (enabled)
            {
                var manager = player.GetNodeOrNull<CommanderDroneManager>("CommanderDroneManager");
                int count = manager != null ? manager.ActiveDroneCount : 0;
                _commanderLabel.Text = $"Коммандер: дронов {count}/{upgrades.CommanderDrones}";
            }
        }
    }
}