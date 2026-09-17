using Godot;

public partial class CommanderDrone : Area2D
{
    [Export] public PackedScene DroneShotScene;
    [Export] public uint LineOfSightMask = 1;

    private Player _player;
    private PlayerUpgrades _upgrades;
    private int _index;
    private int _count;

    private float _angleOffset;
    private float _shootTimer = 0f;

    public void Init(Player player, int index, int count, PlayerUpgrades upgrades)
    {
        _player = player;
        _index = index;
        _count = count;
        _upgrades = upgrades;
        _angleOffset = (Mathf.Tau / Mathf.Max(1, count)) * index;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null || _upgrades == null)
            return;

        if (_upgrades.SelectedClass != PlayerClassType.Commander)
            return;

        float d = (float)delta;
        float t = Time.GetTicksMsec() / 1000f;
        float angle = t * _upgrades.CommanderOrbitSpeed + _angleOffset;

        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _upgrades.CommanderOrbitRadius;
        GlobalPosition = _player.GlobalPosition + offset;

        _shootTimer -= d;
        if (_shootTimer <= 0f)
        {
            TryShootAtEnemy();
            _shootTimer = _upgrades.CommanderShootCooldown;
        }
    }

    private void TryShootAtEnemy()
    {
        Node2D target = FindVisibleEnemy();
        if (target == null)
            return;

        Vector2 dir = (target.GlobalPosition - GlobalPosition).Normalized();
        Shoot(dir);
    }

    private Node2D FindVisibleEnemy()
    {
        Node2D best = null;
        float bestDist = float.MaxValue;

        foreach (Node node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is not Node2D enemy2D)
                continue;

            if (!HasLineOfSight(enemy2D))
                continue;

            float dist = GlobalPosition.DistanceSquaredTo(enemy2D.GlobalPosition);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = enemy2D;
            }
        }

        return best;
    }

    private bool HasLineOfSight(Node2D target)
    {
        var space = GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, target.GlobalPosition, LineOfSightMask);
        query.Exclude = new Godot.Collections.Array<Rid>();

        var result = space.IntersectRay(query);
        if (result.Count == 0)
            return false;

        if (!result.ContainsKey("collider"))
            return false;

        var collider = result["collider"].AsGodotObject();
        return collider == target;
    }

    private void Shoot(Vector2 direction)
    {
        if (DroneShotScene == null)
            return;

        var shot = DroneShotScene.Instantiate<CommanderShot>();

        Node parent = GetTree().CurrentScene ?? GetTree().Root;
        parent.AddChild(shot);

        float damage = 10f * (_upgrades != null ? _upgrades.BulletDamageMultiplier : 1f);

        shot.GlobalPosition = GlobalPosition;
        shot.Init(direction, damage, 1000f, _player);
    }
}