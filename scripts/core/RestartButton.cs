using Godot;

public partial class RestartButton : Button
{
    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    public override void _ExitTree()
    {
        Pressed -= OnPressed;
    }

    private void OnPressed()
    {
        GetTree().ReloadCurrentScene();
    }
}