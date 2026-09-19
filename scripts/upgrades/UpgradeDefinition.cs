using Godot;

[GlobalClass]
public partial class UpgradeDefinition : Resource
{
    [Export] public string Id = "";

    [Export] public string Title = "Upgrade";
    [Export(PropertyHint.MultilineText)] public string Description = "";
    [Export] public Texture2D Icon;

    [Export] public UpgradeKind Kind = UpgradeKind.MoveSpeed;

    [Export] public PlayerClassType ClassChoice = PlayerClassType.None;

    [Export] public float Value = 0.15f;

    // Доп. параметры для апгрейдов со сложной механикой (стек/длительность).
    [Export] public int IntParam1 = 0;
    [Export] public int IntParam2 = 0;

    [Export] public float Weight = 1f;

    [Export] public UpgradeRarity Rarity = UpgradeRarity.Common;
}