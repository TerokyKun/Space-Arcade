using Godot;
using System.Collections.Generic;

public partial class CommanderDroneManager : Node
{
    [Export] public PackedScene DroneScene;

    private Player _player;
    private PlayerUpgrades _upgrades;
    private readonly List<CommanderDrone> _drones = new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _player = GetParent<Player>();
        _upgrades = _player?.GetNodeOrNull<PlayerUpgrades>("PlayerUpgrades");

        if (_upgrades != null)
            _upgrades.Changed += SyncClass;

        SyncClass();
    }

    public override void _ExitTree()
    {
        if (_upgrades != null)
            _upgrades.Changed -= SyncClass;
    }

    private void SyncClass()
    {
        if (_player == null || _upgrades == null)
            return;

        bool enabled = _upgrades.SelectedClass == PlayerClassType.Commander;

        if (!enabled)
        {
            ClearDrones();
            return;
        }

        if (_drones.Count == _upgrades.CommanderDrones)
            return;

        ClearDrones();

        if (DroneScene == null)
            return;

        for (int i = 0; i < _upgrades.CommanderDrones; i++)
        {
            CommanderDrone drone = DroneScene.Instantiate<CommanderDrone>();
            GetTree().CurrentScene.AddChild(drone);
            drone.Init(_player, i, _upgrades.CommanderDrones, _upgrades);
            _drones.Add(drone);
        }
    }

    private void ClearDrones()
    {
        foreach (var drone in _drones)
        {
            if (IsInstanceValid(drone))
                drone.QueueFree();
        }

        _drones.Clear();
    }
}