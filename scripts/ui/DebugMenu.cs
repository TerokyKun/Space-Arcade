using Godot;

public partial class DebugMenu : CanvasLayer
{
    private VBoxContainer _vBox;
    private Label _companionStatusLabel;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        _vBox = GetNodeOrNull<VBoxContainer>("Panel/Margin/Scroll/VBox");
        if (_vBox == null)
        {
            GD.PushError("DebugMenu: VBox не найден по пути Panel/Margin/Scroll/VBox");
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
        Visible = !Visible;
        if (Visible)
            RefreshCompanionStatus();
    }

    private void BuildUi()
    {
        AddTitle("DEBUG");

        AddSection("SPAWN");
        AddButton("Противник x1", () => GetSpawner()?.SpawnEnemy(true));
        AddButton("Противник x5", () => GetSpawner()?.SpawnEnemies(5, true));
        AddButton("Астероид", () => GetSpawner()?.SpawnAsteroid(true));
        AddButton("Хилка", () => GetSpawner()?.SpawnHealPickup(true));
        AddButton("Дроп (XP)", () => GetSpawner()?.SpawnDropPickup());

        AddSection("ENEMIES");
        AddButton("Спавн: Fast", () => GetSpawner()?.SpawnKind(EnemyKind.Fast));
        AddButton("Спавн: Tank", () => GetSpawner()?.SpawnKind(EnemyKind.Tank));
        AddButton("Спавн: Ranged", () => GetSpawner()?.SpawnKind(EnemyKind.Ranged));
        AddButton("Спавн: Elite", () => GetSpawner()?.SpawnKind(EnemyKind.Elite));
        AddButton("Спавн: БОСС", () => GetSpawner()?.SpawnBossNow());

        AddSection("CASES");
        AddButton("Кейс: +5 убийств", () => GetCaseManager()?.DebugAddKills(5));
        AddButton("Кейс: +25 убийств", () => GetCaseManager()?.DebugAddKills(25));
        AddButton("Кейс: пересоздать", () => GetCaseManager()?.DebugRespawnCase());

        AddSection("TIME");
        AddButton("Время +1 мин", () => GetHud()?.AddTime(60.0));
        AddButton("Время +5 мин", () => GetHud()?.AddTime(300.0));
        AddButton("Время +10 сек", () => GetHud()?.AddTime(10.0));

        AddSection("LEVEL");
        AddButton("Уровень +1", () => GetLvl()?.GrantLevels(1));
        AddButton("Уровень +5", () => GetLvl()?.GrantLevels(5));

        AddSection("COMPANIONS");
        _companionStatusLabel = new Label();
        _companionStatusLabel.Text = "—";
        _companionStatusLabel.AddThemeFontSizeOverride("font_size", 13);
        _companionStatusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _vBox.AddChild(_companionStatusLabel);

        AddButton("Дать: Мед-Бот", () => { GetCompanions()?.AddCompanion(CompanionId.MedBot); RefreshCompanionStatus(); });
        AddButton("Дать: Страж", () => { GetCompanions()?.AddCompanion(CompanionId.Guardian); RefreshCompanionStatus(); });
        AddButton("Дать: Огненный", () => { GetCompanions()?.AddCompanion(CompanionId.FireCompanion); RefreshCompanionStatus(); });
        AddButton("Дать: Боевой дрон", () => { GetCompanions()?.AddCompanion(CompanionId.CombatDrone); RefreshCompanionStatus(); });
        AddButton("Дать: случайный", () => { GetCompanions()?.GrantRandomCompanion(); RefreshCompanionStatus(); });
        AddButton("Очистить всех", () => { GetCompanions()?.ClearCompanions(); RefreshCompanionStatus(); });
        AddButton("Восстановить HP всем", () => { RestoreCompanionHpAll(); RefreshCompanionStatus(); });
        AddButton("Убить все", () => { KillCompanionsAll(); RefreshCompanionStatus(); });
        AddButton("Обновить статус", RefreshCompanionStatus);
        AddButton("Кейс: показать награду", () => GetRewardUi()?.Open());

        AddSection("SETTINGS");
        AddButton("Настройки: открыть", () => GetSettings()?.Open());
        AddButton("Настройки: сброс", ResetSettings);

        AddButton("Закрыть (ё)", Toggle);
    }

    // Статус компаньонов (HP в реальном времени, вне HUD).
    private void RefreshCompanionStatus()
    {
        if (_companionStatusLabel == null)
            return;

        CompanionManager manager = GetCompanions();
        if (manager == null)
        {
            _companionStatusLabel.Text = "—";
            return;
        }

        var hp = manager.GetDebugHp();
        var runtimes = manager.GetAllRuntimes();

        if (hp.Count == 0 && runtimes.Count == 0)
        {
            _companionStatusLabel.Text = "Нет компаньонов";
            return;
        }

        var lines = new System.Collections.Generic.List<string>();
        foreach (var pair in hp)
        {
            string state = runtimes.Exists(r => r.CompanionId == pair.Key) ? "активен" : "—";
            lines.Add($"{pair.Key}: {pair.Value.X:F0}/{pair.Value.Y:F0} ({state})");
        }

        if (lines.Count == 0)
            _companionStatusLabel.Text = "Нет компаньонов";
        else
            _companionStatusLabel.Text = string.Join("\n", lines);
    }

    private void KillCompanionsAll()
    {
        CompanionManager manager = GetCompanions();
        if (manager == null)
            return;

        foreach (CompanionRuntime runtime in manager.GetAllRuntimes())
        {
            if (runtime != null && GodotObject.IsInstanceValid(runtime))
                runtime.DebugKill();
        }
    }

    private void RestoreCompanionHpAll()
    {
        CompanionManager manager = GetCompanions();
        if (manager == null)
            return;

        foreach (CompanionRuntime runtime in manager.GetAllRuntimes())
        {
            if (runtime != null && GodotObject.IsInstanceValid(runtime))
                runtime.DebugRestoreHp();
        }
    }

    private SettingsMenu GetSettings()
    {
        return GetTree().GetFirstNodeInGroup("settings_menu") as SettingsMenu;
    }

    private void ResetSettings()
    {
        var settings = GetSettings();
        if (settings == null)
            return;

        // Удаляем файл настроек и возвращаем значения по умолчанию.
        string path = "user://settings.cfg";
        if (FileAccess.FileExists(path))
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.Load();
            SettingsManager.Instance.ApplyAll();
        }

        GD.Print("Настройки сброшены");
    }

    private void AddTitle(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 18);
        _vBox.AddChild(label);
    }

    private void AddSection(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 13);
        _vBox.AddChild(label);
    }

    private void AddButton(string text, System.Action onClick)
    {
        var button = new Button();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(0, 26);
        button.AddThemeFontSizeOverride("font_size", 14);
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

    private CaseManager GetCaseManager()
    {
        return GetTree().CurrentScene?.GetNodeOrNull<CaseManager>("Arena/CaseManager");
    }

    private CompanionManager GetCompanions()
    {
        return CompanionManager.Instance ?? GetTree().GetFirstNodeInGroup("companion_manager") as CompanionManager;
    }

    private RewardUI GetRewardUi()
    {
        return GetTree().GetFirstNodeInGroup("reward_ui") as RewardUI;
    }
}