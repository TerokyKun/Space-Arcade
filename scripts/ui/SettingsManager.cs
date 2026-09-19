using Godot;

// Настройки видео/аудио: реально применяются через Window/DisplayServer/AudioServer
// и сохраняются в user://settings.cfg, восстанавливаются при запуске.
public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; }

    private const string ConfigPath = "user://settings.cfg";

    public static readonly (int Width, int Height)[] Resolutions =
    {
        (1280, 720),
        (1600, 900),
        (1920, 1080),
        (2560, 1440)
    };

    public int ResolutionIndex { get; private set; } = 0;
    public bool Fullscreen { get; private set; } = false;
    public bool VSyncEnabled { get; private set; } = true;
    public float UiScale { get; private set; } = 1f;
    public int FpsLimit { get; private set; } = 0;

    public float MasterVolumeDb { get; private set; } = 0f;
    public float MusicVolumeDb { get; private set; } = 0f;
    public float SfxVolumeDb { get; private set; } = 0f;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup("settings_manager");
        ProcessMode = ProcessModeEnum.Always;

        EnsureAudioBuses();
        Load();
        ApplyAll();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // ---- Персистентность ----

    public void Load()
    {
        if (!FileAccess.FileExists(ConfigPath))
            return;

        var config = new ConfigFile();
        if (config.Load(ConfigPath) != Error.Ok)
            return;

        ResolutionIndex = Mathf.Clamp((int)config.GetValue("video", "resolution_index", 0), 0, Resolutions.Length - 1);
        Fullscreen = (bool)config.GetValue("video", "fullscreen", false);
        VSyncEnabled = (bool)config.GetValue("video", "vsync", true);
        UiScale = Mathf.Clamp((float)config.GetValue("video", "ui_scale", 1.0), 0.8f, 1.5f);
        FpsLimit = Mathf.Max(0, (int)config.GetValue("video", "fps_limit", 0));

        MasterVolumeDb = (float)config.GetValue("audio", "master", 0.0);
        MusicVolumeDb = (float)config.GetValue("audio", "music", 0.0);
        SfxVolumeDb = (float)config.GetValue("audio", "sfx", 0.0);
    }

    public void Save()
    {
        var config = new ConfigFile();

        config.SetValue("video", "resolution_index", ResolutionIndex);
        config.SetValue("video", "fullscreen", Fullscreen);
        config.SetValue("video", "vsync", VSyncEnabled);
        config.SetValue("video", "ui_scale", UiScale);
        config.SetValue("video", "fps_limit", FpsLimit);

        config.SetValue("audio", "master", MasterVolumeDb);
        config.SetValue("audio", "music", MusicVolumeDb);
        config.SetValue("audio", "sfx", SfxVolumeDb);

        config.Save(ConfigPath);
    }

    // ---- Применение ----

    public void ApplyAll()
    {
        ApplyVideo();
        ApplyAudio();
    }

    public void ApplyVideo()
    {
        var window = GetWindow();
        if (window == null)
            return;

        int idx = Mathf.Clamp(ResolutionIndex, 0, Resolutions.Length - 1);
        window.Size = new Vector2I(Resolutions[idx].Width, Resolutions[idx].Height);

        Vector2I screenSize = DisplayServer.ScreenGetSize();
        if (screenSize.X > 0)
            window.Position = (screenSize - window.Size) / 2;

        DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetVsyncMode(VSyncEnabled ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

        window.ContentScaleFactor = UiScale;
        Engine.MaxFps = FpsLimit;
    }

    public void ApplyAudio()
    {
        SetBusVolume("Master", MasterVolumeDb);
        SetBusVolume("Music", MusicVolumeDb);
        SetBusVolume("SFX", SfxVolumeDb);
    }

    private static void EnsureAudioBuses()
    {
        foreach (string name in new[] { "Music", "SFX" })
        {
            if (AudioServer.GetBusIndex(name) == -1)
                AudioServer.AddBus();
        }
    }

    private static void SetBusVolume(string busName, float db)
    {
        int index = AudioServer.GetBusIndex(busName);
        if (index == -1)
            return;

        AudioServer.SetBusVolumeDb(index, db);
        AudioServer.SetBusMute(index, db <= -40f);
    }

    // ---- Сеттеры (меняют значение + сохраняют + применяют) ----

    public void SetResolutionIndex(int value)
    {
        ResolutionIndex = Mathf.Clamp(value, 0, Resolutions.Length - 1);
        Save();
        ApplyVideo();
    }

    public void SetFullscreen(bool value)
    {
        Fullscreen = value;
        Save();
        ApplyVideo();
    }

    public void SetVSync(bool value)
    {
        VSyncEnabled = value;
        Save();
        ApplyVideo();
    }

    public void SetUiScale(float value)
    {
        UiScale = Mathf.Clamp(value, 0.8f, 1.5f);
        Save();
        var window = GetWindow();
        if (window != null)
            window.ContentScaleFactor = UiScale;
    }

    public void SetFpsLimit(int value)
    {
        FpsLimit = Mathf.Max(0, value);
        Save();
        Engine.MaxFps = FpsLimit;
    }

    public void SetMasterVolumeDb(float db) { MasterVolumeDb = db; Save(); ApplyAudio(); }
    public void SetMusicVolumeDb(float db) { MusicVolumeDb = db; Save(); ApplyAudio(); }
    public void SetSfxVolumeDb(float db) { SfxVolumeDb = db; Save(); ApplyAudio(); }
}