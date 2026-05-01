using System.Collections;
using System.Text.Json.Serialization;

namespace _progressiveBotSystem.Models;

public class ApbsServerConfig
{
    [JsonPropertyName("usePreset")]
    public bool UsePreset { get; set; } = false;

    [JsonPropertyName("presetName")]
    public string PresetName { get; set; } = "";

    [JsonPropertyName("compatibilityConfig")]
    public ModCompatibilityConfig CompatibilityConfig { get; set; } = new();

    [JsonPropertyName("normalizedHealthPool")]
    public NormalizeHealthConfig NormalizedHealthPool { get; set; } = new();

    [JsonPropertyName("playerScavConfig")]
    public PlayerScavConfig PlayerScavConfig { get; set; } = new();

    [JsonPropertyName("generalConfig")]
    public GeneralConfig GeneralConfig { get; set; } = new();

    [JsonPropertyName("pmcBots")]
    public PmcBotData PmcBots { get; set; } = new();

    [JsonPropertyName("scavBots")]
    public ScavBotData ScavBots { get; set; } = new();

    [JsonPropertyName("bossBots")]
    public GeneralBotData BossBots { get; set; } = new();

    [JsonPropertyName("followerBots")]
    public GeneralBotData FollowerBots { get; set; } = new();

    [JsonPropertyName("specialBots")]
    public GeneralBotData SpecialBots { get; set; } = new();

    [JsonPropertyName("customLevelDeltas")]
    public CustomLevelDelta CustomLevelDeltas { get; set; } = new();

    [JsonPropertyName("customScavLevelDeltas")]
    public CustomLevelDelta CustomScavLevelDeltas { get; set; } = new();

    [JsonPropertyName("enableBotEquipmentLog")]
    public bool EnableBotEquipmentLog { get; set; } = true;

    [JsonPropertyName("enableDebugLog")]
    public bool EnableDebugLog { get; set; } = false;

    [JsonPropertyName("configAppSettings")]
    public ConfigAppSettings ConfigAppSettings { get; set; } = new();
}

public class PlayerScavConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("usePmcSkills")]
    public bool UsePmcSkills { get; set; } = false;

    [JsonPropertyName("writebackSkillsToPmc")]
    public bool WritebackSkillsToPmc { get; set; } = false;

    [JsonPropertyName("allowBossRegeneration")]
    public bool AllowBossRegeneration { get; set; } = false;

    [JsonPropertyName("chance")]
    public int Chance { get; set; } = 5;

    [JsonPropertyName("useBossHealth")]
    public bool UseBossHealth { get; set; } = false;

    [JsonPropertyName("allowedBosses")]
    public List<string> AllowedBosses { get; set; } = [];
}

public class GeneralBotData
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("resourceRandomization")]
    public ResourceRandomizationConfig ResourceRandomization { get; set; } = new();

    [JsonPropertyName("weaponDurability")]
    public WeaponDurabilityConfig WeaponDurability { get; set; } = new();

    [JsonPropertyName("armourDurability")]
    public ArmourDurabilityConfig ArmourDurability { get; set; } = new();

    [JsonPropertyName("lootConfig")]
    public LootConfig LootConfig { get; set; } = new();

    [JsonPropertyName("rerollConfig")]
    public EnableChance RerollConfig { get; set; } = new();

    [JsonPropertyName("toploadConfig")]
    public ToploadConfig ToploadConfig { get; set; } = new();
    
    [JsonPropertyName("skipBackPlateIfMissingFrontPlate")]
    public bool SkipBackPlateIfMissingFrontPlate { get; set; } = false;
    
    [JsonPropertyName("limitPlateClassToFrontPlateClass")]
    public bool LimitPlateClassToFrontPlateClass { get; set; } = false;
}

public class PmcBotData
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("resourceRandomization")]
    public ResourceRandomizationConfig ResourceRandomization { get; set; } = new()
    {
        Enable = true,
        FoodRateMaxChance = 10,
        FoodRateUsagePercent = 33,
        MedRateMaxChance = 10,
        MedRateUsagePercent = 33
    };

    [JsonPropertyName("weaponDurability")]
    public WeaponDurabilityConfig WeaponDurability { get; set; } = new()
    {
        Enable = true,
        Min = 95,
        Max = 100,
        MinDelta = 0,
        MaxDelta = 5,
        MinLimitPercent = 90,
        EnhancementChance = 10
    };

    [JsonPropertyName("armourDurability")]
    public ArmourDurabilityConfig ArmourDurability { get; set; } = new()
    {
        Enable = true,
        Min = 95,
        Max = 100,
        MinDelta = 0,
        MaxDelta = 5,
        MinLimitPercent = 90
    };

    [JsonPropertyName("lootConfig")]
    public LootConfig LootConfig { get; set; } = new()
    {
        Enable = false,
        Blacklist = []
    };

    [JsonPropertyName("rerollConfig")]
    public EnableChance RerollConfig { get; set; } = new()
    {
        Enable = false,
        Chance = 50
    };

    [JsonPropertyName("toploadConfig")]
    public ToploadConfig ToploadConfig { get; set; } = new()
    {
        Enable = true,
        Chance = 30,
        Percent = 30
    };
    
    [JsonPropertyName("skipBackPlateIfMissingFrontPlate")]
    public bool SkipBackPlateIfMissingFrontPlate { get; set; } = true;
    
    [JsonPropertyName("limitPlateClassToFrontPlateClass")]
    public bool LimitPlateClassToFrontPlateClass { get; set; } = true;
    
    [JsonPropertyName("questConfig")]
    public EnableChance QuestConfig { get; set; } = new()
    {
        Enable = true,
        Chance = 5
    };

    [JsonPropertyName("povertyConfig")]
    public EnableChance PovertyConfig { get; set; } = new()
    {
        Enable = true,
        Chance = 5
    };

    [JsonPropertyName("additionalOptions")]
    public PmcSpecificConfig AdditionalOptions { get; set; } = new();

    [JsonPropertyName("secrets")]
    public PmcSecrets Secrets { get; set; } = new();
}

public class ScavBotData
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("resourceRandomization")]
    public ResourceRandomizationConfig ResourceRandomization { get; set; } = new()
    {
        Enable = true,
        FoodRateMaxChance = 10,
        FoodRateUsagePercent = 10,
        MedRateMaxChance = 10,
        MedRateUsagePercent = 10
    };

    [JsonPropertyName("weaponDurability")]
    public WeaponDurabilityConfig WeaponDurability { get; set; } = new()
    {
        Enable = true,
        Min = 70,
        Max = 90,
        MinDelta = 5,
        MaxDelta = 20,
        MinLimitPercent = 50,
        EnhancementChance = 1
    };

    [JsonPropertyName("armourDurability")]
    public ArmourDurabilityConfig ArmourDurability { get; set; } = new()
    {
        Enable = true,
        Min = 70,
        Max = 90,
        MinDelta = 5,
        MaxDelta = 20,
        MinLimitPercent = 50
    };

    [JsonPropertyName("lootConfig")]
    public LootConfig LootConfig { get; set; } = new()
    {
        Enable = true,
        Blacklist = []
    };

    [JsonPropertyName("rerollConfig")]
    public EnableChance RerollConfig { get; set; } = new()
    {
        Enable = true,
        Chance = 50
    };

    [JsonPropertyName("toploadConfig")]
    public ToploadConfig ToploadConfig { get; set; } = new()
    {
        Enable = false,
        Chance = 30,
        Percent = 30
    };
    
    [JsonPropertyName("skipBackPlateIfMissingFrontPlate")]
    public bool SkipBackPlateIfMissingFrontPlate { get; set; } = false;
    
    [JsonPropertyName("limitPlateClassToFrontPlateClass")]
    public bool LimitPlateClassToFrontPlateClass { get; set; } = false;
    
    [JsonPropertyName("keyConfig")]
    public KeyConfig KeyConfig { get; set; } = new();

    [JsonPropertyName("additionalOptions")]
    public ScavSpecificConfig AdditionalOptions { get; set; } = new();

    [JsonPropertyName("secrets")]
    public ScavSecrets Secrets { get; set; } = new();
}

public class ApbsBlacklistConfig
{
    [JsonPropertyName("weaponBlacklist")]
    public TierBlacklistConfig WeaponBlacklist { get; set; } = new();

    [JsonPropertyName("equipmentBlacklist")]
    public TierBlacklistConfig EquipmentBlacklist { get; set; } = new();

    [JsonPropertyName("ammoBlacklist")]
    public TierBlacklistConfig AmmoBlacklist { get; set; } = new();

    [JsonPropertyName("attachmentBlacklist")]
    public TierBlacklistConfig AttachmentBlacklist { get; set; } = new();

    [JsonPropertyName("clothingBlacklist")]
    public TierBlacklistConfig ClothingBlacklist { get; set; } = new();
}

public class PmcSpecificConfig
{
    [JsonPropertyName("enablePrestiging")]
    public bool EnablePrestiging { get; set; } = true;

    [JsonPropertyName("enablePrestigeAnyLevel")]
    public bool EnablePrestigeAnyLevel { get; set; } = true;

    [JsonPropertyName("seasonalPmcAppearance")]
    public bool SeasonalPmcAppearance { get; set; } = true;

    [JsonPropertyName("ammoTierSliding")]
    public AmmoTierSlideConfig AmmoTierSliding { get; set; } = new();

    [JsonPropertyName("gameVersionDogtagChance")]
    public int GameVersionDogtagChance { get; set; } = 50;

    [JsonPropertyName("gameVersionWeighting")]
    public GameVersionWeightConfig GameVersionWeighting { get; set; } = new();
}

public class EnableChance
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("chance")]
    public int Chance { get; set; } = 50;
}

public class ToploadConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("chance")]
    public int Chance { get; set; } = 30;

    [JsonPropertyName("percent")]
    public int Percent { get; set; } = 30;
}

public class ScavSpecificConfig
{
    [JsonPropertyName("enableScavAttachmentTiering")]
    public bool EnableScavAttachmentTiering { get; set; } = false;

    [JsonPropertyName("enableScavEqualEquipmentTiering")]
    public bool EnableScavEqualEquipmentTiering { get; set; } = false;
}

public class ResourceRandomizationConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("foodRateMaxChance")]
    public int FoodRateMaxChance { get; set; } = 10;

    [JsonPropertyName("foodRateUsagePercent")]
    public int FoodRateUsagePercent { get; set; } = 33;

    [JsonPropertyName("medRateMaxChance")]
    public int MedRateMaxChance { get; set; } = 10;

    [JsonPropertyName("medRateUsagePercent")]
    public int MedRateUsagePercent { get; set; } = 33;
}

public class WeaponDurabilityConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("min")]
    public int Min { get; set; } = 95;

    [JsonPropertyName("max")]
    public int Max { get; set; } = 100;

    [JsonPropertyName("minDelta")]
    public int MinDelta { get; set; } = 0;

    [JsonPropertyName("maxDelta")]
    public int MaxDelta { get; set; } = 5;

    [JsonPropertyName("minLimitPercent")]
    public int MinLimitPercent { get; set; } = 90;

    [JsonPropertyName("enhancementChance")]
    public int EnhancementChance { get; set; } = 10;
}

public class ArmourDurabilityConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("min")]
    public int Min { get; set; } = 95;

    [JsonPropertyName("max")]
    public int Max { get; set; } = 100;

    [JsonPropertyName("minDelta")]
    public int MinDelta { get; set; } = 0;

    [JsonPropertyName("maxDelta")]
    public int MaxDelta { get; set; } = 5;

    [JsonPropertyName("minLimitPercent")]
    public int MinLimitPercent { get; set; } = 90;
}

public class LootConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("blacklist")]
    public List<string> Blacklist { get; set; } = [];
}

public class KeyConfig
{
    [JsonPropertyName("addAllKeysToScavs")]
    public bool AddAllKeysToScavs { get; set; } = false;

    [JsonPropertyName("addOnlyMechanicalKeysToScavs")]
    public bool AddOnlyMechanicalKeysToScavs { get; set; } = false;

    [JsonPropertyName("addOnlyKeyCardsToScavs")]
    public bool AddOnlyKeyCardsToScavs { get; set; } = false;

    [JsonPropertyName("keyProbability")]
    public double KeyProbability { get; set; } = 0.15;
}

public class GeneralConfig
{
    [JsonPropertyName("enablePerWeaponTypeAttachmentChances")]
    public bool EnablePerWeaponTypeAttachmentChances { get; set; } = true;

    [JsonPropertyName("enableLargeCapacityMagazineLimit")]
    public bool EnableLargeCapacityMagazineLimit { get; set; } = true;

    [JsonPropertyName("largeCapacityMagazineCount")]
    public int LargeCapacityMagazineCount { get; set; } = 2;

    [JsonPropertyName("forceStock")]
    public bool ForceStock { get; set; } = false;

    [JsonPropertyName("stockButtpadChance")]
    public int StockButtpadChance { get; set; } = 50;

    [JsonPropertyName("forceDustCover")]
    public bool ForceDustCover { get; set; } = false;

    [JsonPropertyName("forceScopeSlot")]
    public bool ForceScopeSlot { get; set; } = false;

    [JsonPropertyName("forceMuzzle")]
    public bool ForceMuzzle { get; set; } = false;

    [JsonPropertyName("muzzleChance")]
    public List<int> MuzzleChance { get; set; } = [10, 25, 40, 55, 65, 75, 75];

    [JsonPropertyName("forceChildrenMuzzle")]
    public bool ForceChildrenMuzzle { get; set; } = false;

    [JsonPropertyName("forceWeaponModLimits")]
    public bool ForceWeaponModLimits { get; set; } = true;

    [JsonPropertyName("scopeLimit")]
    public int ScopeLimit { get; set; } = 2;

    [JsonPropertyName("tacticalLimit")]
    public int TacticalLimit { get; set; } = 1;

    [JsonPropertyName("onlyChads")]
    public bool OnlyChads { get; set; } = false;

    [JsonPropertyName("tarkovAndChill")]
    public bool TarkovAndChill { get; set; } = false;

    [JsonPropertyName("blickyMode")]
    public bool BlickyMode { get; set; } = false;

    [JsonPropertyName("enableT7Thermals")]
    public bool EnableT7Thermals { get; set; } = false;

    [JsonPropertyName("startTier")]
    public int StartTier { get; set; } = 6;

    [JsonPropertyName("mapRangeWeighting")]
    public MapRangeWeights MapRangeWeighting { get; set; } = new();

    [JsonPropertyName("plateChances")]
    public PlateWeightConfig PlateChances { get; set; } = new();

    [JsonPropertyName("plateClasses")]
    public PlateClasses PlateClasses { get; set; } = new();
}

public class MapRangeWeights
{
    [JsonPropertyName("bigmap")]
    public LongShortRange Bigmap { get; set; } = new() { LongRange = 20, ShortRange = 80 };

    [JsonPropertyName("RezervBase")]
    public LongShortRange RezervBase { get; set; } = new() { LongRange = 20, ShortRange = 80 };

    [JsonPropertyName("laboratory")]
    public LongShortRange Laboratory { get; set; } = new() { LongRange = 10, ShortRange = 90 };

    [JsonPropertyName("factory4_night")]
    public LongShortRange Factory4Night { get; set; } = new() { LongRange = 5, ShortRange = 95 };

    [JsonPropertyName("factory4_day")]
    public LongShortRange Factory4Day { get; set; } = new() { LongRange = 5, ShortRange = 95 };

    [JsonPropertyName("Interchange")]
    public LongShortRange Interchange { get; set; } = new() { LongRange = 20, ShortRange = 80 };

    [JsonPropertyName("Sandbox")]
    public LongShortRange Sandbox { get; set; } = new() { LongRange = 15, ShortRange = 85 };

    [JsonPropertyName("Sandbox_high")]
    public LongShortRange SandboxHigh { get; set; } = new() { LongRange = 15, ShortRange = 85 };

    [JsonPropertyName("Woods")]
    public LongShortRange Woods { get; set; } = new() { LongRange = 60, ShortRange = 40 };

    [JsonPropertyName("Shoreline")]
    public LongShortRange Shoreline { get; set; } = new() { LongRange = 50, ShortRange = 50 };

    [JsonPropertyName("Lighthouse")]
    public LongShortRange Lighthouse { get; set; } = new() { LongRange = 30, ShortRange = 70 };

    [JsonPropertyName("TarkovStreets")]
    public LongShortRange TarkovStreets { get; set; } = new() { LongRange = 20, ShortRange = 80 };

    [JsonPropertyName("Labyrinth")]
    public LongShortRange Labyrinth { get; set; } = new() { LongRange = 5, ShortRange = 95 };

    private Dictionary<string, LongShortRange> ToDictionary() => new()
    {
        ["bigmap"] = Bigmap,
        ["RezervBase"] = RezervBase,
        ["laboratory"] = Laboratory,
        ["factory4_night"] = Factory4Night,
        ["factory4_day"] = Factory4Day,
        ["Interchange"] = Interchange,
        ["Sandbox"] = Sandbox,
        ["Sandbox_high"] = SandboxHigh,
        ["Woods"] = Woods,
        ["Shoreline"] = Shoreline,
        ["Lighthouse"] = Lighthouse,
        ["TarkovStreets"] = TarkovStreets,
        ["Labyrinth"] = Labyrinth
    };

    public LongShortRange this[string key]
    {
        get
        {
            return key switch
            {
                "bigmap" => Bigmap,
                "RezervBase" => RezervBase,
                "laboratory" => Laboratory,
                "factory4_night" => Factory4Night,
                "factory4_day" => Factory4Day,
                "Interchange" => Interchange,
                "Sandbox" => Sandbox,
                "Sandbox_high" => SandboxHigh,
                "Woods" => Woods,
                "Shoreline" => Shoreline,
                "Lighthouse" => Lighthouse,
                "TarkovStreets" => TarkovStreets,
                "Labyrinth" => Labyrinth,
                _ => throw new KeyNotFoundException($"Map '{key}' not found.")
            };
        }
    }
}

public class LongShortRange
{
    [JsonPropertyName("LongRange")]
    public double LongRange { get; set; } = 20;

    [JsonPropertyName("ShortRange")]
    public double ShortRange { get; set; } = 80;

    public Dictionary<string, double> ToDictionary()
        => new Dictionary<string, double>(2)
        {
            ["LongRange"] = LongRange,
            ["ShortRange"] = ShortRange
        };
}

public class PlateWeightConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("pmcMainPlateChance")]
    public List<int> PmcMainPlateChance { get; set; } = [65, 75, 90, 90, 95, 100, 100];

    [JsonPropertyName("pmcSidePlateChance")]
    public List<int> PmcSidePlateChance { get; set; } = [15, 25, 35, 55, 75, 95, 100];

    [JsonPropertyName("scavMainPlateChance")]
    public List<int> ScavMainPlateChance { get; set; } = [25, 25, 25, 25, 25, 25, 25];

    [JsonPropertyName("scavSidePlateChance")]
    public List<int> ScavSidePlateChance { get; set; } = [10, 10, 10, 10, 10, 10, 10];

    [JsonPropertyName("bossMainPlateChance")]
    public List<int> BossMainPlateChance { get; set; } = [90, 90, 90, 90, 90, 90, 90];

    [JsonPropertyName("bossSidePlateChance")]
    public List<int> BossSidePlateChance { get; set; } = [75, 75, 75, 75, 75, 75, 75];

    [JsonPropertyName("followerMainPlateChance")]
    public List<int> FollowerMainPlateChance { get; set; } = [75, 75, 75, 75, 75, 75, 75];

    [JsonPropertyName("followerSidePlateChance")]
    public List<int> FollowerSidePlateChance { get; set; } = [50, 50, 50, 50, 50, 50, 50];

    [JsonPropertyName("specialMainPlateChance")]
    public List<int> SpecialMainPlateChance { get; set; } = [75, 75, 75, 75, 75, 75, 75];

    [JsonPropertyName("specialSidePlateChance")]
    public List<int> SpecialSidePlateChance { get; set; } = [50, 50, 50, 50, 50, 50, 50];
}

public class PlateClasses
{
    [JsonPropertyName("pmc")]
    public PlateClassList Pmc { get; set; } = new PlateClassList
    {
        Tier1 = DefaultPmcTier(new() { ["2"] = 0, ["3"] = 90, ["4"] = 10, ["5"] = 0, ["6"] = 0 }),
        Tier2 = DefaultPmcTier(new() { ["2"] = 0, ["3"] = 65, ["4"] = 33, ["5"] = 2, ["6"] = 0 }),
        Tier3 = DefaultPmcTier(new() { ["2"] = 0, ["3"] = 16, ["4"] = 75, ["5"] = 7, ["6"] = 2 }),
        Tier4 = DefaultPmcTier(new() { ["2"] = 0, ["3"] = 1,  ["4"] = 50, ["5"] = 39, ["6"] = 10 }),
        Tier5 = DefaultPmcTier(new() { ["2"] = 0, ["3"] = 0,  ["4"] = 20, ["5"] = 70, ["6"] = 10 }),
        Tier6 = DefaultPmcTier(new() { ["2"] = 0, ["3"] = 0,  ["4"] = 10, ["5"] = 70, ["6"] = 20 }),
        Tier7 = DefaultPmcTier(new() { ["2"] = 0, ["3"] = 0,  ["4"] = 5,  ["5"] = 75, ["6"] = 20 }),
    };

    [JsonPropertyName("scav")]
    public PlateClassList Scav { get; set; } = new PlateClassList
    {
        Tier1 = DefaultScavTier(),
        Tier2 = DefaultScavTier(),
        Tier3 = DefaultScavTier(),
        Tier4 = DefaultScavTier(),
        Tier5 = DefaultScavTier(),
        Tier6 = DefaultScavTier(),
        Tier7 = DefaultScavTier(),
    };

    [JsonPropertyName("bossAndSpecial")]
    public PlateClassList BossAndSpecial { get; set; } = new PlateClassList
    {
        Tier1 = DefaultBossTier(),
        Tier2 = DefaultBossTier(),
        Tier3 = DefaultBossTier(),
        Tier4 = DefaultBossTier(),
        Tier5 = DefaultBossTier(),
        Tier6 = DefaultBossTier(),
        Tier7 = DefaultBossTier(),
    };

    private static Dictionary<string, Dictionary<string, double>> DefaultPmcTier(Dictionary<string, double> weights)
        => new()
        {
            ["front_plate"] = weights,
            ["back_plate"] = weights,
            ["left_side_plate"] = weights,
            ["right_side_plate"] = weights,
        };

    private static Dictionary<string, Dictionary<string, double>> DefaultScavTier() => DefaultPmcTier(new() { ["2"] = 0, ["3"] = 85, ["4"] = 9, ["5"] = 5, ["6"] = 1 });
    private static Dictionary<string, Dictionary<string, double>> DefaultBossTier() => DefaultPmcTier(new() { ["2"] = 0, ["3"] = 10, ["4"] = 50, ["5"] = 35, ["6"] = 5 });
}

public class PlateClassList
{
    [JsonPropertyName("tier1")]
    public required Dictionary<string, Dictionary<string, double>> Tier1 { get; set; }

    [JsonPropertyName("tier2")]
    public required Dictionary<string, Dictionary<string, double>> Tier2 { get; set; }

    [JsonPropertyName("tier3")]
    public required Dictionary<string, Dictionary<string, double>> Tier3 { get; set; }

    [JsonPropertyName("tier4")]
    public required Dictionary<string, Dictionary<string, double>> Tier4 { get; set; }

    [JsonPropertyName("tier5")]
    public required Dictionary<string, Dictionary<string, double>> Tier5 { get; set; }

    [JsonPropertyName("tier6")]
    public required Dictionary<string, Dictionary<string, double>> Tier6 { get; set; }

    [JsonPropertyName("tier7")]
    public required Dictionary<string, Dictionary<string, double>> Tier7 { get; set; }
}

public class AmmoTierSlideConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("slideAmount")]
    public int SlideAmount { get; set; } = 1;

    [JsonPropertyName("slideChance")]
    public int SlideChance { get; set; } = 33;
}

public class GameVersionWeightConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("standard")]
    public int Standard { get; set; } = 20;

    [JsonPropertyName("leftBehind")]
    public int LeftBehind { get; set; } = 10;

    [JsonPropertyName("prepareForEscape")]
    public int PrepareForEscape { get; set; } = 10;

    [JsonPropertyName("edgeOfDarkness")]
    public int EdgeOfDarkness { get; set; } = 30;

    [JsonPropertyName("unheardEdition")]
    public int UnheardEdition { get; set; } = 20;
}

public class NormalizeHealthConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("setHead")]
    public bool SetHead { get; set; } = true;

    [JsonPropertyName("healthHead")]
    public int HealthHead { get; set; } = 35;

    [JsonPropertyName("setChest")]
    public bool SetChest { get; set; } = true;

    [JsonPropertyName("healthChest")]
    public int HealthChest { get; set; } = 85;

    [JsonPropertyName("setStomach")]
    public bool SetStomach { get; set; } = true;

    [JsonPropertyName("healthStomach")]
    public int HealthStomach { get; set; } = 70;

    [JsonPropertyName("setLeftArm")]
    public bool SetLeftArm { get; set; } = true;

    [JsonPropertyName("healthLeftArm")]
    public int HealthLeftArm { get; set; } = 60;

    [JsonPropertyName("setRightArm")]
    public bool SetRightArm { get; set; } = true;

    [JsonPropertyName("healthRightArm")]
    public int HealthRightArm { get; set; } = 60;

    [JsonPropertyName("setLeftLeg")]
    public bool SetLeftLeg { get; set; } = true;

    [JsonPropertyName("healthLeftLeg")]
    public int HealthLeftLeg { get; set; } = 65;

    [JsonPropertyName("setRightLeg")]
    public bool SetRightLeg { get; set; } = true;

    [JsonPropertyName("healthRightLeg")]
    public int HealthRightLeg { get; set; } = 65;

    [JsonPropertyName("excludedBots")]
    public List<string> ExcludedBots { get; set; } = [];

    [JsonPropertyName("normalizeSkills")]
    public bool NormalizeSkills { get; set; } = false;
}

public class TierBlacklistConfig
{
    [JsonPropertyName("tier1Blacklist")]
    public List<string> Tier1Blacklist { get; set; } = [];

    [JsonPropertyName("tier2Blacklist")]
    public List<string> Tier2Blacklist { get; set; } = [];

    [JsonPropertyName("tier3Blacklist")]
    public List<string> Tier3Blacklist { get; set; } = [];

    [JsonPropertyName("tier4Blacklist")]
    public List<string> Tier4Blacklist { get; set; } = [];

    [JsonPropertyName("tier5Blacklist")]
    public List<string> Tier5Blacklist { get; set; } = [];

    [JsonPropertyName("tier6Blacklist")]
    public List<string> Tier6Blacklist { get; set; } = [];

    [JsonPropertyName("tier7Blacklist")]
    public List<string> Tier7Blacklist { get; set; } = [];
}

public class CustomLevelDelta
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("tier1")]
    public MinMax Tier1 { get; set; } = new() { Min = 10, Max = 5 };

    [JsonPropertyName("tier2")]
    public MinMax Tier2 { get; set; } = new() { Min = 10, Max = 5 };

    [JsonPropertyName("tier3")]
    public MinMax Tier3 { get; set; } = new() { Min = 15, Max = 7 };

    [JsonPropertyName("tier4")]
    public MinMax Tier4 { get; set; } = new() { Min = 20, Max = 10 };

    [JsonPropertyName("tier5")]
    public MinMax Tier5 { get; set; } = new() { Min = 30, Max = 15 };

    [JsonPropertyName("tier6")]
    public MinMax Tier6 { get; set; } = new() { Min = 40, Max = 20 };

    [JsonPropertyName("tier7")]
    public MinMax Tier7 { get; set; } = new() { Min = 50, Max = 20 };
}

public class MinMax
{
    [JsonPropertyName("min")]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }
}

public class ModCompatibilityConfig
{
    [JsonPropertyName("enableModdedWeapons")]
    public bool EnableModdedWeapons { get; set; } = false;

    [JsonPropertyName("pmcWeaponWeights")]
    public int PmcWeaponWeights { get; set; } = 8;

    [JsonPropertyName("scavWeaponWeights")]
    public int ScavWeaponWeights { get; set; } = 1;

    [JsonPropertyName("followerWeaponWeights")]
    public int FollowerWeaponWeights { get; set; } = 6;

    [JsonPropertyName("enableModdedEquipment")]
    public bool EnableModdedEquipment { get; set; } = false;

    [JsonPropertyName("armBandWeight")]
    public int ArmBandWeight { get; set; } = 4;

    [JsonPropertyName("armourVestWeight")]
    public int ArmourVestWeight { get; set; } = 10;

    [JsonPropertyName("armouredRigWeight")]
    public int ArmouredRigWeight { get; set; } = 7;

    [JsonPropertyName("backpackWeight")]
    public int BackpackWeight { get; set; } = 5;

    [JsonPropertyName("eyewearWeight")]
    public int EyewearWeight { get; set; } = 1;

    [JsonPropertyName("earpieceWeight")]
    public int EarpieceWeight { get; set; } = 5;

    [JsonPropertyName("faceCoverAc2Weight")]
    public int FaceCoverAc2Weight { get; set; } = 1;

    [JsonPropertyName("faceCoverAc0Weight")]
    public int FaceCoverAc0Weight { get; set; } = 2;

    [JsonPropertyName("faceCoverWeight")]
    public int FaceCoverWeight { get; set; } = 4;

    [JsonPropertyName("headwearESlotWeight")]
    public int HeadwearESlotWeight { get; set; } = 6;

    [JsonPropertyName("headwearWeight")]
    public int HeadwearWeight { get; set; } = 1;

    [JsonPropertyName("tacticalVestGLengthWeight")]
    public int TacticalVestGLengthWeight { get; set; } = 10;

    [JsonPropertyName("tacticalVestWeight")]
    public int TacticalVestWeight { get; set; } = 1;

    [JsonPropertyName("enableModdedClothing")]
    public bool EnableModdedClothing { get; set; } = false;

    [JsonPropertyName("enableModdedAttachments")]
    public bool EnableModdedAttachments { get; set; } = false;

    [JsonPropertyName("initalTierAppearance")]
    public int InitalTierAppearance { get; set; } = 3;

    [JsonPropertyName("enableSafeGuard")]
    public bool EnableSafeGuard { get; set; } = true;

    [JsonPropertyName("enableMPRSafeGuard")]
    public bool EnableMprSafeGuard { get; set; } = true;

    [JsonPropertyName("PackNStrap_UnlootablePMCArmbandBelts")]
    public bool PackNStrapUnlootablePmcArmbandBelts { get; set; } = true;

    [JsonPropertyName("WttBackPort_AllowDogtags")]
    public bool WttBackPortAllowDogtags { get; set; } = true;

    [JsonPropertyName("WttArmoury_AddBossVariantsToBosses")]
    public bool WttArmouryAddBossVariantsToBosses { get; set; } = true;

    [JsonPropertyName("WttArmoury_AddBossVariantsToOthers")]
    public bool WttArmouryAddBossVariantsToOthers { get; set; } = true;

    [JsonPropertyName("Realism_AddGasMasksToBots")]
    public bool RealismAddGasMasksToBots { get; set; } = false;

    [JsonPropertyName("General_SecureContainerAmmoStacks")]
    public int GeneralSecureContainerAmmoStacks { get; set; } = 20;

    [JsonPropertyName("secrets")]
    public ModSecrets Secrets { get; set; } = new();
}

public class ModSecrets
{
    [JsonPropertyName("AprilFoolsEvent")]
    public bool AprilFoolsEvent { get; set; } = true;

    [JsonPropertyName("HalloweenEvent")]
    public bool HalloweenEvent { get; set; } = true;

    [JsonPropertyName("ChristmasEvent")]
    public bool ChristmasEvent { get; set; } = true;
}

public class PmcSecrets
{
    [JsonPropertyName("developerSettings")]
    public DeveloperSettings DeveloperSettings { get; set; } = new();
}

public class ScavSecrets
{
    [JsonPropertyName("jackpotScavRoubleStack")]
    public bool JackpotScavRoubleStack { get; set; } = true;
}

public class DeveloperSettings
{
    [JsonPropertyName("devNames")]
    public DeveloperNames DevNames { get; set; } = new();

    [JsonPropertyName("devLevels")]
    public DeveloperLevels DevLevels { get; set; } = new();
}

public class DeveloperNames
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("nameList")]
    public List<string> NameList { get; set; } =
    [
        "Chomp", "Dirtbikercj", "Clodan", "CWX", "DrakiaXYZ",
        "Kaeno", "Refringe", "Waffle", "AcidPhantasm", "Archangel"
    ];
}

public class DeveloperLevels
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; } = false;

    [JsonPropertyName("min")]
    public int Min { get; set; } = 30;

    [JsonPropertyName("max")]
    public int Max { get; set; } = 79;
}

public class ConfigAppSettings
{
    [JsonPropertyName("showUndo")]
    public bool ShowUndo { get; set; } = true;

    [JsonPropertyName("showDefault")]
    public bool ShowDefault { get; set; } = false;

    [JsonPropertyName("disableAnimations")]
    public bool DisableAnimations { get; set; } = false;

    [JsonPropertyName("allowUpdateChecks")]
    public bool AllowUpdateChecks { get; set; } = false;

    [JsonPropertyName("requireAuthCode")]
    public bool RequireAuthCode { get; set; } = false;

    [JsonPropertyName("authCode")]
    public string AuthCode { get; set; } = "Kitten Mittons";
}