using Godot;

public partial class DropPickup : Area2D
{
    [Export] public int Value = 1;

    [Export] public float DriftSpeed = 18f;
    [Export] public float FloatAmplitude = 8f;
    [Export] public float FloatFrequency = 2.0f;
    [Export] public float RotateSpeed = 1.2f;

    [Export] public float Lifetime = 10f;
    [Export] public float BlinkStartTime = 3f;
    [Export] public float BlinkInterval = 0.15f;

    private float _time = 0f;
    private float _lifetimeTimer = 0f;
    private float _blinkTimer = 0f;
    private bool _visibleState = true;

    private Vector2 _basePosition;
    private Vector2 _driftVelocity = Vector2.Zero;
    private bool _initialized = false;

    private Sprite2D _sprite;

    public override void _Ready()
    {
        AddToGroup("pickup");

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        BodyEntered += OnBodyEntered;

        if (!_initialized)
        {
            _basePosition = GlobalPosition;
            _lifetimeTimer = Lifetime;
            _initialized = true;
        }
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public void Initialize(Vector2 spawnPosition, Vector2 driftDirection)
    {
        GlobalPosition = spawnPosition;
        _basePosition = spawnPosition;

        if (driftDirection == Vector2.Zero)
            driftDirection = Vector2.Right.Rotated((float)GD.RandRange(0f, Mathf.Tau));

        _driftVelocity = driftDirection.Normalized() * DriftSpeed;
        _lifetimeTimer = Lifetime;
        _time = 0f;
        _blinkTimer = 0f;
        _visibleState = true;
        _initialized = true;

        SetSpriteVisible(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized)
            return;

        float d = (float)delta;

        _time += d;
        _basePosition += _driftVelocity * d;

        Vector2 wobble = new Vector2(0f, Mathf.Sin(_time * FloatFrequency) * FloatAmplitude);
        GlobalPosition = _basePosition + wobble;

        Rotation += RotateSpeed * d;

        _lifetimeTimer -= d;

        if (_lifetimeTimer <= BlinkStartTime)
            HandleBlink(d);

        if (_lifetimeTimer <= 0f)
            QueueFree();
    }

    private void HandleBlink(float delta)
    {
        _blinkTimer += delta;
        if (_blinkTimer >= BlinkInterval)
        {
            _blinkTimer = 0f;
            _visibleState = !_visibleState;
            SetSpriteVisible(_visibleState);
        }
    }

    private void SetSpriteVisible(bool visible)
    {
        if (_sprite == null)
            return;

        Color c = _sprite.Modulate;
        c.A = visible ? 1f : 0.15f;
        _sprite.Modulate = c;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body == null || !body.IsInGroup("player"))
            return;

        var lvl = GetTree().GetFirstNodeInGroup("lvl_ui") as Lvl;
        if (lvl != null)
            lvl.AddGears(Value);
        else
            GD.PushWarning("DropPickup: Lvl не найден в группе lvl_ui");

        QueueFree();
    }
}