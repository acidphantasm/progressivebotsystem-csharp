using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using _progressiveBotSystem.Models.Enums;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace _progressiveBotSystem.Models;

public class BotChancesData
{
    [JsonPropertyName("chances")]
    public ApbsChances Chances { get; set; }
    
}

public class ApbsChances
{
    [JsonPropertyName("equipment")]
    public Dictionary<string, double> EquipmentChances { get; set; }

    [JsonPropertyName("equipmentMods")]
    public Dictionary<string, double> EquipmentModsChances { get; set; }

    [JsonPropertyName("weaponMods")]
    public Dictionary<string, double> WeaponModsChances { get; set; }

    [JsonPropertyName("assaultCarbine")]
    public Dictionary<string, double> AssaultCarbineChances { get; set; }

    [JsonPropertyName("sniperRifle")]
    public Dictionary<string, double> SniperRifleChances { get; set; }

    [JsonPropertyName("marksmanRifle")]
    public Dictionary<string, double> MarksmanRifleChances { get; set; }

    [JsonPropertyName("assaultRifle")]
    public Dictionary<string, double> AssaultRifleChances { get; set; }

    [JsonPropertyName("machinegun")]
    public Dictionary<string, double> MachineGunChances { get; set; }

    [JsonPropertyName("smg")]
    public Dictionary<string, double> SubmachineGunChances { get; set; }

    [JsonPropertyName("handgun")]
    public Dictionary<string, double> HandgunChances { get; set; }

    [JsonPropertyName("revolver")]
    public Dictionary<string, double> RevolverChances { get; set; }

    [JsonPropertyName("shotgun")]
    public Dictionary<string, double> ShotgunChances { get; set; }
    
    [JsonPropertyName("generation")]
    public ApbsGeneration Generation { get; set; }
}

public class ApbsGeneration
{
    [JsonPropertyName("items")]
    public ApbsGenerationWeightingItems Items { get; set; }
}

public record ApbsGenerationWeightingItems
{
    [JsonPropertyName("grenades")]
    public ApbsGenerationData Grenades { get; set; }

    [JsonPropertyName("healing")]
    public ApbsGenerationData Healing { get; set; }

    [JsonPropertyName("drugs")]
    public ApbsGenerationData Drugs { get; set; }

    [JsonPropertyName("food")]
    public ApbsGenerationData Food { get; set; }

    [JsonPropertyName("drink")]
    public ApbsGenerationData Drink { get; set; }

    [JsonPropertyName("currency")]
    public ApbsGenerationData Currency { get; set; }

    [JsonPropertyName("stims")]
    public ApbsGenerationData Stims { get; set; }

    [JsonPropertyName("backpackLoot")]
    public ApbsGenerationData BackpackLoot { get; set; }

    [JsonPropertyName("pocketLoot")]
    public ApbsGenerationData PocketLoot { get; set; }

    [JsonPropertyName("vestLoot")]
    public ApbsGenerationData VestLoot { get; set; }

    [JsonPropertyName("magazines")]
    public ApbsGenerationData Magazines { get; set; }

    [JsonPropertyName("specialItems")]
    public ApbsGenerationData SpecialItems { get; set; }
}
public record ApbsGenerationData
{
    [JsonPropertyName("weights")]
    public Dictionary<double, double> Weights { get; set; }
    
    [JsonPropertyName("whitelist")]
    public Dictionary<MongoId, double> Whitelist { get; set; }
}
public class ApbsEquipmentBot
{
    [JsonPropertyName("equipment")]
    public Dictionary<ApbsEquipmentSlots, Dictionary<MongoId, double>> Equipment { get; set; }
}