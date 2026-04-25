using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Models.Enums;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class BotEquipmentHelper(
    RandomUtil randomUtil, 
    DataLoader dataLoader,
    ICloner cloner,
    ApbsLogger apbsLogger) : IOnLoad {


    public Task OnLoad()
    {
        apbsLogger.Debug("BotConfigHelper.OnLoad()");
        return Task.CompletedTask;
    }
    
    private int CheckChadOrChill(int tierNumber)
    {
        if (ModConfig.Config.GeneralConfig.OnlyChads)
            return ModConfig.Config.GeneralConfig.TarkovAndChill
                ? randomUtil.GetInt(1, 7)
                : 7;

        if (ModConfig.Config.GeneralConfig.TarkovAndChill)
            return 1;

        return ModConfig.Config.GeneralConfig.BlickyMode ? 0 : tierNumber;
    }
    
    private Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> GetTierMods(int tierNumber, bool ignoreCheck = false)
    {
        if (!ignoreCheck) tierNumber = CheckChadOrChill(tierNumber);
        if (dataLoader.AllTierDataDirty.Tiers.TryGetValue(tierNumber, out var tierData))
            return new Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>(tierData.ModsData);
        apbsLogger.Error("Mods Data Unknown tier number: " + tierNumber);
        return new Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>(dataLoader.AllTierDataDirty.Tiers[1].ModsData) ?? throw new InvalidOperationException(); // fallback to tier 1
    }

    private ChancesTierData GetTierChances(int tierNumber, bool ignoreCheck = false)
    {
        if (!ignoreCheck) tierNumber = CheckChadOrChill(tierNumber);

        if (dataLoader.AllTierDataDirty.Tiers.TryGetValue(tierNumber, out var tierData))
            return tierData.ChancesData ?? throw new InvalidOperationException();
        
        apbsLogger.Error("Chances Unknown tier number: " + tierNumber);
        return dataLoader.AllTierDataDirty.Tiers[1].ChancesData ?? throw new InvalidOperationException(); // fallback to tier 1
    }

    private AmmoTierData GetTierAmmo(int tierNumber, bool ignoreCheck = false)
    {
        if (!ignoreCheck) tierNumber = CheckChadOrChill(tierNumber);

        if (dataLoader.AllTierDataDirty.Tiers.TryGetValue(tierNumber, out var tierData))
            return tierData.AmmoData ?? throw new InvalidOperationException();
        
        apbsLogger.Error("Ammo Data Unknown tier number: " + tierNumber);
        return dataLoader.AllTierDataDirty.Tiers[1].AmmoData ?? throw new InvalidOperationException(); // fallback to tier 1

    }

    private EquipmentTierData GetTierEquipment(int tierNumber, bool ignoreCheck = false)
    {
        if (!ignoreCheck) tierNumber = CheckChadOrChill(tierNumber);

        if (dataLoader.AllTierDataDirty.Tiers.TryGetValue(tierNumber, out var tierData))
            return tierData.EquipmentData ?? throw new InvalidOperationException();
        
        apbsLogger.Error("Equipment Data Unknown tier number: " + tierNumber);
        return dataLoader.AllTierDataDirty.Tiers[1].EquipmentData ?? throw new InvalidOperationException(); // fallback to tier 1

    }

    private AppearanceTierData GetTierAppearance(int tierNumber, bool ignoreCheck = false)
    {
        if (!ignoreCheck) tierNumber = CheckChadOrChill(tierNumber);

        if (dataLoader.AllTierDataDirty.Tiers.TryGetValue(tierNumber, out var tierData))
            return tierData.AppearanceData ?? throw new InvalidOperationException();
        
        apbsLogger.Error("Appearance Data Unknown tier number: " + tierNumber);
        return dataLoader.AllTierDataDirty.Tiers[1].AppearanceData ?? throw new InvalidOperationException(); // fallback to tier 1

    }

    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> GetModsByBotRole(string botRole, int tierNumber)
    {
        switch (botRole)
        {
            case "bossboar":
            case "bossboarsniper":
            case "bossbully":
            case "bossgluhar":
            case "bosskilla":
            case "bosskillaagro":
            case "bosskojaniy":
            case "bosskolontay":
            case "bosssanitar":
            case "bosstagilla":
            case "bosstagillaagro":
            case "bosspartisan":
            case "bossknight":
            case "followerbigpipe":
            case "followerbirdeye":
            case "sectantpriest":
            case "sectantwarrior":
            case "exusec":
            case "arenafighterevent":
            case "arenafighter":
            case "pmcbot":
                return tierNumber < 4 ? GetTierMods(4) : GetTierMods(tierNumber);
            case "marksman":
            case "cursedassault":
            case "assault":
                if (ModConfig.Config.GeneralConfig.BlickyMode || ModConfig.Config.GeneralConfig.OnlyChads || ModConfig.Config.ScavBots.AdditionalOptions.EnableScavAttachmentTiering) 
                    return GetTierMods(tierNumber);
                return GetTierMods(1);
            default:
                return GetTierMods(tierNumber);
        }
    }

    public Dictionary<ApbsEquipmentSlots, Dictionary<MongoId, double>> GetEquipmentByBotRole(string botRole, int tierNumber)
    {
        var tieredEquipmentData = GetTierEquipment(tierNumber);
        return botRole switch
        {
            "pmcusec" => tieredEquipmentData.PmcUsec.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "pmcbear" => tieredEquipmentData.PmcBear.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "marksman" or "cursedassault" or "assault" => tieredEquipmentData.Scav.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bossboar" => tieredEquipmentData.BossBoar.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bossboarsniper" => tieredEquipmentData.BossBoarSniper.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bossbully" => tieredEquipmentData.BossBully.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bossgluhar" => tieredEquipmentData.BossGluhar.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bosskilla" => tieredEquipmentData.BossKilla.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bosskillaagro" => tieredEquipmentData.BossKillaAgro.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bosskojaniy" => tieredEquipmentData.BossKojaniy.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bosskolontay" => tieredEquipmentData.BossKolontay.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bosssanitar" => tieredEquipmentData.BossSanitar.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bosstagilla" => tieredEquipmentData.BossTagilla.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bosstagillaagro" => tieredEquipmentData.BossTagillaAgro.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bosspartisan" => tieredEquipmentData.BossPartisan.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bosszryachiy" => tieredEquipmentData.BossZryachiy.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "bossknight" => tieredEquipmentData.BossKnight.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "followerbigpipe" => tieredEquipmentData.FollowerBigPipe.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "followerbirdeye" => tieredEquipmentData.FollowerBirdeye.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "sectantpriest" => tieredEquipmentData.SectantPriest.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "sectantwarrior" => tieredEquipmentData.SectantWarrior.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            "exusec" or "arenafighterevent" or "arenafighter" => tieredEquipmentData.ExUsec.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)), 
            "pmcbot" => tieredEquipmentData.PmcBot.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value)),
            _ => tieredEquipmentData.Default.Equipment.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value))
        };
    }

    public Dictionary<MongoId, double> GetEquipmentByBotRoleAndSlot(string botRole, int tierNumber, ApbsEquipmentSlots slot)
    {
        return GetEquipmentByBotRole(botRole, tierNumber)[slot];
    }
    
    public ApbsChances GetChancesByBotRole(string botRole, int tierNumber)
    {
        var tieredChancesData = GetTierChances(tierNumber);
        return botRole switch
        {
            "pmcusec" => tieredChancesData.PmcUsec.Chances,
            "pmcbear" => tieredChancesData.PmcBear.Chances,
            "marksman" or "cursedassault" or "assault" => tieredChancesData.Scav.Chances,
            "bossboar" => tieredChancesData.BossBoar.Chances,
            "bossboarsniper" => tieredChancesData.BossBoarSniper.Chances,
            "bossbully" => tieredChancesData.BossBully.Chances,
            "bossgluhar" => tieredChancesData.BossGluhar.Chances,
            "bosskilla" => tieredChancesData.BossKilla.Chances,
            "bosskillaagro" => tieredChancesData.BossKillaAgro.Chances,
            "bosskojaniy" => tieredChancesData.BossKojaniy.Chances,
            "bosskolontay" => tieredChancesData.BossKolontay.Chances,
            "bosssanitar" => tieredChancesData.BossSanitar.Chances,
            "bosstagilla" => tieredChancesData.BossTagilla.Chances,
            "bosstagillaagro" => tieredChancesData.BossTagillaAgro.Chances,
            "bosspartisan" => tieredChancesData.BossPartisan.Chances,
            "bosszryachiy" => tieredChancesData.BossZryachiy.Chances,
            "bossknight" => tieredChancesData.BossKnight.Chances,
            "followerbigpipe" => tieredChancesData.FollowerBigPipe.Chances,
            "followerbirdeye" => tieredChancesData.FollowerBirdeye.Chances,
            "sectantpriest" => tieredChancesData.SectantPriest.Chances,
            "sectantwarrior" => tieredChancesData.SectantWarrior.Chances,
            "exusec" or "arenafighterevent" or "arenafighter" => tieredChancesData.ExUsec.Chances,
            "pmcbot" => tieredChancesData.PmcBot.Chances,
            _ => tieredChancesData.Default.Chances
        };
    }

    public Dictionary<string, Dictionary<MongoId, double>> GetAmmoByBotRole(string botRole, int tierNumber)
    {
        if (botRole is "pmcusec" or "pmcbear" && ModConfig.Config.PmcBots.AdditionalOptions.AmmoTierSliding.Enable)
        {
            if (randomUtil.GetChance100(ModConfig.Config.PmcBots.AdditionalOptions.AmmoTierSliding.SlideChance))
            {
                var slideAmount = ModConfig.Config.PmcBots.AdditionalOptions.AmmoTierSliding.SlideAmount;
                var minTier = (tierNumber - slideAmount) <= 0 ? 1 : tierNumber - slideAmount;
                var maxTier = tierNumber - 1 <= 0 ? 1 : tierNumber - 1;
                tierNumber = NewTierCalc(tierNumber, minTier, maxTier);
            }
        }
        
        var tieredAmmoData = GetTierAmmo(tierNumber);

        return botRole switch
        {
            "marksman" or "cursedassault" or "assault" => tieredAmmoData.ScavAmmo,
            "pmcusec" or "pmcbear" => tieredAmmoData.PmcAmmo,
            _ => tieredAmmoData.BossAmmo
        };
    }

    public Appearance GetAppearanceByBotRole(string botRole, int tierNumber, Season season, bool seasonal = false)
    {
        var tieredAppearanceData = GetTierAppearance(tierNumber);
        if (!seasonal || tierNumber == 0)
            return botRole is "pmcBEAR"
                ? tieredAppearanceData.PmcBear["appearance"]
                : tieredAppearanceData.PmcUsec["appearance"];
        
        return botRole switch
        {
            "pmcUSEC" => season switch
            {
                Season.SPRING_EARLY => tieredAppearanceData.SpringEarly.PmcUsec["appearance"],
                Season.SPRING => tieredAppearanceData.Spring.PmcUsec["appearance"],
                Season.SUMMER or Season.STORM => tieredAppearanceData.Summer.PmcUsec["appearance"],
                Season.AUTUMN or Season.AUTUMN_LATE => tieredAppearanceData.Autumn.PmcUsec["appearance"],
                Season.WINTER => tieredAppearanceData.Winter.PmcUsec["appearance"],
                _ => tieredAppearanceData.Summer.PmcUsec["appearance"]
            },
            "pmcBEAR" => season switch
            {
                Season.SPRING_EARLY => tieredAppearanceData.SpringEarly.PmcBear["appearance"],
                Season.SPRING => tieredAppearanceData.Spring.PmcBear["appearance"],
                Season.SUMMER or Season.STORM => tieredAppearanceData.Summer.PmcBear["appearance"],
                Season.AUTUMN or Season.AUTUMN_LATE => tieredAppearanceData.Autumn.PmcBear["appearance"],
                Season.WINTER => tieredAppearanceData.Winter.PmcBear["appearance"],
                _ => tieredAppearanceData.Summer.PmcBear["appearance"]
            },
            _ => botRole is "pmcBEAR"
                ? tieredAppearanceData.PmcBear["appearance"]
                : tieredAppearanceData.PmcUsec["appearance"]
        };
    }
    
    private int NewTierCalc(int tierNumber, int minTier, int maxTier)
    {
        var random = new Random();
        if (minTier == maxTier) return minTier;

        var newTier = (Math.Floor(random.NextDouble() * (maxTier - minTier + 1) + minTier)) >= tierNumber
            ? (tierNumber - 1)
            : (Math.Floor(random.NextDouble() * (maxTier - minTier + 1) + minTier));
        return (int)newTier;
    }
}