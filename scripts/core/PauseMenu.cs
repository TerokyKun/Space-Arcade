using Godot;

public partial class PauseMenu : CanvasLayer
{
    private Button _resumeButton;
    private Button _settingsButton;
    private Button _quitButton;
    private GameManager _gameManager;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        AddToGroup("pause_menu");

        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        _resumeButton = GetNodeOrNull<Button>("Panel/Resume");
        _settingsButton = GetNodeOrNull<Button>("Panel/Settings");
        _quitButton = GetNodeOrNull<Button>("Panel/Quit");
        _gameManager = GameManager.Instance ?? GetTree().GetFirstNodeInGroup("game_manager") as GameManager;

        if (_resumeButton == null)
            GD.PushError("PauseMenu: ResumeButton не найден по пути Panel/Resume");

        if (_settingsButton == null)
            GD.PushWarning("PauseMenu: SettingsButton не найден по пути Panel/Settings");

        if (_quitButton == null)
            GD.PushError("PauseMenu: QuitButton не найден по пути Panel/Quit");

        if (_resumeButton != null)
            _resumeButton.Pressed += OnResumePressed;

        if (_settingsButton != null)
            _settingsButton.Pressed += OnSettingsPressed;

        if (_quitButton != null)
            _quitButton.Pressed += OnQuitPressed;
    }

    public override void _ExitTree()
    {
        if (_resumeButton != null)
            _resumeButton.Pressed -= OnResumePressed;

        if (_settingsButton != null)
            _settingsButton.Pressed -= OnSettingsPressed;

        if (_quitButton != null)
            _quitButton.Pressed -= OnQuitPressed;
    }

    // Открыть настройки прямо из паузы (внутри существующей паузы — доп. лока не нужен).
    private void OnSettingsPressed()
    {
        var settings = GetTree().GetFirstNodeInGroup("settings_menu") as SettingsMenu;
        settings?.Open();
    }

    public void ShowMenu()
    {
        Visible = true;
    }

    public void HideMenu()
    {
        Visible = false;
    }

    private void OnResumePressed()
    {
        _gameManager?.Resume();
    }

    private void OnQuitPressed()
    {
        _gameManager?.Quit();
    }
}