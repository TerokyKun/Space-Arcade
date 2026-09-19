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

    [Export] public float RocketChance = 0.08f;

    [Export] public int ExtraProjectiles = 0;

    [Export] public int CommanderDrones = 3;
    [Export] public float CommanderOrbitRadius = 48f;
    [Export] public float CommanderOrbitSpeed = 2.4f;
    [Export] public float CommanderShootCooldown = 0.35f;

    // ---- Дополнительная жизнь (Second Wind) ----
    [Export] public int ExtraLives = 0;
    [Export] public float ReviveHealthPercent = 0.5f;
    [Export] public float ReviveInvulnerability = 3f;

    // ---- Синий апгрейд: бонус за убийство (+% урона на время, стеки) ----
    [Export] public int KillBuffMaxStacks = 0;
    [Export] public float KillBuffDamagePerStack = 0f;
    [Export] public float KillBuffDuration = 3f;
    public int KillBuffStacks = 0;
    private float _killBuffTimer = 0f;

    // ---- Мифические эффекты ----
    [Export] public int SingularityLevel = 0;      // притяжение снарядов к врагам
    [Export] public int SplitCoreLevel = 0;        // расщепление пули при попадании
    [Export] public float SlowChance = 0f;         // Time Fracture: шанс замедлить врага
    [Export] public float SlowFactor = 0.4f;
    [Export] public float SlowDuration = 1.5f;
    [Export] public bool LastStandEnabled = false; // бонус урона при HP < 25%
    [Export] public float LastStandDamageBonus = 0.5f;

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

            case UpgradeKind.ExtraProjectiles:
                ExtraProjectiles += (int)upgrade.Value;
                break;

            case UpgradeKind.ExtraLife:
                ExtraLives += (int)upgrade.Value;
                break;

            case UpgradeKind.KillBuff:
                KillBuffMaxStacks += (int)Mathf.Max(1, upgrade.IntParam1);
                KillBuffDamagePerStack += upgrade.Value;
                KillBuffDuration = Mathf.Max(KillBuffDuration, upgrade.IntParam2 > 0 ? upgrade.IntParam2 : 3f);
                break;

            case UpgradeKind.Singularity:
                SingularityLevel += 1;
                break;

            case UpgradeKind.SplitCore:
                SplitCoreLevel += 1;
                break;

            case UpgradeKind.TimeFracture:
                SlowChance = Mathf.Min(SlowChance + upgrade.Value, 1f);
                SlowFactor = Mathf.Max(0f, SlowFactor);
                break;

            case UpgradeKind.LastStand:
                LastStandEnabled = true;
                LastStandDamageBonus = Mathf.Max(LastStandDamageBonus, upgrade.Value);
                break;
        }

        Changed?.Invoke();
    }

    // Расходует одну «дополнительную жизнь», если она есть.
    public bool TryRevive()
    {
        if (ExtraLives <= 0)
            return false;

        ExtraLives -= 1;
        Changed?.Invoke();
        return true;
    }

    // ---- Синий бафф: уведомление об убийстве врага ----

    public void NotifyKill()
    {
        if (KillBuffMaxStacks <= 0)
            return;

        KillBuffStacks = Mathf.Min(KillBuffMaxStacks, KillBuffStacks + 1);
        _killBuffTimer = KillBuffDuration;
        Changed?.Invoke();
    }

    public void TickBuff(float delta)
    {
        if (KillBuffStacks <= 0 || KillBuffMaxStacks <= 0)
            return;

        _killBuffTimer -= delta;
        if (_killBuffTimer <= 0f)
        {
            KillBuffStacks = 0;
            Changed?.Invoke();
        }
    }

    // Итоговый множитель урона игрока (базовый апгрейд + бафф + Last Stand).
    public float GetBulletDamageMultiplier(bool isLowHealth)
    {
        float m = BulletDamageMultiplier + KillBuffStacks * KillBuffDamagePerStack;

        if (LastStandEnabled && isLowHealth)
            m += LastStandDamageBonus;

        return m;
    }
}