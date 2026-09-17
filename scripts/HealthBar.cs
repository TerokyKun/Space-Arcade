using Godot;

public partial class HealthBar : ProgressBar
{
    [Export] public NodePath HealthPath;

    private Health _health;

    public override void _Ready()
    {
        ResolveHealth();

        if (_health == null || !GodotObject.IsInstanceValid(_health))
        {
            GD.PushError("HealthBar: Health не найден. Укажи HealthPath в инспекторе.");
            return;
        }

        MaxValue = _health.MaxHP;
        Value = _health.CurrentHP;

        _health.HealthChanged += OnHealthChanged;
        _health.Died += OnDied;
    }

    public override void _ExitTree()
    {
        if (_health != null && GodotObject.IsInstanceValid(_health))
        {
            _health.HealthChanged -= OnHealthChanged;
            _health.Died -= OnDied;
        }
    }

    private void ResolveHealth()
    {
        if (HealthPath != null && !HealthPath.IsEmpty)
        {
            _health = GetNodeOrNull<Health>(HealthPath);
            if (_health != null)
                return;
        }

        _health = FindHealthInParents(GetParent());
        if (_health != null)
            return;

        Node root = GetTree().CurrentScene;
        if (root != null)
            _health = FindFirstHealth(root);
    }

    private Health FindHealthInParents(Node node)
    {
        Node current = node;

        while (current != null && GodotObject.IsInstanceValid(current))
        {
            Health health = current.GetNodeOrNull<Health>("Health");
            if (health != null)
                return health;

            current = current.GetParent();
        }

        return null;
    }

    private Health FindFirstHealth(Node node)
    {
        if (node is Health healthNode)
            return healthNode;

        foreach (Node child in node.GetChildren())
        {
            Health found = FindFirstHealth(child);
            if (found != null)
                return found;
        }

        return null;
    }

    private void OnHealthChanged(float currentHp, float maxHp)
    {
        MaxValue = maxHp;
        Value = currentHp;
    }

    private void OnDied()
    {
        Value = 0f;
    }
}