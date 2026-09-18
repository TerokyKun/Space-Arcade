using Godot;

public partial class DebugMenu : CanvasLayer
{
    private const string PauseLock = "debug_menu";

    private VBoxContainer _vBox;
    private GameManager _gameManager;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        _gameManager = GameManager.Instance ?? GetTree().GetFirstNodeInGroup("game_manager") as GameManager;

        _vBox = GetNodeOrNull<VBoxContainer>("Panel/Margin/VBox");
        if (_vBox == null)
        {
            GD.PushError("DebugMenu: VBox не найден по пути Panel/Margin/VBox");
            return;
        }

        BuildUi();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_menu"))
            Toggle();
    }

    public void Toggle()
    {
        if (IsOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    private void OpenMenu()
    {
        Visible = true;
        _gameManager?.AddPauseLock(PauseLock);
    }

    private void CloseMenu()
    {
        Visible = false;
        _gameManager?.RemovePauseLock(PauseLock);
    }

    private void BuildUi()
    {
        AddTitle("DEBUG");

        AddButton("Противник x1", () => GetSpawner()?.SpawnEnemy(true));
        AddButton("Противник x5", () => GetSpawner()?.SpawnEnemies(5, true));
        AddButton("Астероид", () => GetSpawner()?.SpawnAsteroid(true));
        AddButton("Хилка", () => GetSpawner()?.SpawnHealPickup(true));
        AddButton("Дроп (XP)", () => GetSpawner()?.SpawnDropPickup());

        AddSection("Таймер");
        AddButton("Время +1 мин", () => GetHud()?.AddTime(60.0));
        AddButton("Время +5 мин", () => GetHud()?.AddTime(300.0));
        AddButton("Время +10 сек", () => GetHud()?.AddTime(10.0));

        AddSection("Уровень");
        AddButton("Уровень +1", () => GetLvl()?.GrantLevels(1));
        AddButton("Уровень +5", () => GetLvl()?.GrantLevels(5));

        AddButton("Закрыть (ё)", CloseMenu);
    }

    private void AddTitle(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 20);
        _vBox.AddChild(label);
    }

    private void AddSection(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 14);
        _vBox.AddChild(label);
    }

    private void AddButton(string text, System.Action onClick)
    {
        var button = new Button();
        button.Text = text;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.Pressed += () => onClick();
        _vBox.AddChild(button);
    }

    private EnemySpawner GetSpawner()
    {
        return GetTree().CurrentScene?.GetNodeOrNull<EnemySpawner>("Arena/EnemySpawner");
    }

    private PlayerHud GetHud()
    {
        return GetTree().CurrentScene?.GetNodeOrNull<PlayerHud>("PlayerHud");
    }

    private Lvl GetLvl()
    {
        return GetTree().GetFirstNodeInGroup("lvl_ui") as Lvl;
    }
}