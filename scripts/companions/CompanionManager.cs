using Godot;
using System.Collections.Generic;

// Менеджер компаньонов: API получения/удаления и слой эффектов.
// Сами спутники — автономные сущности CompanionRuntime (со своим HP/регеном),
// которые создаются и освобождаются здесь.
public partial class CompanionManager : Node
{
    public static CompanionManager Instance { get; private set; }

    [Export] public PackedScene CompanionScene;

    // Определения активных спутников (отвечают за пассивные эффекты на игрока).
    private readonly Dictionary<CompanionId, CompanionDefinition> _activeDefs = new();
    // Живые автономные сущности.
    private readonly Dictionary<CompanionId, CompanionRuntime> _runtimes = new();
    // Здоровья спутников для отладки: id -> (current, max).
    private readonly Dictionary<CompanionId, Vector2> _runtimeHp = new();

    private Player _player;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup("companion_manager");

        CompanionScene ??= GD.Load<PackedScene>("res://scenes/companions/CompanionRuntime.tscn");
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // ---- API ----

    public bool IsActive(CompanionId id) => _activeDefs.ContainsKey(id);

    public int ActiveCount => _activeDefs.Count;

    public bool AddCompanion(CompanionId id)
    {
        if (_activeDefs.ContainsKey(id))
            return false;

        CompanionDefinition def = CompanionCatalog.Load(id);
        if (def == null)
            return false;

        _activeDefs[id] = def;

        Player player = ResolvePlayer();
        if (player != null && CompanionScene != null)
            SpawnRuntime(id, def, player);

        GD.Print($"🤝 Компаньон получен: {def.DisplayName}");
        return true;
    }

    private void SpawnRuntime(CompanionId id, CompanionDefinition def, Player player)
    {
        var runtime = CompanionScene.Instantiate<CompanionRuntime>();
        runtime.Slot = _runtimes.Count;
        runtime.TotalSlots = _runtimes.Count + 1;

        _runtimes[id] = runtime;

        Node parent = GetTree().CurrentScene ?? GetTree().Root;
        parent.CallDeferred(Node.MethodName.AddChild, runtime);
        CallDeferred(nameof(FinishSpawn), runtime, player);
    }

    private void FinishSpawn(CompanionRuntime runtime, Player player)
    {
        if (runtime == null || !GodotObject.IsInstanceValid(runtime))
            return;

        runtime.GlobalPosition = player.GlobalPosition + new Vector2(80f, 0f);
        runtime.Init(runtime.Definition, player);
    }

    public void NotifyCompanionDied(CompanionRuntime runtime)
    {
        CompanionId id = runtime.CompanionId;

        if (_runtimes.TryGetValue(id, out CompanionRuntime current) && current == runtime)
            _runtimes.Remove(id);

        _activeDefs.Remove(id);

        // Слоты пересчитываются при следующем добавлении; пассивные эффекты
        // (Страж, Мед-Бот) автоматически «выключаются» — определения больше нет.
        GD.Print($"💥 Компаньон погиб: {_runtimes.Count} в строю");
    }

    public bool RemoveCompanion(CompanionId id)
    {
        if (!_activeDefs.Remove(id))
            return false;

        if (_runtimes.Remove(id, out CompanionRuntime runtime) && runtime != null && GodotObject.IsInstanceValid(runtime))
            runtime.QueueFree();

        return true;
    }

    public void ClearCompanions()
    {
        foreach (CompanionRuntime runtime in _runtimes.Values)
        {
            if (runtime != null && GodotObject.IsInstanceValid(runtime))
                runtime.QueueFree();
        }

        _runtimes.Clear();
        _activeDefs.Clear();
    }

    public CompanionDefinition GetDefinition(CompanionId id)
    {
        return _activeDefs.TryGetValue(id, out CompanionDefinition def) ? def : null;
    }

    public CompanionRuntime GetRuntime(CompanionId id)
    {
        return _runtimes.TryGetValue(id, out CompanionRuntime runtime) ? runtime : null;
    }

    public List<CompanionDefinition> GetAllActive()
    {
        return new List<CompanionDefinition>(_activeDefs.Values);
    }

    public List<CompanionRuntime> GetAllRuntimes()
    {
        return new List<CompanionRuntime>(_runtimes.Values);
    }

    public Dictionary<CompanionId, Vector2> GetDebugHp()
    {
        _runtimeHp.Clear();
        foreach (var pair in _runtimes)
        {
            if (GodotObject.IsInstanceValid(pair.Value))
                _runtimeHp[pair.Key] = new Vector2(pair.Value.CurrentHP, pair.Value.MaxHP);
        }

        return _runtimeHp;
    }

    private Player ResolvePlayer()
    {
        if (_player != null && GodotObject.IsInstanceValid(_player))
            return _player;

        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        return _player;
    }

    public void GrantRandomCompanion()
    {
        var pool = new List<CompanionId>();
        foreach (CompanionId id in CompanionCatalog.AllIds)
        {
            if (!_activeDefs.ContainsKey(id))
                pool.Add(id);
        }

        if (pool.Count == 0)
        {
            GD.Print("Все компаньоны уже получены");
            return;
        }

        int index = (int)(GD.Randi() % (uint)pool.Count);
        AddCompanion(pool[index]);
    }
}