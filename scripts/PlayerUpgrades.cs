using Godot;
using System;

public partial class PlayerUpgrades : Node
{
    [Export] public PlayerClassType SelectedClass = PlayerClassType.None;

    [Export] public float HealthMultiplier = 1f;
    [Export] public float FireRateMultiplier = 1f;
    [Export] public float MoveSpeedMultiplier = 1f;
    [Export] public float BulletDamageMultiplier = 1f;
    [Export] public float BulletSizeMultiplier = 1f;

    [Export] public float TeleportCooldown = 5f;
    [Export] public float TeleportInvulnerability = 2f;

    [Export] public float RocketChance = 0.05f;

    [Export] public int CommanderDrones = 3;
    [Export] public float CommanderOrbitRadius = 48f;
    [Export] public float CommanderOrbitSpeed = 2.4f;
    [Export] public float CommanderShootCooldown = 0.35f;

    public event Action Changed;

    public bool HasClass => SelectedClass != PlayerClassType.None;

    public void ApplyUpgrade(UpgradeDefinition upgrade)
    {
        if (upgrade == null)
            return;

        switch (upgrade.Kind)
        {
            case UpgradeKind.ChooseClass:
                SelectedClass = upgrade.ClassChoice;
                break;

            case UpgradeKind.MaxHP:
                HealthMultiplier += upgrade.Value;
                break;

            case UpgradeKind.FireRate:
                FireRateMultiplier += upgrade.Value;
                break;

            case UpgradeKind.MoveSpeed:
                MoveSpeedMultiplier += upgrade.Value;
                break;

            case UpgradeKind.BulletDamage:
                BulletDamageMultiplier += upgrade.Value;
                break;

            case UpgradeKind.BulletSize:
                BulletSizeMultiplier += upgrade.Value;
                break;
        }

        Changed?.Invoke();
    }
}