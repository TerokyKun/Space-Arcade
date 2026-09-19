using Godot;

// Карточка награды: фиксированный размер, короткие подписи, кнопка выбора.
public partial class CompanionCard : PanelContainer
{
    [Signal] public delegate void PickedEventHandler(int id);
    [Signal] public delegate void FallbackPickedEventHandler(int gears);

    private TextureRect _icon;
    private Label _nameLabel;
    private Label _rarityLabel;
    private Label _descLabel;
    private Label _effectLabel;
    private Button _button;

    private CompanionId _id;
    private int _fallbackGears = 0;
    private bool _locked = false;

    public override void _Ready()
    {
        _icon = GetNodeOrNull<TextureRect>("Margin/VBox/Icon");
        _nameLabel = GetNodeOrNull<Label>("Margin/VBox/Name");
        _rarityLabel = GetNodeOrNull<Label>("Margin/VBox/Rarity");
        _descLabel = GetNodeOrNull<Label>("Margin/VBox/Description");
        _effectLabel = GetNodeOrNull<Label>("Margin/VBox/Effect");
        _button = GetNodeOrNull<Button>("Margin/VBox/Button");

        if (_button != null)
            _button.Pressed += OnPressed;
    }

    public override void _ExitTree()
    {
        if (_button != null)
            _button.Pressed -= OnPressed;
    }

    public void Configure(CompanionDefinition def)
    {
        _id = def.Id;

        if (_icon != null)
            _icon.Texture = def.Icon;

        if (_nameLabel != null)
            _nameLabel.Text = def.DisplayName;

        if (_rarityLabel != null)
        {
            _rarityLabel.Text = def.Rarity.DisplayName();
            _rarityLabel.AddThemeColorOverride("font_color", def.Rarity.Color());
        }

        if (_descLabel != null)
            _descLabel.Text = def.Description;

        if (_effectLabel != null)
            _effectLabel.Text = def.EffectText;
    }

    // Фолбэк-карточка, когда список доступных компаньонов исчерпан.
    public void ConfigureFallback(int gears)
    {
        _id = CompanionId.MedBot;
        _fallbackGears = gears;

        if (_icon != null && _icon.Texture == null)
            _icon.Texture = GD.Load<Texture2D>("assets/placeholders/gear_icon.svg");

        if (_nameLabel != null)
            _nameLabel.Text = "Шестерёнки";

        if (_rarityLabel != null)
        {
            _rarityLabel.Text = "Бонус";
            _rarityLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.5f));
        }

        if (_descLabel != null)
            _descLabel.Text = "Лёгкий запас XP";

        if (_effectLabel != null)
            _effectLabel.Text = "+" + gears + " шестерёнок";
    }

    public void Lock()
    {
        _locked = true;
        if (_button != null)
            _button.Disabled = true;
    }

    private void OnPressed()
    {
        if (_locked)
            return;

        _locked = true;
        if (_fallbackGears > 0)
            EmitSignal(SignalName.FallbackPicked, _fallbackGears);
        else
            EmitSignal(SignalName.Picked, (int)_id);
    }
}