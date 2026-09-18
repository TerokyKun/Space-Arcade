using Godot;
using System.Collections.Generic;

public partial class EnemyBeaconSystem : CanvasLayer
{
    [Export] public PackedScene BeaconScene;

    // Один и тот же угол = одно направление: враги в этом секторе сливаются в один кластер.
    [Export(PropertyHint.Range, "1,45,0.5")] public float ClusterAngle = 14f;

    // Отступ маркера от краёв экрана (в пикселях).
    [Export(PropertyHint.Range, "0,120,1")] public float EdgeInset = 26f;

    // Расстояние (px) за краем экрана, на котором маркер полностью проявляется:
    // враг далеко = полный, враг у самой границы = полупрозрачный, войдя в кадр — исчезает.
    [Export(PropertyHint.Range, "20,600,10")] public float FadeRangePx = 150f;

    private readonly List<EnemyBeacon> _beacons = new();
    private Node2D _player;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        if (BeaconScene == null)
            return;

        if (ResolvePlayer() == null)
            return;

        Rect2 visiblePx = GetViewport().GetVisibleRect();
        if (visiblePx.Size.X <= 1f || visiblePx.Size.Y <= 1f)
            return;

        List<Cluster> clusters = BuildClusters(visiblePx);
        clusters.Sort((a, b) => a.Angle.CompareTo(b.Angle));

        int i = 0;
        foreach (Cluster cluster in clusters)
        {
            EnemyBeacon beacon = GetBeacon(i++);

            // Прогресс 0..1: 1 — враг далеко за кадром (полный маркер),
            // 0 — враг у края экрана (маленький, полупрозрачный, готов исчезнуть).
            float progress = Mathf.Clamp(cluster.MinEdgeDistPx / Mathf.Max(1f, FadeRangePx), 0f, 1f);

            beacon.ShowAt(cluster.PositionPx, cluster.Angle, cluster.Count, cluster.Boss, progress);
        }

        for (; i < _beacons.Count; i++)
            _beacons[i].HideBeacon();
    }

    private Node2D ResolvePlayer()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        return _player;
    }

    private List<Cluster> BuildClusters(Rect2 visiblePx)
    {
        var clusters = new List<Cluster>();
        float clusterAngleRad = Mathf.DegToRad(ClusterAngle);

        float halfW = visiblePx.Size.X * 0.5f;
        float halfH = visiblePx.Size.Y * 0.5f;
        float availW = Mathf.Max(1f, halfW - EdgeInset);
        float availH = Mathf.Max(1f, halfH - EdgeInset);
        Vector2 centerPx = visiblePx.GetCenter();

        var processed = new HashSet<ulong>();

        // Боссы обрабатываются отдельно и имеют приоритет (крупный индикатор).
        foreach (Node node in GetTree().GetNodesInGroup("boss"))
            Collect(node, true);

        foreach (Node node in GetTree().GetNodesInGroup("enemy"))
            Collect(node, false);

        return clusters;

        void Collect(Node node, bool boss)
        {
            if (node is not Node2D enemy2D)
                return;

            if (!GodotObject.IsInstanceValid(enemy2D) || enemy2D.IsQueuedForDeletion())
                return;

            if (!processed.Add(enemy2D.GetInstanceId()))
                return;

            // Мировая позиция врага → координаты экрана (с учётом камеры: позиция, zoom, поворот).
            Vector2 enemyPx = enemy2D.GetGlobalTransformWithCanvas().Origin;

            // Знаковое расстояние до прямоугольника экрана (отрицательное — враг внутри).
            float edgeDist = SignedDistanceToRect(visiblePx, enemyPx);
            if (edgeDist <= 0f)
                return; // враг на экране — маркер не нужен

            Vector2 diff = enemyPx - centerPx;
            float length = diff.Length();
            if (length < 0.001f)
                return;

            Vector2 unit = diff / length;
            float angle = Mathf.Atan2(diff.Y, diff.X);

            // Точка на границе экрана: пересечение направления с внутренним прямоугольником.
            float sx = availW / Mathf.Abs(unit.X);
            float sy = availH / Mathf.Abs(unit.Y);
            float t = Mathf.Min(sx, sy);
            Vector2 posPx = centerPx + unit * t;

            int index = FindCluster(clusters, unit, angle, clusterAngleRad);
            if (index < 0)
            {
                clusters.Add(new Cluster(posPx, unit, boss, edgeDist));
            }
            else
            {
                Cluster c = clusters[index];
                c.PositionSum += posPx;
                c.DirSum += unit;
                c.Count += 1;
                c.Boss = c.Boss || boss;
                c.MinEdgeDistPx = Mathf.Min(c.MinEdgeDistPx, edgeDist);
                clusters[index] = c;
            }
        }
    }

    private static int FindCluster(
        List<Cluster> clusters,
        Vector2 unit,
        float angle,
        float clusterAngleRad
    )
    {
        for (int i = 0; i < clusters.Count; i++)
        {
            Cluster c = clusters[i];

            float raw = c.Angle - angle;
            float deltaAngle = Mathf.Abs(Mathf.Atan2(Mathf.Sin(raw), Mathf.Cos(raw)));

            if (deltaAngle <= clusterAngleRad)
                return i;
        }

        return -1;
    }

    private static float SignedDistanceToRect(Rect2 rect, Vector2 p)
    {
        float dx = Mathf.Max(0f,
            Mathf.Max(rect.Position.X - p.X, p.X - (rect.Position.X + rect.Size.X)));
        float dy = Mathf.Max(0f,
            Mathf.Max(rect.Position.Y - p.Y, p.Y - (rect.Position.Y + rect.Size.Y)));

        // Точка вне прямоугольника — евклидова дистанция до него.
        if (dx > 0f || dy > 0f)
            return new Vector2(dx, dy).Length();

        // Точка внутри — отрицательное расстояние до ближайшей границы.
        float inX = Mathf.Min(p.X - rect.Position.X,
            rect.Position.X + rect.Size.X - p.X);
        float inY = Mathf.Min(p.Y - rect.Position.Y,
            rect.Position.Y + rect.Size.Y - p.Y);
        return -Mathf.Min(inX, inY);
    }

    private EnemyBeacon GetBeacon(int index)
    {
        while (_beacons.Count <= index)
        {
            EnemyBeacon beacon = BeaconScene.Instantiate<EnemyBeacon>();
            AddChild(beacon);
            _beacons.Add(beacon);
        }

        return _beacons[index];
    }

    private struct Cluster
    {
        public Vector2 PositionSum;
        public Vector2 DirSum;
        public int Count;
        public bool Boss;

        // Минимальное расстояние до границы экрана среди врагов кластера:
        // ближайший враг определяет прозрачность всего кластера.
        public float MinEdgeDistPx;

        public Vector2 PositionPx => PositionSum / (float)Count;

        // Направление кластера — усреднённый вектор направлений всех врагов.
        public float Angle => Mathf.Atan2(DirSum.Y, DirSum.X);

        public Cluster(Vector2 posPx, Vector2 dirUnit, bool boss, float edgeDistPx)
        {
            PositionSum = posPx;
            DirSum = dirUnit;
            Count = 1;
            Boss = boss;
            MinEdgeDistPx = edgeDistPx;
        }
    }
}