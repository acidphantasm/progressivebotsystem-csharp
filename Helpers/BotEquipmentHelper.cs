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
            return cloner.Clone(tierData.ModsData) ?? throw new InvalidOperationException();
        
        apbsLogger.Error("Mods Data Unknown tier number: " + tierNumber);
        return cloner.Clone(dataLoader.AllTierDataDirty.Tiers[1].ModsData) ?? throw new InvalidOperationException(); // fallback to tier 1

    }

    private ChancesTierData GetTierChances(int tierNumber, bool ignoreCheck = false)
    {
        if (!ignoreCheck) tierNumber = CheckChadOrChill(tierNumber);

        if (dataLoader.AllTierDataDirty.Tiers.TryGetValue(tierNumber, out var tierData))
            return cloner.Clone(tierData.ChancesData) ?? throw new InvalidOperationException();
        
        apbsLogger.Error("Chances Unknown tier number: " + tierNumber);
        return cloner.Clone(dataLoader.AllTierDataDirty.Tiers[1].ChancesData) ?? throw new InvalidOperationException(); // fallback to tier 1

    }

    private AmmoTierData GetTierAmmo(int tierNumber, bool ignoreCheck = false)
    {
        if (!ignoreCheck) tierNumber = CheckChadOrChill(tierNumber);

        if (dataLoader.AllTierDataDirty.Tiers.TryGetValue(tierNumber, out var tierData))
            return cloner.Clone(tierData.AmmoData) ?? throw new InvalidOperationException();
        
        apbsLogger.Error("Ammo Data Unknown tier number: " + tierNumber);
        return cloner.Clone(dataLoader.AllTierDataDirty.Tiers[1].AmmoData) ?? throw new InvalidOperationException(); // fallback to tier 1

    }

    private EquipmentTierData GetTierEquipment(int tierNumber, bool ignoreCheck = false)
    {
        if (!ignoreCheck) tierNumber = CheckChadOrChill(tierNumber);

        if (dataLoader.AllTierDataDirty.Tiers.TryGetValue(tierNumber, out var tierData))
            return cloner.Clone(tierData.EquipmentData) ?? throw new InvalidOperationException();
        
        apbsLogger.Error("Equipment Data Unknown tier number: " + tierNumber);
        return cloner.Clone(dataLoader.AllTierDataDirty.Tiers[1].EquipmentData) ?? throw new InvalidOperationException(); // fallback to tier 1

    }

    private AppearanceTierData GetTierAppearance(int tierNumber, bool ignoreCheck = false)
    {
        if (!ignoreCheck) tierNumber = CheckChadOrChill(tierNumber);

        if (dataLoader.AllTierDataDirty.Tiers.TryGetValue(tierNumber, out var tierData))
            return cloner.Clone(tierData.AppearanceData) ?? throw new InvalidOperationException();
        
        apbsLogger.Error("Appearance Data Unknown tier number: " + tierNumber);
        return cloner.Clone(dataLoader.AllTierDataDirty.Tiers[1].AppearanceData) ?? throw new InvalidOperationException(); // fallback to tier 1

    }

    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> GetModsByBotRole(string botRole, int tierNumber)
    {
        var tieredModsData = GetTierMods(tierNumber);
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
                return tierNumber < 4 ? GetTierMods(4) : tieredModsData;
            case "marksman":
            case "cursedassault":
            case "assault":
                if (ModConfig.Config.GeneralConfig.BlickyMode || ModConfig.Config.GeneralConfig.OnlyChads || ModConfig.Config.ScavBots.AdditionalOptions.EnableScavAttachmentTiering) return tieredModsData;
                return GetTierMods(1);
            default:
                return tieredModsData;
        }
    }

    public Dictionary<ApbsEquipmentSlots, Dictionary<MongoId, double>> GetEquipmentByBotRole(string botRole, int tierNumber)
    {
        var tieredEquipmentData = GetTierEquipment(tierNumber);
        switch (botRole)
        {
            case "pmcusec":
                return tieredEquipmentData.PmcUsec.Equipment;
            case "pmcbear":
                return tieredEquipmentData.PmcBear.Equipment;
            case "marksman":
            case "cursedassault":
            case "assault":
                return tieredEquipmentData.Scav.Equipment;
            case "bossboar":
                return tieredEquipmentData.BossBoar.Equipment;
            case "bossboarsniper":
                return tieredEquipmentData.BossBoarSniper.Equipment;
            case "bossbully":
                return tieredEquipmentData.BossBully.Equipment;
            case "bossgluhar":
                return tieredEquipmentData.BossGluhar.Equipment;
            case "bosskilla":
                return tieredEquipmentData.BossKilla.Equipment;
            case "bosskillaagro":
                return tieredEquipmentData.BossKillaAgro.Equipment;
            case "bosskojaniy":
                return tieredEquipmentData.BossKojaniy.Equipment;
            case "bosskolontay":
                return tieredEquipmentData.BossKolontay.Equipment;
            case "bosssanitar":
                return tieredEquipmentData.BossSanitar.Equipment;
            case "bosstagilla":
                return tieredEquipmentData.BossTagilla.Equipment;
            case "bosstagillaagro":
                return tieredEquipmentData.BossTagillaAgro.Equipment;
            case "bosspartisan":
                return tieredEquipmentData.BossPartisan.Equipment;
            case "bosszryachiy":
                return tieredEquipmentData.BossZryachiy.Equipment;
            case "bossknight":
                return tieredEquipmentData.BossKnight.Equipment;
            case "followerbigpipe":
                return tieredEquipmentData.FollowerBigPipe.Equipment;
            case "followerbirdeye":
                return tieredEquipmentData.FollowerBirdeye.Equipment;
            case "sectantpriest":
                return tieredEquipmentData.SectantPriest.Equipment;
            case "sectantwarrior":
                return tieredEquipmentData.SectantWarrior.Equipment;
            case "exusec":
            case "arenafighterevent":
            case "arenafighter":
                return tieredEquipmentData.ExUsec.Equipment;
            case "pmcbot":
                return tieredEquipmentData.PmcBot.Equipment;
            default:
                return tieredEquipmentData.Default.Equipment;
        }
    }

    public Dictionary<MongoId, double> GetEquipmentByBotRoleAndSlot(string botRole, int tierNumber, ApbsEquipmentSlots slot)
    {
        var tieredEquipmentData = GetTierEquipment(tierNumber);
        switch (botRole)
        {
            case "pmcusec":
                return tieredEquipmentData.PmcUsec.Equipment[slot];
            case "pmcbear":
                return tieredEquipmentData.PmcBear.Equipment[slot];
            case "marksman":
            case "cursedassault":
            case "assault":
                return tieredEquipmentData.Scav.Equipment[slot];
            case "bossboar":
                return tieredEquipmentData.BossBoar.Equipment[slot];
            case "bossboarsniper":
                return tieredEquipmentData.BossBoarSniper.Equipment[slot];
            case "bossbully":
                return tieredEquipmentData.BossBully.Equipment[slot];
            case "bossgluhar":
                return tieredEquipmentData.BossGluhar.Equipment[slot];
            case "bosskilla":
                return tieredEquipmentData.BossKilla.Equipment[slot];
            case "bosskillaagro":
                return tieredEquipmentData.BossKillaAgro.Equipment[slot];
            case "bosskojaniy":
                return tieredEquipmentData.BossKojaniy.Equipment[slot];
            case "bosskolontay":
                return tieredEquipmentData.BossKolontay.Equipment[slot];
            case "bosssanitar":
                return tieredEquipmentData.BossSanitar.Equipment[slot];
            case "bosstagilla":
                return tieredEquipmentData.BossTagilla.Equipment[slot];
            case "bosstagillaagro":
                return tieredEquipmentData.BossTagillaAgro.Equipment[slot];
            case "bosspartisan":
                return tieredEquipmentData.BossPartisan.Equipment[slot];
            case "bosszryachiy":
                return tieredEquipmentData.BossZryachiy.Equipment[slot];
            case "bossknight":
                return tieredEquipmentData.BossKnight.Equipment[slot];
            case "followerbigpipe":
                return tieredEquipmentData.FollowerBigPipe.Equipment[slot];
            case "followerbirdeye":
                return tieredEquipmentData.FollowerBirdeye.Equipment[slot];
            case "sectantpriest":
                return tieredEquipmentData.SectantPriest.Equipment[slot];
            case "sectantwarrior":
                return tieredEquipmentData.SectantWarrior.Equipment[slot];
            case "exusec":
            case "arenafighterevent":
            case "arenafighter":
                return tieredEquipmentData.ExUsec.Equipment[slot];
            case "pmcbot":
                return tieredEquipmentData.PmcBot.Equipment[slot];
            default:
                return tieredEquipmentData.Default.Equipment[slot];
        }
    }
    
    public ApbsChances GetChancesByBotRole(string botRole, int tierNumber)
    {
        var tieredChancesData = GetTierChances(tierNumber);
        switch (botRole)
        {
            case "pmcusec":
                return tieredChancesData.PmcUsec.Chances;
            case "pmcbear":
                return tieredChancesData.PmcBear.Chances;
            case "marksman":
            case "cursedassault":
            case "assault":
                return tieredChancesData.Scav.Chances;
            case "bossboar":
                return tieredChancesData.BossBoar.Chances;
            case "bossboarsniper":
                return tieredChancesData.BossBoarSniper.Chances;
            case "bossbully":
                return tieredChancesData.BossBully.Chances;
            case "bossgluhar":
                return tieredChancesData.BossGluhar.Chances;
            case "bosskilla":
                return tieredChancesData.BossKilla.Chances;
            case "bosskillaagro":
                return tieredChancesData.BossKillaAgro.Chances;
            case "bosskojaniy":
                return tieredChancesData.BossKojaniy.Chances;
            case "bosskolontay":
                return tieredChancesData.BossKolontay.Chances;
            case "bosssanitar":
                return tieredChancesData.BossSanitar.Chances;
            case "bosstagilla":
                return tieredChancesData.BossTagilla.Chances;
            case "bosstagillaagro":
                return tieredChancesData.BossTagillaAgro.Chances;
            case "bosspartisan":
                return tieredChancesData.BossPartisan.Chances;
            case "bosszryachiy":
                return tieredChancesData.BossZryachiy.Chances;
            case "bossknight":
                return tieredChancesData.BossKnight.Chances;
            case "followerbigpipe":
                return tieredChancesData.FollowerBigPipe.Chances;
            case "followerbirdeye":
                return tieredChancesData.FollowerBirdeye.Chances;
            case "sectantpriest":
                return tieredChancesData.SectantPriest.Chances;
            case "sectantwarrior":
                return tieredChancesData.SectantWarrior.Chances;
            case "exusec":
            case "arenafighterevent":
            case "arenafighter":
                return tieredChancesData.ExUsec.Chances;
            case "pmcbot":
                return tieredChancesData.PmcBot.Chances;
            default:
                return tieredChancesData.Default.Chances;
        }
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