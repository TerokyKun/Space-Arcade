using Godot;

public partial class SpaceBackground : Node2D
{
    [Export] public NodePath FollowTargetPath { get; set; }

    [Export] public int SectorSize = 1200;
    [Export] public int GridRadius = 2;
    [Export] public int StarsPerSector = 22;
    [Export] public int NebulaPerSector = 1;

    [Export] public float MinStarSize = 1f;
    [Export] public float MaxStarSize = 2.5f;

    [Export] public Color StarColor = new Color(1f, 1f, 1f, 0.9f);
    [Export] public Color DimStarColor = new Color(0.75f, 0.85f, 1f, 0.45f);

    private Node2D _followTarget;
    private Vector2I _currentSector = new Vector2I(int.MinValue, int.MinValue);

    public override void _Ready()
    {
        ZIndex = -100;
        ResolveTarget();
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_followTarget == null || !GodotObject.IsInstanceValid(_followTarget))
            ResolveTarget();

        Vector2 center = GetCenter();
        Vector2I sector = new Vector2I(
            Mathf.FloorToInt(center.X / SectorSize),
            Mathf.FloorToInt(center.Y / SectorSize)
        );

        if (sector != _currentSector)
        {
            _currentSector = sector;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        Vector2 center = GetCenter();
        Vector2I sector = new Vector2I(
            Mathf.FloorToInt(center.X / SectorSize),
            Mathf.FloorToInt(center.Y / SectorSize)
        );

        for (int sy = sector.Y - GridRadius; sy <= sector.Y + GridRadius; sy++)
        {
            for (int sx = sector.X - GridRadius; sx <= sector.X + GridRadius; sx++)
            {
                DrawSector(sx, sy);
            }
        }
    }

    private void ResolveTarget()
    {
        _followTarget = null;

        if (FollowTargetPath != null && !FollowTargetPath.IsEmpty)
        {
            _followTarget = GetNodeOrNull<Node2D>(FollowTargetPath);
            if (_followTarget != null)
                return;
        }

        foreach (Node node in GetTree().GetNodesInGroup("player"))
        {
            if (node is Node2D n2d)
            {
                _followTarget = n2d;
                return;
            }
        }
    }

    private Vector2 GetCenter()
    {
        if (_followTarget != null && GodotObject.IsInstanceValid(_followTarget))
            return _followTarget.GlobalPosition;

        return GlobalPosition;
    }

    private void DrawSector(int sx, int sy)
    {
        unchecked
        {
            int seed = sx * 73856093 ^ sy * 19349663;
            var rng = new RandomNumberGenerator();
            rng.Seed = (ulong)(uint)seed;

            Vector2 origin = new Vector2(sx * SectorSize, sy * SectorSize);

            for (int i = 0; i < NebulaPerSector; i++)
            {
                Vector2 nebulaPos = origin + new Vector2(
                    rng.RandfRange(0, SectorSize),
                    rng.RandfRange(0, SectorSize)
                );

                float nebulaRadius = rng.RandfRange(120f, 240f);
                Color nebulaColor = new Color(
                    rng.RandfRange(0.35f, 0.7f),
                    rng.RandfRange(0.35f, 0.8f),
                    1f,
                    rng.RandfRange(0.03f, 0.08f)
                );

                DrawCircle(nebulaPos, nebulaRadius, nebulaColor);
            }

            for (int i = 0; i < StarsPerSector; i++)
            {
                Vector2 starPos = origin + new Vector2(
                    rng.RandfRange(0, SectorSize),
                    rng.RandfRange(0, SectorSize)
                );

                float size = rng.RandfRange(MinStarSize, MaxStarSize);
                Color color = rng.Randf() > 0.8f ? DimStarColor : StarColor;

                DrawCircle(starPos, size, color);
            }
        }
    }
}