using System.Collections;
using System.Text.Json.Serialization;

namespace _progressiveBotSystem.Models;

public class ApbsServerConfig
{
    [JsonPropertyName("usePreset")]
    public bool UsePreset { get; set; }
    [JsonPropertyName("presetName")]
    public required string PresetName { get; set; }
    [JsonPropertyName("compatibilityConfig")]
    public required ModCompatibilityConfig CompatibilityConfig { get; set; }
    [JsonPropertyName("normalizedHealthPool")]
    public required NormalizeHealthConfig NormalizedHealthPool { get; set; }
    [JsonPropertyName("generalConfig")]
    public required GeneralConfig GeneralConfig { get; set; }
    [JsonPropertyName("pmcBots")]
    public required PmcBotData PmcBots { get; set; }
    [JsonPropertyName("scavBots")]
    public required ScavBotData ScavBots { get; set; }
    [JsonPropertyName("bossBots")]
    public required GeneralBotData BossBots { get; set; }
    [JsonPropertyName("followerBots")]
    public required GeneralBotData FollowerBots { get; set; }
    [JsonPropertyName("specialBots")]
    public required GeneralBotData SpecialBots { get; set; }
    [JsonPropertyName("customLevelDeltas")]
    public required CustomLevelDelta CustomLevelDeltas { get; set; }
    [JsonPropertyName("customScavLevelDeltas")]
    public required CustomLevelDelta CustomScavLevelDeltas { get; set; }
    [JsonPropertyName("enableBotEquipmentLog")]
    public bool EnableBotEquipmentLog { get; set; }
    [JsonPropertyName("enableDebugLog")]
    public bool EnableDebugLog { get; set; }
    [JsonPropertyName("configAppSettings")]
    public required ConfigAppSettings ConfigAppSettings { get; set; }
}
public class GeneralBotData
{
    [JsonPropertyName("enable")]
    public bool Enable {  get; set; }
    [JsonPropertyName("resourceRandomization")]
    public required ResourceRandomizationConfig ResourceRandomization { get; set; }
    [JsonPropertyName("weaponDurability")]
    public required WeaponDurabilityConfig WeaponDurability { get; set; }
    [JsonPropertyName("armourDurability")]
    public required ArmourDurabilityConfig ArmourDurability { get; set; }
    [JsonPropertyName("lootConfig")]
    public required LootConfig LootConfig { get; set; }
    [JsonPropertyName("rerollConfig")]
    public required EnableChance RerollConfig { get; set; }
    [JsonPropertyName("toploadConfig")]
    public required ToploadConfig ToploadConfig { get; set; }
}
public class PmcBotData
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("resourceRandomization")]
    public required ResourceRandomizationConfig ResourceRandomization { get; set; }
    [JsonPropertyName("weaponDurability")]
    public required WeaponDurabilityConfig WeaponDurability { get; set; }
    [JsonPropertyName("armourDurability")]
    public required ArmourDurabilityConfig ArmourDurability { get; set; }
    [JsonPropertyName("lootConfig")]
    public required LootConfig LootConfig { get; set; }
    [JsonPropertyName("rerollConfig")]
    public required EnableChance RerollConfig { get; set; }
    [JsonPropertyName("toploadConfig")]
    public required ToploadConfig ToploadConfig { get; set; }
    [JsonPropertyName("questConfig")]
    public required EnableChance QuestConfig { get; set; }
    [JsonPropertyName("povertyConfig")]
    public required EnableChance PovertyConfig { get; set; }
    [JsonPropertyName("additionalOptions")]
    public required PmcSpecificConfig AdditionalOptions { get; set; }
    [JsonPropertyName("secrets")]
    public required PmcSecrets Secrets { get; set; }
}
public class ScavBotData
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("resourceRandomization")]
    public required ResourceRandomizationConfig ResourceRandomization { get; set; }
    [JsonPropertyName("weaponDurability")]
    public required WeaponDurabilityConfig WeaponDurability { get; set; }
    [JsonPropertyName("armourDurability")]
    public required ArmourDurabilityConfig ArmourDurability { get; set; }
    [JsonPropertyName("lootConfig")]
    public required LootConfig LootConfig { get; set; }
    [JsonPropertyName("rerollConfig")]
    public required EnableChance RerollConfig { get; set; }
    [JsonPropertyName("toploadConfig")]
    public required ToploadConfig ToploadConfig { get; set; }
    [JsonPropertyName("keyConfig")]
    public required KeyConfig KeyConfig { get; set; }
    [JsonPropertyName("additionalOptions")]
    public required ScavSpecificConfig AdditionalOptions { get; set; }
    [JsonPropertyName("secrets")]
    public required ScavSecrets Secrets { get; set; }
}

public class ApbsBlacklistConfig
{
    [JsonPropertyName("weaponBlacklist")]
    public required TierBlacklistConfig WeaponBlacklist { get; set; }
    [JsonPropertyName("equipmentBlacklist")]
    public required TierBlacklistConfig EquipmentBlacklist { get; set; }
    [JsonPropertyName("ammoBlacklist")]
    public required TierBlacklistConfig AmmoBlacklist { get; set; }
    [JsonPropertyName("attachmentBlacklist")]
    public required TierBlacklistConfig AttachmentBlacklist { get; set; }
    [JsonPropertyName("clothingBlacklist")]
    public required TierBlacklistConfig ClothingBlacklist { get; set; }
}
public class PmcSpecificConfig
{
    [JsonPropertyName("enablePrestiging")]
    public bool EnablePrestiging { get; set; }
    [JsonPropertyName("enablePrestigeAnyLevel")]
    public bool EnablePrestigeAnyLevel { get; set; }
    [JsonPropertyName("seasonalPmcAppearance")]
    public bool SeasonalPmcAppearance { get; set; }
    [JsonPropertyName("ammoTierSliding")]
    public required AmmoTierSlideConfig AmmoTierSliding { get; set; }
    [JsonPropertyName("gameVersionDogtagChance")]
    public int GameVersionDogtagChance { get; set; }
    [JsonPropertyName("gameVersionWeighting")]
    public required GameVersionWeightConfig GameVersionWeighting { get; set; }

}
public class EnableChance
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("chance")]
    public int Chance { get; set; }
}
public class ToploadConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("chance")]
    public int Chance { get; set; }
    [JsonPropertyName("percent")]
    public int Percent { get; set; }
}
public class ScavSpecificConfig
{
    [JsonPropertyName("enableScavAttachmentTiering")]
    public bool EnableScavAttachmentTiering { get; set; }
    [JsonPropertyName("enableScavEqualEquipmentTiering")]
    public bool EnableScavEqualEquipmentTiering { get; set; }
}
public class ResourceRandomizationConfig
{
    [JsonPropertyName("enable")]
    public required bool Enable { get; set; }
    [JsonPropertyName("foodRateMaxChance")]
    public required int FoodRateMaxChance { get; set; }
    [JsonPropertyName("foodRateUsagePercent")]
    public required int FoodRateUsagePercent { get; set; }
    [JsonPropertyName("medRateMaxChance")]
    public required int MedRateMaxChance { get; set; }
    [JsonPropertyName("medRateUsagePercent")]
    public required int MedRateUsagePercent { get; set; }
}
public class WeaponDurabilityConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("min")]
    public int Min { get; set; }
    [JsonPropertyName("max")]
    public int Max { get; set; }
    [JsonPropertyName("minDelta")]
    public int MinDelta { get; set; }
    [JsonPropertyName("maxDelta")]
    public int MaxDelta { get; set; }
    [JsonPropertyName("minLimitPercent")]
    public int MinLimitPercent { get; set; }
    [JsonPropertyName("enhancementChance")]
    public int EnhancementChance { get; set; }
}
public class ArmourDurabilityConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("min")]
    public int Min { get; set; }
    [JsonPropertyName("max")]
    public int Max { get; set; }
    [JsonPropertyName("minDelta")]
    public int MinDelta { get; set; }
    [JsonPropertyName("maxDelta")]
    public int MaxDelta { get; set; }
    [JsonPropertyName("minLimitPercent")]
    public int MinLimitPercent { get; set; }
}
public class LootConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("blacklist")]
    public required List<string> Blacklist { get; set; }
}
public class KeyConfig
{
    [JsonPropertyName("addAllKeysToScavs")]
    public bool AddAllKeysToScavs { get; set; }
    [JsonPropertyName("addOnlyMechanicalKeysToScavs")]
    public bool AddOnlyMechanicalKeysToScavs { get; set; }
    [JsonPropertyName("addOnlyKeyCardsToScavs")]
    public bool AddOnlyKeyCardsToScavs { get; set; }
    [JsonPropertyName("keyProbability")]
    public double KeyProbability { get; set; }
}
public class GeneralConfig
{
    [JsonPropertyName("enablePerWeaponTypeAttachmentChances")]
    public bool EnablePerWeaponTypeAttachmentChances { get; set; }
    [JsonPropertyName("enableLargeCapacityMagazineLimit")]
    public bool EnableLargeCapacityMagazineLimit { get; set; }
    [JsonPropertyName("largeCapacityMagazineCount")]
    public int LargeCapacityMagazineCount { get; set; }
    [JsonPropertyName("forceStock")]
    public bool ForceStock { get; set; }
    [JsonPropertyName("stockButtpadChance")]
    public int StockButtpadChance { get; set; }
    [JsonPropertyName("forceDustCover")]
    public bool ForceDustCover { get; set; }
    [JsonPropertyName("forceScopeSlot")]
    public bool ForceScopeSlot { get; set; }
    [JsonPropertyName("forceMuzzle")]
    public bool ForceMuzzle { get; set; }
    [JsonPropertyName("muzzleChance")]
    public required List<int> MuzzleChance { get; set; }
    [JsonPropertyName("forceChildrenMuzzle")]
    public bool ForceChildrenMuzzle { get; set; }
    [JsonPropertyName("forceWeaponModLimits")]
    public bool ForceWeaponModLimits { get; set; }
    [JsonPropertyName("scopeLimit")]
    public int ScopeLimit { get; set; }
    [JsonPropertyName("tacticalLimit")]
    public int TacticalLimit { get; set; }
    [JsonPropertyName("onlyChads")]
    public bool OnlyChads { get; set; }
    [JsonPropertyName("tarkovAndChill")]
    public bool TarkovAndChill { get; set; }
    [JsonPropertyName("blickyMode")]
    public bool BlickyMode { get; set; }
    [JsonPropertyName("enableT7Thermals")]
    public bool EnableT7Thermals { get; set; }
    [JsonPropertyName("startTier")]
    public int StartTier { get; set; }
    [JsonPropertyName("mapRangeWeighting")]
    public required MapRangeWeights MapRangeWeighting { get; set; }
    [JsonPropertyName("plateChances")]
    public required PlateWeightConfig PlateChances { get; set; }
    [JsonPropertyName("plateClasses")]
    public required PlateClasses PlateClasses { get; set; }
}
public class MapRangeWeights
{
    [JsonPropertyName("bigmap")]
    public required LongShortRange Bigmap { get; set; }

    [JsonPropertyName("RezervBase")]
    public required LongShortRange RezervBase { get; set; }

    [JsonPropertyName("laboratory")]
    public required LongShortRange Laboratory { get; set; }

    [JsonPropertyName("factory4_night")]
    public required LongShortRange Factory4Night { get; set; }

    [JsonPropertyName("factory4_day")]
    public required LongShortRange Factory4Day { get; set; }

    [JsonPropertyName("Interchange")]
    public required LongShortRange Interchange { get; set; }

    [JsonPropertyName("Sandbox")]
    public required LongShortRange Sandbox { get; set; }

    [JsonPropertyName("Sandbox_high")]
    public required LongShortRange SandboxHigh { get; set; }

    [JsonPropertyName("Woods")]
    public required LongShortRange Woods { get; set; }

    [JsonPropertyName("Shoreline")]
    public required LongShortRange Shoreline { get; set; }

    [JsonPropertyName("Lighthouse")]
    public required LongShortRange Lighthouse { get; set; }

    [JsonPropertyName("TarkovStreets")]
    public required LongShortRange TarkovStreets { get; set; }

    [JsonPropertyName("Labyrinth")]
    public required LongShortRange Labyrinth { get; set; }

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
    public double LongRange { get; set; }

    [JsonPropertyName("ShortRange")]
    public double ShortRange { get; set; }
    
    public Dictionary<string,double> ToDictionary()
        => new Dictionary<string,double>(2)
        {
            ["LongRange"]  = LongRange,
            ["ShortRange"] = ShortRange
        };
}
public class PlateWeightConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("pmcMainPlateChance")]
    public required List<int> PmcMainPlateChance { get; set; }
    [JsonPropertyName("pmcSidePlateChance")]
    public required List<int> PmcSidePlateChance { get; set; }
    [JsonPropertyName("scavMainPlateChance")]
    public required List<int> ScavMainPlateChance { get; set; }
    [JsonPropertyName("scavSidePlateChance")]
    public required List<int> ScavSidePlateChance { get; set; }
    [JsonPropertyName("bossMainPlateChance")]
    public required List<int> BossMainPlateChance { get; set; }
    [JsonPropertyName("bossSidePlateChance")]
    public required List<int> BossSidePlateChance { get; set; }
    [JsonPropertyName("followerMainPlateChance")]
    public required List<int> FollowerMainPlateChance { get; set; }
    [JsonPropertyName("followerSidePlateChance")]
    public required List<int> FollowerSidePlateChance { get; set; }
    [JsonPropertyName("specialMainPlateChance")]
    public required List<int> SpecialMainPlateChance { get; set; }
    [JsonPropertyName("specialSidePlateChance")]
    public required List<int> SpecialSidePlateChance { get; set; }
}
public class PlateClasses
{
    [JsonPropertyName("pmc")]
    public required PlateClassList Pmc { get; set; }
    [JsonPropertyName("scav")]
    public required PlateClassList Scav { get; set; }
    [JsonPropertyName("bossAndSpecial")]
    public required PlateClassList BossAndSpecial { get; set; }

}
public class PlateClassList
{
    [JsonPropertyName("tier1")]
    public required Dictionary<string,Dictionary<string,double>> Tier1 { get; set; }
    [JsonPropertyName("tier2")]
    public required Dictionary<string,Dictionary<string,double>> Tier2 { get; set; }
    [JsonPropertyName("tier3")]
    public required Dictionary<string,Dictionary<string,double>> Tier3 { get; set; }
    [JsonPropertyName("tier4")]
    public required Dictionary<string,Dictionary<string,double>> Tier4 { get; set; }
    [JsonPropertyName("tier5")]
    public required Dictionary<string,Dictionary<string,double>> Tier5 { get; set; }
    [JsonPropertyName("tier6")]
    public required Dictionary<string,Dictionary<string,double>> Tier6 { get; set; }
    [JsonPropertyName("tier7")]
    public required Dictionary<string,Dictionary<string,double>> Tier7 { get; set; }
}
public class PlateTierList
{
    [JsonPropertyName("front_plate")]
    public required PlateSlotList FrontPlate { get; set; }
    [JsonPropertyName("back_plate")]
    public required PlateSlotList BackPlate { get; set; }
    [JsonPropertyName("left_side_plate")]
    public required PlateSlotList LeftSidePlate { get; set; }
    [JsonPropertyName("right_side_plate")]
    public required PlateSlotList RightSidePlate { get; set; }

}
public class PlateSlotList
{
    [JsonPropertyName("2")]
    public int Class2 { get; set; }
    [JsonPropertyName("3")]
    public int Class3 { get; set; }
    [JsonPropertyName("4")]
    public int Class4 { get; set; }
    [JsonPropertyName("5")]
    public int Class5 { get; set; }
    [JsonPropertyName("6")]
    public int Class6 { get; set; }
}
    public class AmmoTierSlideConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("slideAmount")]
    public int SlideAmount { get; set; }
    [JsonPropertyName("slideChance")]
    public int SlideChance { get; set; }
}
public class GameVersionWeightConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("standard")]
    public int Standard { get; set; }
    [JsonPropertyName("leftBehind")]
    public int LeftBehind { get; set; }
    [JsonPropertyName("prepareForEscape")]
    public int PrepareForEscape { get; set; }
    [JsonPropertyName("edgeOfDarkness")]
    public int EdgeOfDarkness { get; set; }
    [JsonPropertyName("unheardEdition")]
    public int UnheardEdition { get; set; }
}
public class NormalizeHealthConfig
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("setHead")]
    public bool SetHead { get; set; }
    [JsonPropertyName("healthHead")]
    public int HealthHead { get; set; }
    [JsonPropertyName("setChest")]
    public bool SetChest { get; set; }
    [JsonPropertyName("healthChest")]
    public int HealthChest { get; set; }
    [JsonPropertyName("setStomach")]
    public bool SetStomach { get; set; }
    [JsonPropertyName("healthStomach")]
    public int HealthStomach { get; set; }
    [JsonPropertyName("setLeftArm")]
    public bool SetLeftArm { get; set; }
    [JsonPropertyName("healthLeftArm")]
    public int HealthLeftArm { get; set; }
    [JsonPropertyName("setRightArm")]
    public bool SetRightArm { get; set; }
    [JsonPropertyName("healthRightArm")]
    public int HealthRightArm { get; set; }
    [JsonPropertyName("setLeftLeg")]
    public bool SetLeftLeg { get; set; }
    [JsonPropertyName("healthLeftLeg")]
    public int HealthLeftLeg { get; set; }
    [JsonPropertyName("setRightLeg")]
    public bool SetRightLeg { get; set; }
    [JsonPropertyName("healthRightLeg")]
    public int HealthRightLeg { get; set; }
    [JsonPropertyName("excludedBots")]
    public required List<string> ExcludedBots { get; set; }
    [JsonPropertyName("normalizeSkills")]
    public bool NormalizeSkills { get; set; }
}
public class TierBlacklistConfig
{
    [JsonPropertyName("tier1Blacklist")]
    public required List<string> Tier1Blacklist { get; set; }
    [JsonPropertyName("tier2Blacklist")]
    public required List<string> Tier2Blacklist { get; set; }
    [JsonPropertyName("tier3Blacklist")]
    public required List<string> Tier3Blacklist { get; set; }
    [JsonPropertyName("tier4Blacklist")]
    public required List<string> Tier4Blacklist { get; set; }
    [JsonPropertyName("tier5Blacklist")]
    public required List<string> Tier5Blacklist { get; set; }
    [JsonPropertyName("tier6Blacklist")]
    public required List<string> Tier6Blacklist { get; set; }
    [JsonPropertyName("tier7Blacklist")]
    public required List<string> Tier7Blacklist { get; set; }
}
public class CustomLevelDelta
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("tier1")]
    public required MinMax Tier1 { get; set; }
    [JsonPropertyName("tier2")]
    public required MinMax Tier2 { get; set; }
    [JsonPropertyName("tier3")]
    public required MinMax Tier3 { get; set; }
    [JsonPropertyName("tier4")]
    public required MinMax Tier4 { get; set; }
    [JsonPropertyName("tier5")]
    public required MinMax Tier5 { get; set; }
    [JsonPropertyName("tier6")]
    public required MinMax Tier6 { get; set; }
    [JsonPropertyName("tier7")]
    public required MinMax Tier7 { get; set; }
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
    public bool EnableModdedWeapons { get; set; }
    [JsonPropertyName("enableModdedEquipment")]
    public bool EnableModdedEquipment { get; set; }
    [JsonPropertyName("enableModdedClothing")]
    public bool EnableModdedClothing { get; set; }
    [JsonPropertyName("enableModdedAttachments")]
    public bool EnableModdedAttachments { get; set; }
    [JsonPropertyName("initalTierAppearance")]
    public int InitalTierAppearance { get; set; }
    [JsonPropertyName("pmcWeaponWeights")]
    public int PmcWeaponWeights { get; set; }
    [JsonPropertyName("scavWeaponWeights")]
    public int ScavWeaponWeights { get; set; }
    [JsonPropertyName("followerWeaponWeights")]
    public int FollowerWeaponWeights { get; set; }
    [JsonPropertyName("enableSafeGuard")]
    public bool EnableSafeGuard { get; set; }
    [JsonPropertyName("enableMPRSafeGuard")]
    public bool EnableMprSafeGuard { get; set; }
    [JsonPropertyName("PackNStrap_UnlootablePMCArmbandBelts")]
    public bool PackNStrapUnlootablePmcArmbandBelts { get; set; }
    [JsonPropertyName("WttBackPort_AllowDogtags")]
    public bool WttBackPortAllowDogtags { get; set; }
    [JsonPropertyName("WttArmoury_AddBossVariantsToBosses")]
    public bool WttArmouryAddBossVariantsToBosses { get; set; }
    [JsonPropertyName("WttArmoury_AddBossVariantsToOthers")]
    public bool WttArmouryAddBossVariantsToOthers { get; set; }
    [JsonPropertyName("Realism_AddGasMasksToBots")]
    public bool RealismAddGasMasksToBots { get; set; }
    [JsonPropertyName("General_SecureContainerAmmoStacks")]
    public int GeneralSecureContainerAmmoStacks { get; set; }
}

public class PmcSecrets
{
    [JsonPropertyName("developerSettings")]
    public required DeveloperSettings DeveloperSettings { get; set; }
}
public class ScavSecrets
{
    [JsonPropertyName("jackpotScavRoubleStack")]
    public required bool JackpotScavRoubleStack { get; set; }
}
public class DeveloperSettings
{
    [JsonPropertyName("devNames")]
    public required DeveloperNames DevNames { get; set; }
    [JsonPropertyName("devLevels")]
    public required DeveloperLevels DevLevels { get; set; }

}
public class DeveloperNames
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("nameList")]
    public required List<string> NameList { get; set; }
}

public class DeveloperLevels
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
    [JsonPropertyName("min")]
    public int Min { get; set; }
    [JsonPropertyName("max")]
    public int Max { get; set; }
}
public class ConfigAppSettings
{
    [JsonPropertyName("showUndo")]
    public bool ShowUndo { get; set; }
    [JsonPropertyName("showDefault")]
    public bool ShowDefault { get; set; }
    [JsonPropertyName("disableAnimations")]
    public bool DisableAnimations { get; set; }
    [JsonPropertyName("allowUpdateChecks")]
    public bool AllowUpdateChecks { get; set; }
    [JsonPropertyName("requireAuthCode")]
    public bool RequireAuthCode { get; set; }
    [JsonPropertyName("authCode")]
    public string AuthCode { get; set; }
}