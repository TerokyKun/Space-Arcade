using Godot;

public partial class Health : Node
{
    [Signal] public delegate void HealthChangedEventHandler(float currentHp, float maxHp);
    [Signal] public delegate void DiedEventHandler();

    public enum TeamType
    {
        Player,
        Ally,
        Enemy
    }

    [Export] public TeamType Team = TeamType.Enemy;
    [Export] public float MaxHP = 100f;
    [Export] public bool AutoFreeOwner = true;

    public float CurrentHP { get; private set; }

    private bool _isDead = false;

    public override void _Ready()
    {
        MaxHP = Mathf.Max(1f, MaxHP);
        CurrentHP = MaxHP;

        AddToGroup("damageable");

        EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);
    }

    public void ApplyDamage(float damage)
    {
        if (damage <= 0f || _isDead || CurrentHP <= 0f)
            return;

        CurrentHP = Mathf.Max(0f, CurrentHP - damage);
        EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);

        Node parent = GetParent();
        if (parent != null)
            GD.Print($"{parent.Name} получил урон: {damage}, HP: {CurrentHP}/{MaxHP}");

        if (CurrentHP <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || _isDead || CurrentHP <= 0f)
            return;

        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0f, MaxHP);
        EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);
    }

    public void SetMaxHealth(float value, bool refill = true)
    {
        MaxHP = Mathf.Max(1f, value);

        if (refill)
            CurrentHP = MaxHP;
        else
            CurrentHP = Mathf.Clamp(CurrentHP, 0f, MaxHP);

        EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);
    }

    public void Die()
    {
        if (_isDead)
            return;

        _isDead = true;

        Node parent = GetParent();
        if (parent != null)
            GD.Print($"💀 {parent.Name} умер");

        EmitSignal(SignalName.Died);

        if (AutoFreeOwner)
        {
            Node ownerNode = GetParent();
            if (ownerNode != null && GodotObject.IsInstanceValid(ownerNode))
                ownerNode.QueueFree();
        }
    }
}