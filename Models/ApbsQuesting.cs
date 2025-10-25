using System.Text.Json.Serialization;

namespace _progressiveBotSystem.Models;

public class QuestBotGenerationDetails
{
    public bool IsQuesting { get; set; }
    public required QuestData QuestBotGear { get; set; }
}

public class QuestDataJson
{
    public required List<QuestData> AvailableQuests { get; set; }
}
public class QuestData
{
    [JsonPropertyName("isQuestEnabled")]
    public bool IsQuestEnabled { get; set; }
    [JsonPropertyName("questName")]
    public required string QuestName { get; set; }
    [JsonPropertyName("requiredMap")]
    public required List<string> RequiredMap { get; set; }
    [JsonPropertyName("requiredWeaponMods")]
    public required List<string> RequiredWeaponMods { get; set; }
    [JsonPropertyName("requiredWeaponModSlots")]
    public required List<string> RequiredWeaponModSlots { get; set; }
    [JsonPropertyName("requiredWeaponModBaseClasses")]
    public required List<string> RequiredWeaponModBaseClasses { get; set; }
    [JsonPropertyName("requiredEquipmentSlots")]
    public required List<string> RequiredEquipmentSlots { get; set; }
    public required List<string> Headwear { get; set; }
    public required List<string> Earpiece { get; set; }
    public required List<string> FaceCover { get; set; }
    public required List<string> ArmorVest { get; set; }
    public required List<string> Eyewear { get; set; }
    public required List<string> TacticalVest { get; set; }
    public required List<string> Backpack { get; set; }
    public required List<string> PrimaryWeapon { get; set; }
    public required List<string> Holster { get; set; }
    public required List<string> Scabbard { get; set; }
    [JsonPropertyName("minLevel")]
    public int MinLevel { get; set; }
    [JsonPropertyName("maxLevel")]
    public int MaxLevel { get; set; }
    
    public object this[string key]
    {
        get => key switch
        {
            nameof(IsQuestEnabled) => IsQuestEnabled,
            nameof(QuestName) => QuestName,
            nameof(RequiredMap) => RequiredMap,
            nameof(RequiredWeaponMods) => RequiredWeaponMods,
            nameof(RequiredWeaponModSlots) => RequiredWeaponModSlots,
            nameof(RequiredWeaponModBaseClasses) => RequiredWeaponModBaseClasses,
            nameof(RequiredEquipmentSlots) => RequiredEquipmentSlots,
            nameof(Headwear) => Headwear,
            nameof(Earpiece) => Earpiece,
            nameof(FaceCover) => FaceCover,
            nameof(ArmorVest) => ArmorVest,
            nameof(Eyewear) => Eyewear,
            nameof(TacticalVest) => TacticalVest,
            nameof(Backpack) => Backpack,
            nameof(PrimaryWeapon) => PrimaryWeapon,
            nameof(Holster) => Holster,
            nameof(Scabbard) => Scabbard,
            nameof(MinLevel) => MinLevel,
            nameof(MaxLevel) => MaxLevel,
            _ => throw new KeyNotFoundException($"Property '{key}' not found")
        };
    }
}