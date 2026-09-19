using Godot;

public enum CaseState
{
    Locked,
    Active,
    Ready,
    Opened
}

public partial class CaseZone : Node2D
{
    [Signal] public delegate void OpenedEventHandler(CaseZone zone);

    // Награда за открытие (использует существующие системы Health / Lvl).
    [Export(PropertyHint.Range, "0,1,0.05")] public float RewardHealPercent = 0.5f;
    [Export] public int RewardGears = 40;

    // Цвета акцентирования по состояниям (SVG сделан в белых тонах, цвет задаётся через Modulate).
    [Export] public Color ActiveTint = new Color(0.35f, 0.8f, 1f);
    [Export] public Color ReadyTint = new Color(1f, 0.82f, 0.25f);
    [Export] public Color LockedTint = new Color(0.55f, 0.6f, 0.65f);

    private Sprite2D _caseSprite;
    private Sprite2D _ring;
    private Sprite2D _glow;
    private Area2D _trigger;
    private CaseProgressUI _ui;

    private CaseState _state = CaseState.Locked;
    private int _caseIndex = 0;
    private int _requiredKills = 20;
    private int _currentKills = 0;
    private float _pulse = 0f;

    public int CaseIndex => _caseIndex;
    public CaseState State => _state;

    public override void _Ready()
    {
        AddToGroup("case_zone");

        _caseSprite = GetNodeOrNull<Sprite2D>("CaseSprite");
        _ring = GetNodeOrNull<Sprite2D>("Ring");
        _glow = GetNodeOrNull<Sprite2D>("Glow");
        _trigger = GetNodeOrNull<Area2D>("TriggerArea");
        _ui = GetNodeOrNull<CaseProgressUI>("CaseProgressUI");

        if (_trigger != null)
            _trigger.BodyEntered += OnBodyEntered;

        SetState(CaseState.Locked);
    }

    public override void _ExitTree()
    {
        if (_trigger != null)
            _trigger.BodyEntered -= OnBodyEntered;
    }

    // Вызывается спавнером кейсов: задаёт индекс, требуемое число убийств
    // и запускает появление (locked → active).
    public void Configure(int caseIndex, int requiredKills)
    {
        _caseIndex = caseIndex;
        _requiredKills = Mathf.Max(1, requiredKills);
        _currentKills = 0;

        Scale = new Vector2(0f, 0f);
        Modulate = ChangeAlpha(Modulate, 0f);

        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector2.One, 0.3f);
        tween.Parallel().TweenMethod(new Callable(this, nameof(TweenAlpha)), 0f, 1f, 0.3f);
        tween.TweenCallback(new Callable(this, nameof(ActivateFromLocked)));
    }

    public void ActivateFromLocked()
    {
        SetState(CaseState.Active);
    }

    public void SetKillProgress(int kills)
    {
        if (_state != CaseState.Active)
            return;

        _currentKills = Mathf.Clamp(kills, 0, _requiredKills);

        if (_currentKills >= _requiredKills)
        {
            SetState(CaseState.Ready);
            return;
        }

        UpdateUi();
    }

    private void SetState(CaseState next)
    {
        if (_state == CaseState.Opened)
            return;

        _state = next;
        UpdateUi();
        UpdateVisuals();
    }

    private void UpdateUi()
    {
        if (_ui == null)
            return;

        if (_state == CaseState.Opened)
        {
            _ui.ShowOpened();
            return;
        }

        _ui.ShowProgress(_currentKills, _requiredKills, _state == CaseState.Ready, _state == CaseState.Locked);
    }

    private void UpdateVisuals()
    {
        switch (_state)
        {
            case CaseState.Locked:
                ApplyTone(LockedTint, 0.8f);
                break;
            case CaseState.Active:
                ApplyTone(ActiveTint, 1f);
                break;
            case CaseState.Ready:
                ApplyTone(ReadyTint, 1f);
                break;
            case CaseState.Opened:
                ApplyTone(ReadyTint, 1f);
                break;
        }

        if (_glow != null)
            _glow.Visible = _state != CaseState.Opened;
    }

    private void ApplyTone(Color color, float alpha)
    {
        if (_caseSprite != null)
            _caseSprite.Modulate = ChangeAlpha(color, alpha);

        if (_ring != null)
            _ring.Modulate = ChangeAlpha(color, 0.85f);

        if (_glow != null)
            _glow.Modulate = ChangeAlpha(color, 0.45f);
    }

    public override void _Process(double delta)
    {
        _pulse += (float)delta;

        if (_state == CaseState.Ready)
        {
            // Пульс, когда кейс готов к открытию.
            float t = (Mathf.Sin(_pulse * 4f) + 1f) * 0.5f;

            if (_glow != null)
                _glow.Modulate = ChangeAlpha(ReadyTint, 0.35f + 0.55f * t);

            if (_ring != null)
                _ring.Scale = Vector2.One * (1f + 0.1f * t);

            if (_caseSprite != null)
                _caseSprite.Scale = Vector2.One * (1f + 0.06f * t);
        }
    }

    private void TweenAlpha(float alpha)
    {
        Color c = Modulate;
        c.A = alpha;
        Modulate = c;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_state != CaseState.Ready)
            return;

        if (body == null || !body.IsInGroup("player"))
            return;

        Open();
    }

    private void Open()
    {
        if (_state == CaseState.Opened)
            return;

        SetState(CaseState.Opened);

        // Показываем выбор награды (3 карточки) отложенно — не в физик-колбэке.
        CallDeferred(nameof(ShowRewardChoice));

        // Сигнал слушателю (CaseManager) — но не освобождаемся внутри колбэка физики.
        CallDeferred(nameof(EmitOpenedSignal));
    }

    private void ShowRewardChoice()
    {
        var rewardUi = GetTree().GetFirstNodeInGroup("reward_ui") as RewardUI;
        if (rewardUi != null && GodotObject.IsInstanceValid(rewardUi))
            rewardUi.Open();
        else
            GrantReward();
    }

    private void EmitOpenedSignal()
    {
        EmitSignal(SignalName.Opened, this);
    }

    private void GrantReward()
    {
        if (GetTree().GetFirstNodeInGroup("player") is Node2D player)
        {
            var health = player.GetNodeOrNull<Health>("Health");
            if (health != null && RewardHealPercent > 0f)
                health.Heal(Mathf.Max(1f, health.MaxHP * RewardHealPercent));
        }

        var lvl = GetTree().GetFirstNodeInGroup("lvl_ui") as Lvl;
        if (lvl != null)
            lvl.AddGears(RewardGears);
        else
            GD.PushWarning("CaseZone: Lvl не найден в группе lvl_ui");
    }

    private static Color ChangeAlpha(Color c, float alpha)
    {
        c.A = Mathf.Clamp(alpha, 0f, 1f);
        return c;
    }
}