using Godot;

public enum CompanionId
{
    MedBot = 0,
    Guardian = 1,
    FireCompanion = 2,
    CombatDrone = 3
}

[GlobalClass]
public partial class CompanionDefinition : Resource
{
    [Export] public CompanionId Id = CompanionId.MedBot;

    [Export] public string DisplayName = "Мед-Бот";
    [Export] public CompanionRarity Rarity = CompanionRarity.Common;

    [Export] public Texture2D Icon;

    // Короткие подписи на карточке награды.
    [Export(PropertyHint.MultilineText)] public string Description = "";
    [Export(PropertyHint.MultilineText)] public string EffectText = "";

    // ---- Боевые тайминги/эффект (активные спутники) ----
    [Export(PropertyHint.Range, "0.2,20,0.1")] public float Cooldown = 3f;
    [Export(PropertyHint.Range, "1,200,1")] public float Damage = 15f;
    [Export(PropertyHint.Range, "100,3000,50")] public float Range = 900f;
    [Export(PropertyHint.Range, "0.02,0.5,0.01")] public float HealPercentPerTick = 0.01f;
    [Export(PropertyHint.Range, "0.02,0.5,0.01")] public float DamageReductionPercent = 0.08f;
    [Export(PropertyHint.Range, "1,8,1")] public int ProjectileCount = 1;
    [Export(PropertyHint.Range, "0,1,0.05")] public float Spread = 0f;

    // ---- Выживаемость: спутник — самостоятельная сущность со своим HP и регеном ----
    [Export(PropertyHint.Range, "20,1000,5")] public float MaxHP = 120f;
    // HP в секунду, реген включается через RegenDelay секунд без урона.
    [Export(PropertyHint.Range, "0,20,0.5")] public float RegenRate = 3f;
    [Export(PropertyHint.Range, "0,15,0.5")] public float RegenDelay = 3f;

    // ---- Движение: мягкое следование за игроком (не строем) ----
    [Export(PropertyHint.Range, "50,300,5")] public float FollowDistance = 90f;
    [Export(PropertyHint.Range, "1,10,0.5")] public float MoveSmoothing = 4f;

    // ---- Атака «Огненного спутника»: лазерный луч + горение (DoT) ----
    [Export(PropertyHint.Range, "0,80,1")] public float BurnDamagePerSec = 0f;
    [Export(PropertyHint.Range, "0,12,0.5")] public float BurnDuration = 0f;
    [Export(PropertyHint.Range, "1,6,1")] public int BurnMaxStacks = 3;

    // ---- Атака «Боевого дрона»: очередь пуль ----
    [Export(PropertyHint.Range, "1,12,1")] public int BurstCount = 1;
    [Export(PropertyHint.Range, "0.02,0.6,0.01")] public float BurstGap = 0.1f;
    [Export(PropertyHint.Range, "200,2000,50")] public float ProjectileSpeed = 950f;
}