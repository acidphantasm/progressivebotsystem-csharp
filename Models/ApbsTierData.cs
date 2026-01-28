using System.Collections;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace _progressiveBotSystem.Models;

public record AllTierData
{
    public required Dictionary<int, TierInnerData> Tiers { get; set; }
}

public record TierInnerData
{
    public EquipmentTierData EquipmentData { get; set; }
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> ModsData { get; set; }
    public AmmoTierData AmmoData { get; set; }
    public ChancesTierData ChancesData{ get; set; }
    public AppearanceTierData AppearanceData { get; set; }
}
public class EquipmentTierData
{
    [JsonPropertyName("pmcUSEC")]
    public ApbsEquipmentBot PmcUsec { get; set; }
    [JsonPropertyName("pmcBEAR")]
    public ApbsEquipmentBot PmcBear { get; set; }
    [JsonPropertyName("scav")]
    public ApbsEquipmentBot Scav { get; set; }
    [JsonPropertyName("bossboar")]
    public ApbsEquipmentBot BossBoar { get; set; }
    [JsonPropertyName("bossboarsniper")]
    public ApbsEquipmentBot BossBoarSniper { get; set; }
    [JsonPropertyName("bossbully")]
    public ApbsEquipmentBot BossBully { get; set; }
    [JsonPropertyName("bossgluhar")]
    public ApbsEquipmentBot BossGluhar { get; set; }
    [JsonPropertyName("bosskilla")]
    public ApbsEquipmentBot BossKilla { get; set; }
    [JsonPropertyName("bossknight")]
    public ApbsEquipmentBot BossKnight { get; set; }
    [JsonPropertyName("bosskojaniy")]
    public ApbsEquipmentBot BossKojaniy { get; set; }
    [JsonPropertyName("bosskolontay")]
    public ApbsEquipmentBot BossKolontay { get; set; }
    [JsonPropertyName("bosssanitar")]
    public ApbsEquipmentBot BossSanitar { get; set; }
    [JsonPropertyName("bosstagilla")]
    public ApbsEquipmentBot BossTagilla { get; set; }
    [JsonPropertyName("bosspartisan")]
    public ApbsEquipmentBot BossPartisan { get; set; }
    [JsonPropertyName("bosszryachiy")]
    public ApbsEquipmentBot BossZryachiy { get; set; }
    [JsonPropertyName("followerbigpipe")]
    public ApbsEquipmentBot FollowerBigPipe { get; set; }
    [JsonPropertyName("followerbirdeye")]
    public ApbsEquipmentBot FollowerBirdeye { get; set; }
    [JsonPropertyName("sectantpriest")]
    public ApbsEquipmentBot SectantPriest { get; set; }
    [JsonPropertyName("sectantwarrior")]
    public ApbsEquipmentBot SectantWarrior { get; set; }
    [JsonPropertyName("exusec")]
    public ApbsEquipmentBot ExUsec { get; set; }
    [JsonPropertyName("pmcbot")]
    public ApbsEquipmentBot PmcBot { get; set; }
    [JsonPropertyName("default")]
    public ApbsEquipmentBot Default { get; set; }
}

public class ChancesTierData
{
    [JsonPropertyName("pmcUSEC")]
    public BotChancesData PmcUsec { get; set; }
    [JsonPropertyName("pmcBEAR")]
    public BotChancesData PmcBear { get; set; }
    [JsonPropertyName("scav")]
    public BotChancesData Scav { get; set; }
    [JsonPropertyName("bossboar")]
    public BotChancesData BossBoar { get; set; }
    [JsonPropertyName("bossboarsniper")]
    public BotChancesData BossBoarSniper { get; set; }
    [JsonPropertyName("bossbully")]
    public BotChancesData BossBully { get; set; }
    [JsonPropertyName("bossgluhar")]
    public BotChancesData BossGluhar { get; set; }
    [JsonPropertyName("bosskilla")]
    public BotChancesData BossKilla { get; set; }
    [JsonPropertyName("bossknight")]
    public BotChancesData BossKnight { get; set; }
    [JsonPropertyName("bosskojaniy")]
    public BotChancesData BossKojaniy { get; set; }
    [JsonPropertyName("bosskolontay")]
    public BotChancesData BossKolontay { get; set; }
    [JsonPropertyName("bosssanitar")]
    public BotChancesData BossSanitar { get; set; }
    [JsonPropertyName("bosstagilla")]
    public BotChancesData BossTagilla { get; set; }
    [JsonPropertyName("bosspartisan")]
    public BotChancesData BossPartisan { get; set; }
    [JsonPropertyName("bosszryachiy")]
    public BotChancesData BossZryachiy { get; set; }
    [JsonPropertyName("followerbigpipe")]
    public BotChancesData FollowerBigPipe { get; set; }
    [JsonPropertyName("followerbirdeye")]
    public BotChancesData FollowerBirdeye { get; set; }
    [JsonPropertyName("sectantpriest")]
    public BotChancesData SectantPriest { get; set; }
    [JsonPropertyName("sectantwarrior")]
    public BotChancesData SectantWarrior { get; set; }
    [JsonPropertyName("exusec")]
    public BotChancesData ExUsec { get; set; }
    [JsonPropertyName("pmcbot")]
    public BotChancesData PmcBot { get; set; }
    [JsonPropertyName("default")]
    public BotChancesData Default { get; set; }

}

public class AmmoTierData
{
    [JsonPropertyName("scavAmmo")]
    public Dictionary<string, Dictionary<MongoId, double>> ScavAmmo { get; set; }
    
    [JsonPropertyName("pmcAmmo")]
    public Dictionary<string, Dictionary<MongoId, double>> PmcAmmo { get; set; }
    
    [JsonPropertyName("bossAmmo")]
    public Dictionary<string, Dictionary<MongoId, double>> BossAmmo { get; set; }
}

public class AppearanceTierData
{
    [JsonPropertyName("pmcUSEC")]
    public Dictionary<string, Appearance> PmcUsec { get; set; }
    [JsonPropertyName("pmcBEAR")]
    public Dictionary<string, Appearance> PmcBear { get; set; }
    [JsonPropertyName("springEarly")]
    public SeasonAppearance SpringEarly { get; set; }
    [JsonPropertyName("spring")]
    public SeasonAppearance Spring { get; set; }
    [JsonPropertyName("summer")]
    public SeasonAppearance Summer { get; set; }
    [JsonPropertyName("autumn")]
    public SeasonAppearance Autumn { get; set; }
    [JsonPropertyName("winter")]
    public SeasonAppearance Winter { get; set; }
}

public class SeasonAppearance
{
    [JsonPropertyName("pmcUSEC")]
    public Dictionary<string, Appearance> PmcUsec { get; set; }

    [JsonPropertyName("pmcBEAR")]
    public Dictionary<string, Appearance> PmcBear { get; set; }
}