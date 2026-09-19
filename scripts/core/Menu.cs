using Godot;

public partial class Menu : CanvasLayer
{
    private Button _restartButton;

    public override void _Ready()
    {
        // Меню должно работать во время паузы Game Over, поэтому Always.
        ProcessMode = ProcessModeEnum.Always;

        Visible = false;

        _restartButton = GetNodeOrNull<Button>("Restart");

        if (_restartButton == null)
        {
            GD.PushError("Menu: Restart НЕ НАЙДЕН");
            return;
        }

        _restartButton.Pressed += OnRestartPressed;
    }

    public void ShowOnDeath()
    {
        Visible = true;
    }

    private void OnRestartPressed()
    {
        GetTree().ReloadCurrentScene();
    }
}