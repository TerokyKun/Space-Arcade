using Godot;
using System.Collections.Generic;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    private const string PauseMenuLock = "pause_menu";
    private const string UpgradeMenuLock = "upgrade_menu";

    private readonly HashSet<string> _pauseLocks = new();

    private PauseMenu _pauseMenu;
    private UpgradeMenu _upgradeMenu;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup("game_manager");
        ProcessMode = ProcessModeEnum.Always;

        CacheMenus();
        SyncPauseState();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
            TogglePauseMenu();
    }

    private void CacheMenus()
    {
        _pauseMenu ??= GetTree().GetFirstNodeInGroup("pause_menu") as PauseMenu;
        _upgradeMenu ??= GetTree().GetFirstNodeInGroup("upgrade_menu") as UpgradeMenu;
    }

    private void SyncPauseState()
    {
        GetTree().Paused = _pauseLocks.Count > 0;
    }

    public void AddPauseLock(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return;

        if (_pauseLocks.Add(source))
            SyncPauseState();
    }

    public void RemovePauseLock(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return;

        if (_pauseLocks.Remove(source))
            SyncPauseState();
    }

    public void ClearPauseLocks()
    {
        _pauseLocks.Clear();
        SyncPauseState();
    }

    public bool HasPauseLock(string source)
    {
        return _pauseLocks.Contains(source);
    }

    public bool IsUpgradeMenuOpen()
    {
        CacheMenus();
        return _upgradeMenu != null && _upgradeMenu.IsOpen;
    }

    public void TogglePauseMenu()
    {
        CacheMenus();

        if (_pauseMenu == null)
            return;

        if (_pauseMenu.IsOpen)
        {
            _pauseMenu.HideMenu();
            RemovePauseLock(PauseMenuLock);
            return;
        }

        _pauseMenu.ShowMenu();
        AddPauseLock(PauseMenuLock);
    }

    public void Resume()
    {
        CacheMenus();

        if (_pauseMenu != null)
            _pauseMenu.HideMenu();

        RemovePauseLock(PauseMenuLock);
    }

    public void OpenUpgradeMenu()
    {
        CacheMenus();
        _upgradeMenu?.Open();
    }

    public void CloseUpgradeMenu()
    {
        CacheMenus();
        _upgradeMenu?.CloseMenu();
    }

    public void NotifyUpgradeMenuOpened()
    {
        AddPauseLock(UpgradeMenuLock);
    }

    public void NotifyUpgradeMenuClosed()
    {
        RemovePauseLock(UpgradeMenuLock);
    }

    public void Quit()
    {
        ClearPauseLocks();
        GetTree().Quit();
    }
}