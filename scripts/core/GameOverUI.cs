using Godot;

public partial class GameOverUI : CanvasLayer
{
    [Export] public NodePath PlayerHealthPath;
    [Export] public NodePath RestartButtonPath;

    private Health _playerHealth;
    private Button _restartButton;

    public override void _Ready()
    {
        _restartButton = ResolveRestartButton();
        _playerHealth = ResolvePlayerHealth();

        if (_restartButton == null)
        {
            GD.PushError("GameOverUI: кнопка RestartButton не найдена.");
            return;
        }

        _restartButton.Visible = false;
        _restartButton.Pressed += OnRestartPressed;

        if (_playerHealth == null || !GodotObject.IsInstanceValid(_playerHealth))
        {
            GD.PushError("GameOverUI: Health игрока не найден. Укажи PlayerHealthPath в инспекторе.");
            return;
        }

        _playerHealth.Died += OnPlayerDied;
    }

    public override void _ExitTree()
    {
        if (_restartButton != null && GodotObject.IsInstanceValid(_restartButton))
            _restartButton.Pressed -= OnRestartPressed;

        if (_playerHealth != null && GodotObject.IsInstanceValid(_playerHealth))
            _playerHealth.Died -= OnPlayerDied;
    }

    private Button ResolveRestartButton()
    {
        if (RestartButtonPath != null && !RestartButtonPath.IsEmpty)
            return GetNodeOrNull<Button>(RestartButtonPath);

        return GetNodeOrNull<Button>("RestartButton");
    }

    private Health ResolvePlayerHealth()
    {
        if (PlayerHealthPath != null && !PlayerHealthPath.IsEmpty)
            return GetNodeOrNull<Health>(PlayerHealthPath);

        return null;
    }

    private void OnPlayerDied()
    {
        if (_restartButton != null && GodotObject.IsInstanceValid(_restartButton))
            _restartButton.Visible = true;
    }

    private void OnRestartPressed()
    {
        GetTree().ReloadCurrentScene();
    }
}