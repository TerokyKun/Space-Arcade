using Godot;
using System;
using System.Collections.Generic;

public partial class UpgradeDatabase : Node
{
    public static UpgradeDatabase Instance { get; private set; }

    [Export] public string JsonPath = "res://resources/upgrades.json";

    public List<UpgradeDefinition> ClassChoices { get; private set; } = new();
    public List<UpgradeDefinition> StatChoices { get; private set; } = new();
    public List<UpgradeDefinition> EpicChoices { get; private set; } = new();
    public List<UpgradeDefinition> LegendaryChoices { get; private set; } = new();
    public List<UpgradeDefinition> MythicChoices { get; private set; } = new();

    // Все апгрейды кроме выбора класса — единый пул с учётом редкости.
    public List<UpgradeDefinition> AllChoices
    {
        get
        {
            var combined = new List<UpgradeDefinition>();
            combined.AddRange(StatChoices);
            combined.AddRange(EpicChoices);
            combined.AddRange(LegendaryChoices);
            combined.AddRange(MythicChoices);
            return combined;
        }
    }

    public override void _Ready()
    {
        Instance = this;
        AddToGroup("upgrade_database");
        LoadFromJson();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadFromJson()
    {
        ClassChoices.Clear();
        StatChoices.Clear();
        EpicChoices.Clear();
        LegendaryChoices.Clear();
        MythicChoices.Clear();

        if (!FileAccess.FileExists(JsonPath))
        {
            GD.PushError($"UpgradeDatabase: JSON не найден: {JsonPath}");
            return;
        }

        string jsonText = FileAccess.GetFileAsString(JsonPath);
        Variant parsed = Json.ParseString(jsonText);

        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            GD.PushError("UpgradeDatabase: JSON корень должен быть объектом");
            return;
        }

        var root = (Godot.Collections.Dictionary)parsed;

        LoadList(root, "classes", ClassChoices, true, UpgradeRarity.Rare);
        LoadList(root, "stats", StatChoices, false, UpgradeRarity.Common);
        LoadList(root, "epic", EpicChoices, false, UpgradeRarity.Epic);
        LoadList(root, "legendary", LegendaryChoices, false, UpgradeRarity.Legendary);
        LoadList(root, "mythic", MythicChoices, false, UpgradeRarity.Mythic);
    }

    private void LoadList(
        Godot.Collections.Dictionary root,
        string key,
        List<UpgradeDefinition> target,
        bool classList,
        UpgradeRarity defaultRarity
    )
    {
        if (!root.ContainsKey(key))
            return;

        if (root[key].VariantType != Variant.Type.Array)
            return;

        var arr = (Godot.Collections.Array)root[key];

        foreach (Variant item in arr)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;

            var data = (Godot.Collections.Dictionary)item;

            var def = new UpgradeDefinition
            {
                Id = GetString(data, "id", ""),
                Kind = ParseKind(GetString(data, "kind", classList ? "ChooseClass" : "MaxHP")),
                Title = GetString(data, "title", ""),
                Description = GetString(data, "description", ""),
                Value = GetFloat(data, "value", 0f),
                IntParam1 = GetInt(data, "int_param1", 0),
                IntParam2 = GetInt(data, "int_param2", 0),
                ClassChoice = ParseClass(GetString(data, "class_choice", "None")),
                Weight = GetFloat(data, "weight", 1f),
                Rarity = ParseRarity(GetString(data, "rarity", ""), defaultRarity),
                Icon = LoadIcon(GetString(data, "icon", ""))
            };

            if (classList && def.Kind != UpgradeKind.ChooseClass)
                def.Kind = UpgradeKind.ChooseClass;

            target.Add(def);
        }
    }

    private static Texture2D LoadIcon(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return GD.Load<Texture2D>(path);
    }

    private static string GetString(Godot.Collections.Dictionary dict, string key, string fallback)
    {
        if (!dict.ContainsKey(key) || dict[key].VariantType == Variant.Type.Nil)
            return fallback;

        return dict[key].AsString();
    }

    private static float GetFloat(Godot.Collections.Dictionary dict, string key, float fallback)
    {
        if (!dict.ContainsKey(key) || dict[key].VariantType == Variant.Type.Nil)
            return fallback;

        return dict[key].AsSingle();
    }

    private static int GetInt(Godot.Collections.Dictionary dict, string key, int fallback)
    {
        if (!dict.ContainsKey(key) || dict[key].VariantType == Variant.Type.Nil)
            return fallback;

        return dict[key].AsInt32();
    }

    private static UpgradeKind ParseKind(string value)
    {
        return Enum.TryParse(value, true, out UpgradeKind result) ? result : UpgradeKind.MaxHP;
    }

    private static PlayerClassType ParseClass(string value)
    {
        return Enum.TryParse(value, true, out PlayerClassType result) ? result : PlayerClassType.None;
    }

    private static UpgradeRarity ParseRarity(string value, UpgradeRarity fallback)
    {
        return Enum.TryParse(value, true, out UpgradeRarity result) ? result : fallback;
    }
}