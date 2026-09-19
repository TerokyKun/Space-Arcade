using Godot;

// Редкость компаньонов: 6 ступеней с цветом, русским названием и весом
// для взвешенного выбора на карточках награды.
public enum CompanionRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Mythic = 5
}

public static class CompanionRarityInfo
{
    public static string DisplayName(this CompanionRarity rarity) => rarity switch
    {
        CompanionRarity.Common => "Обычный",
        CompanionRarity.Uncommon => "Необычный",
        CompanionRarity.Rare => "Редкий",
        CompanionRarity.Epic => "Эпический",
        CompanionRarity.Legendary => "Легендарный",
        CompanionRarity.Mythic => "Мифический",
        _ => "Обычный"
    };

    public static Color Color(this CompanionRarity rarity) => rarity switch
    {
        CompanionRarity.Common => new Color(0.72f, 0.74f, 0.78f),
        CompanionRarity.Uncommon => new Color(0.35f, 0.85f, 0.4f),
        CompanionRarity.Rare => new Color(0.3f, 0.65f, 1f),
        CompanionRarity.Epic => new Color(0.7f, 0.42f, 1f),
        CompanionRarity.Legendary => new Color(1f, 0.82f, 0.2f),
        CompanionRarity.Mythic => new Color(1f, 0.25f, 0.25f),
        _ => Colors.White
    };

    // Вес для случайного выбора: редкие — реже.
    public static float Weight(this CompanionRarity rarity) => rarity switch
    {
        CompanionRarity.Common => 40f,
        CompanionRarity.Uncommon => 22f,
        CompanionRarity.Rare => 11f,
        CompanionRarity.Epic => 5f,
        CompanionRarity.Legendary => 1.6f,
        CompanionRarity.Mythic => 0.8f,
        _ => 1f
    };
}