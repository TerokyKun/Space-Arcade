using Godot;

public partial class PauseMenu : CanvasLayer
{
    private Button _resumeButton;
    private Button _quitButton;
    private GameManager _gameManager;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        AddToGroup("pause_menu");

        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        _resumeButton = GetNodeOrNull<Button>("Panel/Resume");
        _quitButton = GetNodeOrNull<Button>("Panel/Quit");
        _gameManager = GameManager.Instance ?? GetTree().GetFirstNodeInGroup("game_manager") as GameManager;

        if (_resumeButton == null)
            GD.PushError("PauseMenu: ResumeButton не найден по пути Panel/Resume");

        if (_quitButton == null)
            GD.PushError("PauseMenu: QuitButton не найден по пути Panel/Quit");

        if (_resumeButton != null)
            _resumeButton.Pressed += OnResumePressed;

        if (_quitButton != null)
            _quitButton.Pressed += OnQuitPressed;
    }

    public override void _ExitTree()
    {
        if (_resumeButton != null)
            _resumeButton.Pressed -= OnResumePressed;

        if (_quitButton != null)
            _quitButton.Pressed -= OnQuitPressed;
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