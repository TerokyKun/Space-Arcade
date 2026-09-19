using Godot;
using System.Collections.Generic;

public static class CompanionCatalog
{
    public static string PathFor(CompanionId id) => id switch
    {
        CompanionId.MedBot => "res://resources/companions/companion_medbot.tres",
        CompanionId.Guardian => "res://resources/companions/companion_guardian.tres",
        CompanionId.FireCompanion => "res://resources/companions/companion_fire.tres",
        CompanionId.CombatDrone => "res://resources/companions/companion_drone.tres",
        _ => "res://resources/companions/companion_medbot.tres"
    };

    public static string IconFor(CompanionId id) => id switch
    {
        CompanionId.MedBot => "res://assets/placeholders/companion_medbot.svg",
        CompanionId.Guardian => "res://assets/placeholders/companion_guardian.svg",
        CompanionId.FireCompanion => "res://assets/placeholders/companion_fire.svg",
        CompanionId.CombatDrone => "res://assets/placeholders/companion_drone.svg",
        _ => "res://assets/placeholders/companion_medbot.svg"
    };

    public static IEnumerable<CompanionId> AllIds
    {
        get
        {
            yield return CompanionId.MedBot;
            yield return CompanionId.Guardian;
            yield return CompanionId.FireCompanion;
            yield return CompanionId.CombatDrone;
        }
    }

    public static CompanionDefinition Load(CompanionId id)
    {
        string path = PathFor(id);
        CompanionDefinition def = null;

        if (ResourceLoader.Exists(path))
            def = ResourceLoader.Load<CompanionDefinition>(path);

        return def ?? MakeDefault(id);
    }

    public static CompanionDefinition MakeDefault(CompanionId id) => id switch
    {
        CompanionId.Guardian => new CompanionDefinition
        {
            Id = CompanionId.Guardian,
            DisplayName = "Страж",
            Rarity = CompanionRarity.Uncommon,
            Icon = GD.Load<Texture2D>(IconFor(CompanionId.Guardian)),
            Description = "Энергетический щит, поглощающий часть урона игрока.",
            EffectText = "-8% получаемого урона",
            MaxHP = 150f,
            RegenRate = 4f,
            RegenDelay = 4f,
            FollowDistance = 60f,
            DamageReductionPercent = 0.08f
        },
        CompanionId.FireCompanion => new CompanionDefinition
        {
            Id = CompanionId.FireCompanion,
            DisplayName = "Огненный спутник",
            Rarity = CompanionRarity.Rare,
            Icon = GD.Load<Texture2D>(IconFor(CompanionId.FireCompanion)),
            Description = "Атакующий спутник, поджигающий ближайшего врага.",
            EffectText = "Лазер + горение 18/с на 4с",
            MaxHP = 120f,
            RegenRate = 3.5f,
            RegenDelay = 3f,
            FollowDistance = 100f,
            Cooldown = 1.6f,
            Damage = 18f,
            Range = 850f,
            BurnDamagePerSec = 18f,
            BurnDuration = 4f,
            BurnMaxStacks = 3
        },
        CompanionId.CombatDrone => new CompanionDefinition
        {
            Id = CompanionId.CombatDrone,
            DisplayName = "Боевой дрон",
            Rarity = CompanionRarity.Epic,
            Icon = GD.Load<Texture2D>(IconFor(CompanionId.CombatDrone)),
            Description = "Дальнобойный дрон, ведущий очереди по врагам.",
            EffectText = "Очередь 5×8 через 2.2с",
            MaxHP = 180f,
            RegenRate = 5f,
            RegenDelay = 3f,
            FollowDistance = 120f,
            Cooldown = 2.2f,
            Damage = 8f,
            Range = 950f,
            ProjectileCount = 1,
            Spread = 0.1f,
            BurstCount = 5,
            BurstGap = 0.09f,
            ProjectileSpeed = 1000f
        },
        _ => new CompanionDefinition
        {
            Id = CompanionId.MedBot,
            DisplayName = "Мед-Бот",
            Rarity = CompanionRarity.Common,
            Icon = GD.Load<Texture2D>(IconFor(CompanionId.MedBot)),
            Description = "Пассивный дрон. Поддерживает здоровье игрока в бою.",
            EffectText = "+1% HP каждые 3 сек",
            MaxHP = 90f,
            RegenRate = 4f,
            RegenDelay = 3f,
            FollowDistance = 55f,
            Cooldown = 3f,
            HealPercentPerTick = 0.01f
        }
    };
}