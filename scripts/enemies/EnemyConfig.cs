using Godot;

public enum EnemyKind
{
    Normal = 0,
    Fast = 1,
    Tank = 2,
    Ranged = 3,
    Elite = 4,
    Boss = 5
}

[GlobalClass]
public partial class EnemyConfig : Resource
{
    [Export] public EnemyKind Kind = EnemyKind.Normal;
    [Export] public string ConfigName = "Normal";

    [Export] public Texture2D Icon;
    [Export] public float ScaleMultiplier = 1f;

    [Export] public float MaxHP = 100f;
    [Export] public float Acceleration = 700f;
    [Export] public float MaxSpeed = 100f;
    [Export] public float Friction = 500f;
    [Export] public float TurnSpeed = 5f;

    [Export] public float FireCooldown = 3f;
    [Export] public float StopDistance = 150f;

    [Export] public float BulletSpeed = 450f;
    [Export] public float BulletDamage = 10f;

    [Export] public int DropMin = 2;
    [Export] public int DropMax = 6;

    [Export(PropertyHint.Range, "0,200,1")] public int XpReward = 8;

    [Export] public bool IsRanged = false;
    [Export] public bool IsBoss = false;
}

public static class EnemyCatalog
{
    public static string PathFor(EnemyKind kind) => kind switch
    {
        EnemyKind.Fast => "res://resources/enemies/enemy_fast.tres",
        EnemyKind.Tank => "res://resources/enemies/enemy_tank.tres",
        EnemyKind.Ranged => "res://resources/enemies/enemy_ranged.tres",
        EnemyKind.Elite => "res://resources/enemies/enemy_elite.tres",
        EnemyKind.Boss => "res://resources/enemies/enemy_boss.tres",
        _ => "res://resources/enemies/enemy_normal.tres"
    };

    public static string IconFor(EnemyKind kind) => kind switch
    {
        EnemyKind.Fast => "res://assets/placeholders/enemy_fast.svg",
        EnemyKind.Tank => "res://assets/placeholders/enemy_tank.svg",
        EnemyKind.Ranged => "res://assets/placeholders/enemy_ranged.svg",
        EnemyKind.Elite => "res://assets/placeholders/enemy_elite.svg",
        EnemyKind.Boss => "res://assets/placeholders/enemy_boss.svg",
        _ => "res://assets/placeholders/enemy_normal.svg"
    };

    public static EnemyConfig Load(EnemyKind kind)
    {
        string path = PathFor(kind);
        EnemyConfig config = null;

        if (ResourceLoader.Exists(path))
            config = ResourceLoader.Load<EnemyConfig>(path);

        return config ?? MakeDefault(kind);
    }

    public static EnemyConfig MakeDefault(EnemyKind kind)
    {
        var config = new EnemyConfig
        {
            Kind = kind,
            Icon = GD.Load<Texture2D>(IconFor(kind))
        };

        switch (kind)
        {
            case EnemyKind.Fast:
                config.ConfigName = "Fast";
                config.ScaleMultiplier = 0.8f;
                config.MaxHP = 55f;
                config.MaxSpeed = 220f;
                config.Acceleration = 900f;
                config.Friction = 500f;
                config.TurnSpeed = 7f;
                config.FireCooldown = 4.5f;
                config.StopDistance = 140f;
                config.BulletSpeed = 420f;
                config.BulletDamage = 6f;
                config.DropMin = 1;
                config.DropMax = 4;
                break;
            case EnemyKind.Tank:
                config.ConfigName = "Tank";
                config.ScaleMultiplier = 1.35f;
                config.MaxHP = 400f;
                config.MaxSpeed = 55f;
                config.Acceleration = 350f;
                config.Friction = 600f;
                config.TurnSpeed = 2.5f;
                config.FireCooldown = 2.2f;
                config.StopDistance = 220f;
                config.BulletSpeed = 380f;
                config.BulletDamage = 18f;
                config.DropMin = 4;
                config.DropMax = 10;
                break;
            case EnemyKind.Ranged:
                config.ConfigName = "Ranged";
                config.ScaleMultiplier = 0.9f;
                config.MaxHP = 80f;
                config.MaxSpeed = 80f;
                config.Acceleration = 450f;
                config.Friction = 500f;
                config.TurnSpeed = 3f;
                config.FireCooldown = 1.6f;
                config.StopDistance = 360f;
                config.BulletSpeed = 600f;
                config.BulletDamage = 12f;
                config.DropMin = 2;
                config.DropMax = 5;
                config.IsRanged = true;
                break;
            case EnemyKind.Elite:
                config.ConfigName = "Elite";
                config.ScaleMultiplier = 1.4f;
                config.MaxHP = 650f;
                config.MaxSpeed = 115f;
                config.Acceleration = 620f;
                config.Friction = 550f;
                config.TurnSpeed = 4f;
                config.FireCooldown = 1.2f;
                config.StopDistance = 200f;
                config.BulletSpeed = 520f;
                config.BulletDamage = 22f;
                config.DropMin = 5;
                config.DropMax = 12;
                break;
            case EnemyKind.Boss:
                config.ConfigName = "Boss";
                config.ScaleMultiplier = 2f;
                config.MaxHP = 3000f;
                config.MaxSpeed = 70f;
                config.Acceleration = 350f;
                config.Friction = 600f;
                config.TurnSpeed = 2f;
                config.FireCooldown = 0.8f;
                config.StopDistance = 260f;
                config.BulletSpeed = 430f;
                config.BulletDamage = 24f;
                config.DropMin = 10;
                config.DropMax = 20;
                config.IsBoss = true;
                break;
            default:
                config.ConfigName = "Normal";
                config.MaxHP = 100f;
                config.MaxSpeed = 100f;
                config.Acceleration = 700f;
                config.Friction = 500f;
                config.TurnSpeed = 5f;
                config.FireCooldown = 3f;
                config.StopDistance = 150f;
                config.BulletSpeed = 450f;
                config.BulletDamage = 10f;
                config.DropMin = 2;
                config.DropMax = 6;
                break;
        }

        return config;
    }
}