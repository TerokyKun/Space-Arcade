using Godot;
using System.Collections.Generic;

// Экран выбора награды за кейс: 3 фиксированные карточки без прокрутки.
// Игрок выбирает одну; выбранный компаньон попадает в CompanionManager.
public partial class RewardUI : CanvasLayer
{
    [Export] public PackedScene CardScene;

    private const string PauseLock = "reward_menu";
    private HBoxContainer _cardsBox;
    private GameManager _gameManager;
    private Lvl _lvl;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        AddToGroup("reward_ui");

        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        CardScene ??= GD.Load<PackedScene>("res://scenes/companions/CompanionCard.tscn");
        _cardsBox = GetNodeOrNull<HBoxContainer>("Panel/Margin/VBox/Cards");
        _gameManager = GameManager.Instance ?? GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
        _lvl = GetTree().GetFirstNodeInGroup("lvl_ui") as Lvl;
    }

    // Вызов из CaseZone (деферред), чтобы не менять дерево в физик-колбэке.
    public void Open()
    {
        if (_cardsBox == null)
        {
            GD.PushError("RewardUI: Cards не найден");
            return;
        }

        ClearCards();

        List<CompanionDefinition> options = GenerateOptions(3);
        int fallbackGears = 20;

        for (int i = 0; i < 3; i++)
        {
            var card = CardScene.Instantiate<CompanionCard>();
            _cardsBox.AddChild(card);

            if (i < options.Count)
            {
                CompanionDefinition def = options[i];
                card.Configure(def);
                card.Picked += OnCardPicked;
            }
            else
            {
                card.ConfigureFallback(fallbackGears);
                card.FallbackPicked += OnFallbackPicked;
            }
        }

        Visible = true;
        _gameManager?.AddPauseLock(PauseLock);
    }

    public void Close()
    {
        ClearCards();
        Visible = false;
        _gameManager?.RemovePauseLock(PauseLock);
    }

    // Возвращает до count уникальных (не полученных игроком) компаньонов.
    // Полученные исключаются, чтобы не предлагать дубликаты.
    private List<CompanionDefinition> GenerateOptions(int count)
    {
        var manager = CompanionManager.Instance;
        var result = new List<CompanionDefinition>();

        var candidates = new List<CompanionDefinition>();
        foreach (CompanionId id in CompanionCatalog.AllIds)
        {
            if (manager != null && manager.IsActive(id))
                continue;

            CompanionDefinition def = CompanionCatalog.Load(id);
            if (def == null)
                continue;
            candidates.Add(def);
        }

        candidates.Sort((a, b) => b.Rarity.Weight().CompareTo(a.Rarity.Weight()));

        var pooled = new List<CompanionDefinition>(candidates);
        while (result.Count < count && pooled.Count > 0)
        {
            CompanionDefinition pick = PickWeighted(pooled);
            result.Add(pick);
            pooled.Remove(pick);
        }

        return result;
    }

    private CompanionDefinition PickWeighted(List<CompanionDefinition> pool)
    {
        float total = 0f;
        foreach (CompanionDefinition d in pool)
            total += d.Rarity.Weight();

        float roll = (float)GD.RandRange(0.0, total);
        float acc = 0f;
        foreach (CompanionDefinition d in pool)
        {
            acc += d.Rarity.Weight();
            if (roll <= acc)
                return d;
        }

        return pool[pool.Count - 1];
    }

    private void OnCardPicked(int id)
    {
        var manager = CompanionManager.Instance;
        if (manager != null)
            manager.AddCompanion((CompanionId)id);

        Close();
    }

    private void OnFallbackPicked(int gears)
    {
        if (_lvl != null)
            _lvl.AddGears(gears);

        Close();
    }

    private void ClearCards()
    {
        if (_cardsBox == null)
            return;

        foreach (var child in _cardsBox.GetChildren())
            child.Free();
    }
}