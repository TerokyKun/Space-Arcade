using Godot;

public partial class Lvl : CanvasLayer
{
    private Label _levelLabel;
    private XpBar _xpBar;
    private Label _xpGainLabel;
    private Button _chooseUpgradeButton;

    private int _currentLevel = 0;
    private int _currentGears = 0;
    private int _gearsToNextLevel = 40;
    private int _upgradeTokens = 0;

    public override void _Ready()
    {
        AddToGroup("lvl_ui");

        ProcessMode = ProcessModeEnum.Always;

        _levelLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/Row/Label");
        _xpBar = GetNodeOrNull<XpBar>("Panel/Margin/VBox/Row/XpBar");
        _xpGainLabel = GetNodeOrNull<Label>("Panel/Margin/VBox/XpGainLabel");
        _chooseUpgradeButton = GetNodeOrNull<Button>("Panel/Margin/VBox/ChooseUpgradeButton");

        if (_chooseUpgradeButton != null)
        {
            _chooseUpgradeButton.Visible = false;
            _chooseUpgradeButton.Pressed += OnChooseUpgradePressed;
        }
        else
        {
            GD.PushWarning("Lvl: ChooseUpgradeButton не найден по пути Panel/Margin/VBox/ChooseUpgradeButton");
        }

        RefreshUI();
    }

    public override void _ExitTree()
    {
        if (_chooseUpgradeButton != null)
            _chooseUpgradeButton.Pressed -= OnChooseUpgradePressed;
    }

    public void AddGears(int amount, bool showFeedback = true)
    {
        if (amount <= 0)
            return;

        if (GameManager.IsGameOver)
            return;

        _currentGears += amount;

        while (_currentGears >= _gearsToNextLevel)
        {
            _currentGears -= _gearsToNextLevel;
            LevelUp();
        }

        if (showFeedback)
            ShowXpGain(amount);

        RefreshUI();
    }

    private void LevelUp()
    {
        _currentLevel += 1;
        _upgradeTokens += 1;
        _gearsToNextLevel = CalculateNextRequirement(_currentLevel);
    }

    public bool TryConsumeUpgradeToken()
    {
        if (_upgradeTokens <= 0)
            return false;

        _upgradeTokens -= 1;
        RefreshUI();
        return true;
    }

    public int GetAvailableTokens() => _upgradeTokens;

    public void GrantLevels(int count)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
            LevelUp();

        RefreshUI();
    }

    // Короткая анимация "+N EXP": появляется, держится, плавно исчезает.
    private void ShowXpGain(int amount)
    {
        if (_xpGainLabel == null)
            return;

        Tween tween = CreateTween();
        GetTree().CreateTimer(0.02f).Timeout += () =>
        {
            if (!GodotObject.IsInstanceValid(_xpGainLabel))
                return;

            _xpGainLabel.Visible = true;
            _xpGainLabel.Text = $"+{amount} EXP";
            _xpGainLabel.Modulate = new Color(0.45f, 0.75f, 1f, 1f);

            Tween fade = CreateTween();
            fade.TweenInterval(1.1f);
            fade.TweenProperty(_xpGainLabel, "modulate:a", 0f, 0.6f);
            fade.TweenCallback(Callable.From(OnXpLabelFaded));
        };
    }

    private void OnXpLabelFaded()
    {
        if (GodotObject.IsInstanceValid(_xpGainLabel))
            _xpGainLabel.Visible = false;
    }

    private void OnChooseUpgradePressed()
    {
        GD.Print("Кнопка нажата");

        var menu = GetTree().GetFirstNodeInGroup("upgrade_menu") as UpgradeMenu;

        if (menu == null)
        {
            GD.PrintErr("UpgradeMenu НЕ найден!");
            return;
        }

        menu.Open();
    }

    private int CalculateNextRequirement(int level)
    {
        // Кривая опыта (плавная, с ускорением в середине и замедлением в конце):
        //   1-5  — быстро;  5-10 — комфортно;  10-20 — плавный рост;  20+ — медленно.
        if (level < 5)
            return 35 + level * 15;                    // 35, 50, 65, 80, 95
        if (level < 10)
            return 110 + (level - 5) * 32;             // 110, 142, 174, 206, 238
        if (level < 20)
            return 260 + (level - 10) * 46;            // 260 ... 720
        return 740 + (level - 20) * 132;               // 740+ (замедление прокачки)
    }

    private void RefreshUI()
    {
        if (_levelLabel != null)
            _levelLabel.Text = $"Lvl: {_currentLevel}";

        if (_xpBar != null)
        {
            _xpBar.SetProgress(_currentGears, _gearsToNextLevel);
            _xpBar.SetUpgradeAvailable(_upgradeTokens > 0);
        }

        if (_chooseUpgradeButton != null)
        {
            _chooseUpgradeButton.Visible = _upgradeTokens > 0;
            _chooseUpgradeButton.Text = _upgradeTokens > 1
                ? $" Upgrade! ({_upgradeTokens})"
                : "Upgrade!";
        }
    }
}