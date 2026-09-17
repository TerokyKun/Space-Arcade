using Godot;

public partial class Lvl : CanvasLayer
{
    private Label _levelLabel;
    private ProgressBar _progressBar;
    private Button _chooseUpgradeButton;

    private int _currentLevel = 0;
    private int _currentGears = 0;
    private int _gearsToNextLevel = 50;
    private int _upgradeTokens = 0;

    public override void _Ready()
    {
        AddToGroup("lvl_ui");

        _levelLabel = GetNodeOrNull<Label>("Panel/Label");
        _progressBar = GetNodeOrNull<ProgressBar>("Panel/PlayerProgress");
        _chooseUpgradeButton = GetNodeOrNull<Button>("Panel/ChooseUpgradeButton");

        if (_chooseUpgradeButton != null)
        {
            _chooseUpgradeButton.Visible = false;
            _chooseUpgradeButton.Pressed += OnChooseUpgradePressed;
        }
        else
        {
            GD.PushWarning("Lvl: ChooseUpgradeButton не найден по пути Panel/ChooseUpgradeButton");
        }

        RefreshUI();
    }

    public override void _ExitTree()
    {
        if (_chooseUpgradeButton != null)
            _chooseUpgradeButton.Pressed -= OnChooseUpgradePressed;
    }

    public void AddGears(int amount)
    {
        if (amount <= 0)
            return;

        _currentGears += amount;

        while (_currentGears >= _gearsToNextLevel)
        {
            _currentGears -= _gearsToNextLevel;
            LevelUp();
        }

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
        // Мягкий рост: 50, 80, 120, 170, 230...
        return 50 + (level * 25) + (level * level * 5);
    }

    private void RefreshUI()
    {
        if (_levelLabel != null)
            _levelLabel.Text = $"Lvl: {_currentLevel}";

        if (_progressBar != null)
        {
            _progressBar.MaxValue = _gearsToNextLevel;
            _progressBar.Value = Mathf.Clamp(_currentGears, 0, _gearsToNextLevel);
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