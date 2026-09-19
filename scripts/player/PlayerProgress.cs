using Godot;

public partial class PlayerProgress : Node
{
    [Signal] public delegate void ExpChangedEventHandler(float currentExp, float maxExp, int level);
    [Signal] public delegate void LevelUpEventHandler(int newLevel);

    [Export] public int Level = 1;
    [Export] public float CurrentExp = 0f;
    [Export] public float ExpToNextLevel = 100f;
    [Export] public float ExpGrowth = 1.25f;

    public void AddExp(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentExp += amount;

        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            Level++;

            EmitSignal(SignalName.LevelUp, Level);

            ExpToNextLevel = Mathf.Ceil(ExpToNextLevel * ExpGrowth);
        }

        EmitSignal(SignalName.ExpChanged, CurrentExp, ExpToNextLevel, Level);
    }
}