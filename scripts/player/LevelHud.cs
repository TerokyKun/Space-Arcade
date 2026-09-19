using Godot;

public partial class LevelHud : Panel
{
    [Export] public NodePath ProgressBarPath;
    [Export] public NodePath LabelPath;
    [Export] public NodePath PlayerProgressPath;

    private ProgressBar _progressBar;
    private Label _label;
    private PlayerProgress _progress;

    public override void _Ready()
    {
        _progressBar = GetNode<ProgressBar>(ProgressBarPath);
        _label = GetNode<Label>(LabelPath);
        _progress = GetNode<PlayerProgress>(PlayerProgressPath);

        _progress.ExpChanged += OnExpChanged;
        _progress.LevelUp += OnLevelUp;

        OnExpChanged(_progress.CurrentExp, _progress.ExpToNextLevel, _progress.Level);
    }

    private void OnExpChanged(float currentExp, float maxExp, int level)
    {
        _progressBar.MaxValue = maxExp;
        _progressBar.Value = currentExp;
        _label.Text = $"LvL {level}";
    }

    private void OnLevelUp(int newLevel)
    {
        _label.Text = $"LvL {newLevel}";
    }
}