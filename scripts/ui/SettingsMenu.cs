using Godot;
using System;

// Экран настроек: секции VIDEO (разрешение/полноэкранный/VSync/масштаб UI/лимит FPS)
// и AUDIO (громкость Master/Music/SFX). Компактный, открывается поверх игры с паузой.
public partial class SettingsMenu : CanvasLayer
{
    private SettingsManager _settings;

    private Panel _panel;
    private VBoxContainer _content;
    private Button _tabVideoButton;
    private Button _tabAudioButton;
    private Button _closeButton;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        AddToGroup("settings_menu");
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        _settings = SettingsManager.Instance;
        if (_settings == null)
            GD.PushWarning("SettingsMenu: SettingsManager не найден");

        BuildUi();
    }

    private SettingsManager ResolveSettings()
    {
        _settings ??= SettingsManager.Instance;
        return _settings;
    }

    private void CleanContent()
    {
        if (_content == null)
            return;

        foreach (Node child in _content.GetChildren())
            child.QueueFree();
    }

    // ---- Управление ----

    public void Open() => Visible = true;
    public void Close() => Visible = false;

    // ---- Сборка интерфейса ----

    private void BuildUi()
    {
        _panel = new Panel();
        _panel.GrowHorizontal = Control.GrowDirection.Both;
        _panel.GrowVertical = Control.GrowDirection.Both;
        _panel.AnchorLeft = 0.5f;
        _panel.AnchorTop = 0.5f;
        _panel.AnchorRight = 0.5f;
        _panel.AnchorBottom = 0.5f;
        _panel.OffsetLeft = -260f;
        _panel.OffsetTop = -210f;
        _panel.OffsetRight = 260f;
        _panel.OffsetBottom = 210f;
        AddChild(_panel);

        var main = new VBoxContainer();
        main.MouseFilter = Control.MouseFilterEnum.Stop;
        main.AnchorRight = 1f;
        main.AnchorBottom = 1f;
        main.AddThemeConstantOverride("separation", 6);
        _panel.AddChild(main);

        var title = new Label();
        title.Text = "Настройки";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 24);
        main.AddChild(title);

        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 6);
        _tabVideoButton = MakeTabButton("VIDEO", OnVideoTab);
        _tabAudioButton = MakeTabButton("AUDIO", OnAudioTab);
        tabs.AddChild(_tabVideoButton);
        tabs.AddChild(_tabAudioButton);
        main.AddChild(tabs);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        main.AddChild(scroll);

        _content = new VBoxContainer();
        _content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _content.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_content);

        _closeButton = new Button();
        _closeButton.Text = "Готово";
        _closeButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _closeButton.Pressed += Close;
        main.AddChild(_closeButton);

        OpenTab(OnVideoTab);
    }

    private Button MakeTabButton(string text, Action tabMethod)
    {
        var btn = new Button();
        btn.Text = text;
        btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        btn.CustomMinimumSize = new Vector2(0, 30);
        btn.AddThemeFontSizeOverride("font_size", 15);
        btn.Pressed += () => OpenTab(tabMethod);
        return btn;
    }

    private void OpenTab(Action build)
    {
        CleanContent();
        build?.Invoke();
        HighlightTab(build == OnVideoTab);
    }

    private void HighlightTab(bool video)
    {
        Color active = new Color(0.45f, 0.75f, 1f);

        if (_tabVideoButton != null)
            _tabVideoButton.AddThemeColorOverride("font_color", video ? active : Colors.White);
        if (_tabAudioButton != null)
            _tabAudioButton.AddThemeColorOverride("font_color", video ? Colors.White : active);
    }

    // ---- VIDEO ----

    private void OnVideoTab()
    {
        SettingsManager s = ResolveSettings();
        if (s == null)
            return;

        AddSectionLabel("VIDEO");

        var resolutionBox = new OptionButton();
        for (int i = 0; i < SettingsManager.Resolutions.Length; i++)
        {
            var (width, height) = SettingsManager.Resolutions[i];
            resolutionBox.AddItem($"{width}×{height}");
        }
        resolutionBox.Selected = s.ResolutionIndex;
        resolutionBox.ItemSelected += index => s.SetResolutionIndex((int)index);
        AddRow("Разрешение", resolutionBox);

        var fullscreen = new CheckButton();
        fullscreen.ButtonPressed = s.Fullscreen;
        fullscreen.Toggled += value => s.SetFullscreen(value);
        AddRow("Полноэкранный", fullscreen);

        var vsync = new CheckButton();
        vsync.ButtonPressed = s.VSyncEnabled;
        vsync.Toggled += value => s.SetVSync(value);
        AddRow("Вертикальная синхронизация", vsync);

        var fpsBox = new OptionButton();
        var fpsValues = new[] { 0, 60, 120, 144 };
        var fpsNames = new[] { "Без лимита", "60 FPS", "120 FPS", "144 FPS" };
        for (int i = 0; i < fpsValues.Length; i++)
            fpsBox.AddItem(fpsNames[i]);

        int currentFps = Array.IndexOf(fpsValues, s.FpsLimit);
        fpsBox.Selected = currentFps < 0 ? 0 : currentFps;
        fpsBox.ItemSelected += index => s.SetFpsLimit(fpsValues[(int)index]);
        AddRow("Лимит FPS", fpsBox);

        AddSliderRow("Масштаб UI",
            0.8f, 1.5f, 0.05f, s.UiScale,
            value => s.SetUiScale((float)value));
    }

    // ---- AUDIO ----

    private void OnAudioTab()
    {
        SettingsManager s = ResolveSettings();
        if (s == null)
            return;

        AddSectionLabel("AUDIO");

        AddSliderRow("Мастер", -40f, 0f, 1f, s.MasterVolumeDb, value => s.SetMasterVolumeDb((float)value));
        AddSliderRow("Музыка", -40f, 0f, 1f, s.MusicVolumeDb, value => s.SetMusicVolumeDb((float)value));
        AddSliderRow("Эффекты", -40f, 0f, 1f, s.SfxVolumeDb, value => s.SetSfxVolumeDb((float)value));
    }

    // ---- Помощники построения ----

    private void AddSectionLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 13);
        label.Modulate = new Color(0.7f, 0.8f, 1f);
        _content.AddChild(label);
    }

    private void AddRow(string text, Control control)
    {
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var label = new Label();
        label.Text = text;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.AddThemeFontSizeOverride("font_size", 14);
        label.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(label);

        control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        row.AddChild(control);
        _content.AddChild(row);
    }

    private void AddSliderRow(string text, float min, float max, float step, float value, Action<float> apply)
    {
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var label = new Label();
        label.Text = text;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.AddThemeFontSizeOverride("font_size", 14);
        label.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(label);

        var slider = new HSlider();
        slider.MinValue = min;
        slider.MaxValue = max;
        slider.Step = step;
        slider.Value = value;
        slider.CustomMinimumSize = new Vector2(130, 0);
        slider.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        slider.ValueChanged += v => apply((float)v);
        row.AddChild(slider);

        _content.AddChild(row);
    }
}