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
    public IEnumerable<ApbsEquipmentBot> GetAllBots()
    {
        yield return PmcUsec;
        yield return PmcBear;
        yield return Scav;
        yield return BossBoar;
        yield return BossBoarSniper;
        yield return BossBully;
        yield return BossGluhar;
        yield return BossKilla;
        yield return BossKillaAgro;
        yield return BossKnight;
        yield return BossKojaniy;
        yield return BossKolontay;
        yield return BossSanitar;
        yield return BossTagilla;
        yield return BossTagillaAgro;
        yield return BossPartisan;
        yield return BossZryachiy;
        yield return FollowerBigPipe;
        yield return FollowerBirdeye;
        yield return SectantPriest;
        yield return SectantWarrior;
        yield return ExUsec;
        yield return PmcBot;
        yield return Default;
    }
    
    [JsonPropertyName("pmcUSEC")]
    public required ApbsEquipmentBot PmcUsec { get; set; }
    [JsonPropertyName("pmcBEAR")]
    public required ApbsEquipmentBot PmcBear { get; set; }
    [JsonPropertyName("scav")]
    public required ApbsEquipmentBot Scav { get; set; }
    [JsonPropertyName("bossboar")]
    public required ApbsEquipmentBot BossBoar { get; set; }
    [JsonPropertyName("bossboarsniper")]
    public required ApbsEquipmentBot BossBoarSniper { get; set; }
    [JsonPropertyName("bossbully")]
    public required ApbsEquipmentBot BossBully { get; set; }
    [JsonPropertyName("bossgluhar")]
    public required ApbsEquipmentBot BossGluhar { get; set; }
    [JsonPropertyName("bosskilla")]
    public required ApbsEquipmentBot BossKilla { get; set; }
    [JsonPropertyName("bosskillaagro")]
    public required ApbsEquipmentBot BossKillaAgro { get; set; }
    [JsonPropertyName("bossknight")]
    public required ApbsEquipmentBot BossKnight { get; set; }
    [JsonPropertyName("bosskojaniy")]
    public required ApbsEquipmentBot BossKojaniy { get; set; }
    [JsonPropertyName("bosskolontay")]
    public required ApbsEquipmentBot BossKolontay { get; set; }
    [JsonPropertyName("bosssanitar")]
    public required ApbsEquipmentBot BossSanitar { get; set; }
    [JsonPropertyName("bosstagilla")]
    public required ApbsEquipmentBot BossTagilla { get; set; }
    [JsonPropertyName("bosstagillaagro")]
    public required ApbsEquipmentBot BossTagillaAgro { get; set; }
    [JsonPropertyName("bosspartisan")]
    public required ApbsEquipmentBot BossPartisan { get; set; }
    [JsonPropertyName("bosszryachiy")]
    public required ApbsEquipmentBot BossZryachiy { get; set; }
    [JsonPropertyName("followerbigpipe")]
    public required ApbsEquipmentBot FollowerBigPipe { get; set; }
    [JsonPropertyName("followerbirdeye")]
    public required ApbsEquipmentBot FollowerBirdeye { get; set; }
    [JsonPropertyName("sectantpriest")]
    public required ApbsEquipmentBot SectantPriest { get; set; }
    [JsonPropertyName("sectantwarrior")]
    public required ApbsEquipmentBot SectantWarrior { get; set; }
    [JsonPropertyName("exusec")]
    public required ApbsEquipmentBot ExUsec { get; set; }
    [JsonPropertyName("pmcbot")]
    public required ApbsEquipmentBot PmcBot { get; set; }
    [JsonPropertyName("default")]
    public required ApbsEquipmentBot Default { get; set; }
}

public class ChancesTierData
{
    public IEnumerable<BotChancesData> GetAllBots()
    {
        yield return PmcUsec;
        yield return PmcBear;
        yield return Scav;
        yield return BossBoar;
        yield return BossBoarSniper;
        yield return BossBully;
        yield return BossGluhar;
        yield return BossKilla;
        yield return BossKillaAgro;
        yield return BossKnight;
        yield return BossKojaniy;
        yield return BossKolontay;
        yield return BossSanitar;
        yield return BossTagilla;
        yield return BossTagillaAgro;
        yield return BossPartisan;
        yield return BossZryachiy;
        yield return FollowerBigPipe;
        yield return FollowerBirdeye;
        yield return SectantPriest;
        yield return SectantWarrior;
        yield return ExUsec;
        yield return PmcBot;
        yield return Default;
    }
    [JsonPropertyName("pmcUSEC")]
    public required BotChancesData PmcUsec { get; set; }
    [JsonPropertyName("pmcBEAR")]
    public required BotChancesData PmcBear { get; set; }
    [JsonPropertyName("scav")]
    public required BotChancesData Scav { get; set; }
    [JsonPropertyName("bossboar")]
    public required BotChancesData BossBoar { get; set; }
    [JsonPropertyName("bossboarsniper")]
    public required BotChancesData BossBoarSniper { get; set; }
    [JsonPropertyName("bossbully")]
    public required BotChancesData BossBully { get; set; }
    [JsonPropertyName("bossgluhar")]
    public required BotChancesData BossGluhar { get; set; }
    [JsonPropertyName("bosskilla")]
    public required BotChancesData BossKilla { get; set; }
    [JsonPropertyName("bosskillaagro")]
    public required BotChancesData BossKillaAgro { get; set; }
    [JsonPropertyName("bossknight")]
    public required BotChancesData BossKnight { get; set; }
    [JsonPropertyName("bosskojaniy")]
    public required BotChancesData BossKojaniy { get; set; }
    [JsonPropertyName("bosskolontay")]
    public required BotChancesData BossKolontay { get; set; }
    [JsonPropertyName("bosssanitar")]
    public required BotChancesData BossSanitar { get; set; }
    [JsonPropertyName("bosstagilla")]
    public required BotChancesData BossTagilla { get; set; }
    [JsonPropertyName("bosstagillaagro")]
    public required BotChancesData BossTagillaAgro { get; set; }
    [JsonPropertyName("bosspartisan")]
    public required BotChancesData BossPartisan { get; set; }
    [JsonPropertyName("bosszryachiy")]
    public required BotChancesData BossZryachiy { get; set; }
    [JsonPropertyName("followerbigpipe")]
    public required BotChancesData FollowerBigPipe { get; set; }
    [JsonPropertyName("followerbirdeye")]
    public required BotChancesData FollowerBirdeye { get; set; }
    [JsonPropertyName("sectantpriest")]
    public required BotChancesData SectantPriest { get; set; }
    [JsonPropertyName("sectantwarrior")]
    public required BotChancesData SectantWarrior { get; set; }
    [JsonPropertyName("exusec")]
    public required BotChancesData ExUsec { get; set; }
    [JsonPropertyName("pmcbot")]
    public required BotChancesData PmcBot { get; set; }
    [JsonPropertyName("default")]
    public required BotChancesData Default { get; set; }

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