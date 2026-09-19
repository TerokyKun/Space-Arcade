using Godot;

// Редкость улучшений: 6 ступеней. Редкость влияет на игровой эффект
// (мифические меняют правила игры), а не только на цвет/цифры.
public enum UpgradeRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Mythic = 5
}

public static class UpgradeRarityInfo
{
    public static string DisplayName(this UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => "Обычное",
        UpgradeRarity.Uncommon => "Необычное",
        UpgradeRarity.Rare => "Редкое",
        UpgradeRarity.Epic => "Эпическое",
        UpgradeRarity.Legendary => "Легендарное",
        UpgradeRarity.Mythic => "Мифическое",
        _ => "Обычное"
    };

    public static Color Color(this UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => new Color(0.9f, 0.9f, 0.9f),
        UpgradeRarity.Uncommon => new Color(0.35f, 0.85f, 0.4f),
        UpgradeRarity.Rare => new Color(0.45f, 0.72f, 1f),
        UpgradeRarity.Epic => new Color(0.72f, 0.45f, 1f),
        UpgradeRarity.Legendary => new Color(1f, 0.82f, 0.2f),
        UpgradeRarity.Mythic => new Color(1f, 0.32f, 0.32f),
        _ => Colors.White
    };

    // Префикс-маркер для кнопки апгрейда (короткий, без лишнего текста).
    public static string Tag(this UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Epic => "◆ ",
        UpgradeRarity.Legendary => "★ ",
        UpgradeRarity.Mythic => "☀ ",
        UpgradeRarity.Rare => "● ",
        UpgradeRarity.Uncommon => "▪ ",
        _ => ""
    };

    // Множитель вероятности появления: чем редче — тем ниже шанс.
    public static float WeightMultiplier(this UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => 3.0f,
        UpgradeRarity.Uncommon => 2.4f,
        UpgradeRarity.Rare => 2.0f,
        UpgradeRarity.Epic => 1.5f,
        UpgradeRarity.Legendary => 0.9f,
        UpgradeRarity.Mythic => 0.5f,
        _ => 1f
    };
}