using Godot;
using System.Collections.Generic;

public partial class UpgradeMenu : CanvasLayer
{
    private Label _titleLabel;
    private Button _closeButton;
    private VBoxContainer _optionsVBox;

    private Lvl _lvl;
    private Player _player;
    private PlayerUpgrades _upgrades;
    private UpgradeDatabase _database;
    private GameManager _gameManager;

    private readonly RandomNumberGenerator _rng = new();

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        AddToGroup("upgrade_menu");

        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        _rng.Randomize();

        _titleLabel = GetNodeOrNull<Label>("Panel/TitleLabel");
        _closeButton = GetNodeOrNull<Button>("Panel/CloseButton");
        _optionsVBox = GetNodeOrNull<VBoxContainer>("Panel/ScrollContainer/OptionsVBox");

        _database = null;
        _gameManager = GameManager.Instance ?? GetTree().GetFirstNodeInGroup("game_manager") as GameManager;

        if (_closeButton != null)
            _closeButton.Pressed += OnClosePressed;
        else
            GD.PushError("UpgradeMenu: CloseButton не найден по пути Panel/CloseButton");

        if (_titleLabel == null)
            GD.PushError("UpgradeMenu: TitleLabel не найден по пути Panel/TitleLabel");

        if (_optionsVBox == null)
            GD.PushError("UpgradeMenu: OptionsVBox не найден по пути Panel/ScrollContainer/OptionsVBox");
    }

    public override void _ExitTree()
    {
        if (_closeButton != null)
            _closeButton.Pressed -= OnClosePressed;
    }

    public void Open()
    {
        _lvl = GetTree().GetFirstNodeInGroup("lvl_ui") as Lvl;
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        _upgrades = _player?.GetNodeOrNull<PlayerUpgrades>("PlayerUpgrades");

        if (_lvl == null || _player == null || _upgrades == null)
        {
            GD.PushWarning("UpgradeMenu: не найдены Lvl / Player / PlayerUpgrades");
            return;
        }

        _database = ResolveDatabase();
        if (_database == null)
        {
            GD.PushError("UpgradeMenu: UpgradeDatabase не найден. Добавь его в сцену или сделай autoload.");
            return;
        }

        Visible = true;
        _gameManager?.NotifyUpgradeMenuOpened();

        BuildOptions();
    }

    public void CloseMenu()
    {
        Visible = false;
        _gameManager?.NotifyUpgradeMenuClosed();
    }

    private UpgradeDatabase ResolveDatabase()
    {
        if (_database != null)
            return _database;

        _database = UpgradeDatabase.Instance ?? GetTree().GetFirstNodeInGroup("upgrade_database") as UpgradeDatabase;
        return _database;
    }

    private void BuildOptions()
    {
        ClearOptions();

        bool chooseClass = !_upgrades.HasClass;
        var source = GetSource(chooseClass);

        if (_titleLabel != null)
            _titleLabel.Text = chooseClass ? "Выбери класс" : "Выбери улучшение";

        if (source == null || source.Count == 0)
        {
            GD.PushWarning("UpgradeMenu: список апгрейдов пуст. Проверь UpgradeDatabase и JSON.");
            return;
        }

        var options = PickUpToThreeUnique(source);

        foreach (var option in options)
        {
            var btn = CreateOptionButton(option);
            _optionsVBox.AddChild(btn);
        }
    }

    private List<UpgradeDefinition> GetSource(bool chooseClass)
    {
        var db = ResolveDatabase();
        if (db == null)
            return new List<UpgradeDefinition>();

        return chooseClass ? db.ClassChoices : db.StatChoices;
    }

    private List<UpgradeDefinition> PickUpToThreeUnique(IReadOnlyList<UpgradeDefinition> source)
    {
        var result = new List<UpgradeDefinition>();
        var used = new HashSet<int>();

        if (source == null || source.Count == 0)
            return result;

        int count = Mathf.Min(3, source.Count);

        while (result.Count < count)
        {
            int index = _rng.RandiRange(0, source.Count - 1);

            if (used.Contains(index))
                continue;

            used.Add(index);
            result.Add(source[index]);
        }

        return result;
    }

    private Button CreateOptionButton(UpgradeDefinition upgrade)
    {
        var btn = new Button();
        btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        btn.CustomMinimumSize = new Vector2(0, 90);
        btn.Text = $"{upgrade.Title}\n{upgrade.Description}";

        if (upgrade.Icon != null)
            btn.Icon = upgrade.Icon;

        btn.Pressed += () => OnUpgradeSelected(upgrade);
        return btn;
    }

    private void OnUpgradeSelected(UpgradeDefinition upgrade)
    {
        if (upgrade == null || _lvl == null || _player == null || _upgrades == null)
            return;

        if (!_lvl.TryConsumeUpgradeToken())
            return;

        _upgrades.ApplyUpgrade(upgrade);
        CloseMenu();
    }

    private void OnClosePressed()
    {
        CloseMenu();
    }

    private void ClearOptions()
    {
        if (_optionsVBox == null)
            return;

        foreach (var child in _optionsVBox.GetChildren())
            child.QueueFree();
    }
}