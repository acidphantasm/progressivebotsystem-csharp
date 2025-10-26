using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 90010)]
public class BotConfigHelper : IOnLoad
{
    public BotConfigHelper(
        DatabaseService databaseService,
        ConfigServer configServer,
        ApbsLogger apbsLogger,
        BotActivityHelper botActivityHelper,
        ItemHelper itemHelper,
        BotEquipmentHelper botEquipmentHelper,
        TierInformation tierInformation)
    {
        _databaseService = databaseService;
        _botConfig = configServer.GetConfig<BotConfig>();
        _pmcConfig = configServer.GetConfig<PmcConfig>();
        _apbsLogger = apbsLogger;
        _botActivityHelper = botActivityHelper;
        _itemHelper = itemHelper;
        _botEquipmentHelper = botEquipmentHelper;
        _tierInformation = tierInformation;
    }
    
    private ApbsLogger _apbsLogger;
    private DatabaseService _databaseService;
    private BotConfig _botConfig;
    private PmcConfig _pmcConfig;
    private BotActivityHelper _botActivityHelper;
    private ItemHelper _itemHelper;
    private BotEquipmentHelper _botEquipmentHelper;
    private TierInformation _tierInformation;
    
    public Task OnLoad()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("BotConfigHelper.OnLoad()");
        PmcConfigs();
        ScavConfigs();
        BossConfigs();
        FollowerConfigs();
        SpecialConfigs();
        AllBotConfigs();
        AllBotsConfigsBypassEnableCheck();
        SpecialHandlingConfigs();
        
        return Task.CompletedTask;
    }

    public Task ReapplyConfig()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("BotConfigHelper.ReapplyConfig()");
        PmcConfigs();
        ScavConfigs();
        BossConfigs();
        FollowerConfigs();
        SpecialConfigs();
        AllBotConfigs();
        AllBotsConfigsBypassEnableCheck();
        SpecialHandlingConfigs();
        
        return Task.CompletedTask;
    }
    
    private void PmcConfigs()
    {
        if (!ModConfig.Config.PmcBots.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("--Configuring PMC Bots");
        PmcItemLimits();
        PmcLoot();
        PmcScopeWhitelist();
        PmcRequiredSlots();
        PmcGameVersionWeights();
        PmcPlateClasses();
    }
    #region PmcConfigs
    private void PmcItemLimits()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Pmc Item Limits");
        _botConfig.ItemSpawnLimits["pmc"] = new Dictionary<MongoId, double>();
    }
    private void PmcLoot()
    {
        _pmcConfig.LooseWeaponInBackpackLootMinMax.Min = 0;
        _pmcConfig.LooseWeaponInBackpackLootMinMax.Max = 0;

        if (ModConfig.Config.PmcBots.LootConfig.Enable)
        {
            if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Pmc Loot");
            foreach (var item in ModConfig.Config.PmcBots.LootConfig.Blacklist ?? [])
            {
                _pmcConfig.BackpackLoot.Blacklist.Add(item);
                _pmcConfig.VestLoot.Blacklist.Add(item);
                _pmcConfig.PocketLoot.Blacklist.Add(item);
            }
        }
        else
        {
            if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Disabling Pmc Loot");
            _botConfig.DisableLootOnBotTypes.Add("pmcusec");
            _botConfig.DisableLootOnBotTypes.Add("pmcbear");
        }

        foreach (var bot in (List<string>)["pmcbear", "pmcusec"])
        {
            _databaseService.GetBots().Types[bot]!.BotInventory.Items.Backpack.Clear();
            _databaseService.GetBots().Types[bot]!.BotInventory.Items.Pockets.Clear();
            _databaseService.GetBots().Types[bot]!.BotInventory.Items.TacticalVest.Clear();
            _databaseService.GetBots().Types[bot]!.BotInventory.Items.SpecialLoot.Clear();
        }
    }
    private void PmcScopeWhitelist()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Pmc Scope Whitelist");
        if (!_botConfig.Equipment.TryGetValue("pmc", out var pmcEquipment)) return;
        
        pmcEquipment.WeaponSightWhitelist = new Dictionary<MongoId, HashSet<MongoId>>();
        // Assault Carbine
        pmcEquipment.WeaponSightWhitelist.TryAdd("5447b5fc4bdc2d87278b4567", ["55818ad54bdc2ddc698b4569", "55818acf4bdc2dde698b456b", "55818ae44bdc2dde698b456c", "55818ac54bdc2d5b648b456e", "55818add4bdc2d5b648b456f", "55818aeb4bdc2ddc698b456a"]);
        // Assault Rifle
        pmcEquipment.WeaponSightWhitelist.TryAdd("5447b5f14bdc2d61278b4567", ["55818ad54bdc2ddc698b4569", "55818acf4bdc2dde698b456b", "55818ae44bdc2dde698b456c", "55818ac54bdc2d5b648b456e", "55818add4bdc2d5b648b456f", "55818aeb4bdc2ddc698b456a"]);
        // Grenade Launcher
        pmcEquipment.WeaponSightWhitelist.TryAdd("5447bedf4bdc2d87278b4568", ["55818ad54bdc2ddc698b4569", "55818add4bdc2d5b648b456f", "55818ac54bdc2d5b648b456e", "55818aeb4bdc2ddc698b456a"]);
        // MachineGun
        pmcEquipment.WeaponSightWhitelist.TryAdd("5447bed64bdc2d97278b4568", ["55818ad54bdc2ddc698b4569", "55818acf4bdc2dde698b456b", "55818ac54bdc2d5b648b456e", "55818add4bdc2d5b648b456f", "55818aeb4bdc2ddc698b456a"]);
        // Marksman Rifle
        pmcEquipment.WeaponSightWhitelist.TryAdd("5447b6194bdc2d67278b4567", ["55818ad54bdc2ddc698b4569", "55818ae44bdc2dde698b456c", "55818ac54bdc2d5b648b456e", "55818aeb4bdc2ddc698b456a", "55818add4bdc2d5b648b456f"]);
        // Handgun
        pmcEquipment.WeaponSightWhitelist.TryAdd("5447b5cf4bdc2d65278b4567", ["55818ad54bdc2ddc698b4569", "55818acf4bdc2dde698b456b", "55818ac54bdc2d5b648b456e"]);
        // Revolver
        pmcEquipment.WeaponSightWhitelist.TryAdd("617f1ef5e8b54b0998387733", ["55818ad54bdc2ddc698b4569", "55818acf4bdc2dde698b456b", "55818ac54bdc2d5b648b456e"]);
        // Shotgun
        pmcEquipment.WeaponSightWhitelist.TryAdd("5447b6094bdc2dc3278b4567", ["55818ad54bdc2ddc698b4569", "55818acf4bdc2dde698b456b", "55818ac54bdc2d5b648b456e"]);
        // SMG
        pmcEquipment.WeaponSightWhitelist.TryAdd("5447b5e04bdc2d62278b4567", ["55818ad54bdc2ddc698b4569", "55818acf4bdc2dde698b456b", "55818ac54bdc2d5b648b456e"]);
        //Sniper Rifle
        pmcEquipment.WeaponSightWhitelist.TryAdd("5447b6254bdc2dc3278b4568", ["55818ae44bdc2dde698b456c", "55818ac54bdc2d5b648b456e", "55818aeb4bdc2ddc698b456a", "55818add4bdc2d5b648b456f"]);
    }
    private void PmcRequiredSlots()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Pmc Required Slots");
        if (!_botConfig.Equipment.TryGetValue("pmc", out var pmcEquipment)) return;
        pmcEquipment.WeaponSlotIdsToMakeRequired = new HashSet<string>();
        pmcEquipment.WeaponSlotIdsToMakeRequired.Add("mod_stock");
        pmcEquipment.WeaponSlotIdsToMakeRequired.Add("mod_reciever");
    }
    private void PmcGameVersionWeights()
    {
        if (!ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Pmc Game Version Weights");
        _pmcConfig.GameVersionWeight["standard"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.Standard;
        _pmcConfig.GameVersionWeight["left_behind"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.LeftBehind;
        _pmcConfig.GameVersionWeight["prepare_for_escape"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.PrepareForEscape;
        _pmcConfig.GameVersionWeight["edge_of_darkness"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.EdgeOfDarkness;
        _pmcConfig.GameVersionWeight["unheard_edition"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.UnheardEdition;
    }
    private void PmcPlateClasses()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Pmc Plate Classes");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            if (!typeof(PmcBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType)) continue;
            botConfigEquipment[botType].FilterPlatesByLevel = true;
            botConfigEquipment[botType].ArmorPlateWeighting = new List<ArmorPlateWeights>();
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 1,
                    Max = 10
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Pmc.Tier1
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 11,
                    Max = 20
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Pmc.Tier2
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 21,
                    Max = 30
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Pmc.Tier3
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 31,
                    Max = 40
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Pmc.Tier4
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 41,
                    Max = 50
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Pmc.Tier5
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 51,
                    Max = 60
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Pmc.Tier6
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 61,
                    Max = 100
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Pmc.Tier7
            });
        }
    }
    #endregion
    private void ScavConfigs()
    {
        if (!ModConfig.Config.ScavBots.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("--Configuring Scav Bots");
        ScavPushKeyConfig();
        ScavLoot();
        ScavIdenticalWeightConfig();
        ScavLevelDeltas();
        ScavPlateClasses();
    }
    #region ScavConfigs
    private void ScavPushKeyConfig()
    {
        if (!ModConfig.Config.ScavBots.KeyConfig.AddAllKeysToScavs &&
            !ModConfig.Config.ScavBots.KeyConfig.AddOnlyKeyCardsToScavs &&
            !ModConfig.Config.ScavBots.KeyConfig.AddOnlyMechanicalKeysToScavs) return;

        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Scav Key Loot");
        _databaseService.GetBots().Types.TryGetValue("assault", out var assaultBot);
        _databaseService.GetBots().Types.TryGetValue("marksman", out var marksmanBot);

        var itemValueCollection = _databaseService.GetItems().Values;
        var filteredKeyItems = itemValueCollection.Where(item => _itemHelper.IsOfBaseclass(item.Id, GetKeyConfig()));

        var assaultBotCount = 0;
        var marksmanBotCount = 0;
        foreach (var item in filteredKeyItems ?? [])
        {
            if (VanillaItemConstants.LabyrinthKeys.Contains(item.Id)) continue;
            
            if (assaultBot.BotInventory.Items.Backpack.TryGetValue(item.Id, out var assaultItemWeight)) assaultItemWeight = 1;
            else
            {
                assaultBot.BotInventory.Items.Backpack.Add(item.Id, 1);
                assaultBotCount++;
            }
            
            if (marksmanBot.BotInventory.Items.Backpack.TryGetValue(item.Id, out var marksmanItemWeight)) marksmanItemWeight = 1;
            else
            {
                marksmanBot.BotInventory.Items.Backpack.Add(item.Id, 1);
                marksmanBotCount++;
            }
        }
        _apbsLogger.Debug($"Added {assaultBotCount} keys to Scavs and {marksmanBotCount} keys to Marksman. Key Class Config: {GetKeyConfig()}");
    }
    private MongoId GetKeyConfig()
    {
        return ModConfig.Config.ScavBots.KeyConfig.AddAllKeysToScavs ? BaseClasses.KEY : ModConfig.Config.ScavBots.KeyConfig.AddOnlyMechanicalKeysToScavs ? BaseClasses.KEY_MECHANICAL : BaseClasses.KEYCARD;
    }
    private void ScavLoot()
    {
        if (ModConfig.Config.ScavBots.LootConfig.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Disabling Scav Loot");
        var scavBotTypes = typeof(ScavBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
        foreach (var bot in scavBotTypes)
        {
            _botConfig.DisableLootOnBotTypes.Add(bot);
        }
    }
    private void ScavIdenticalWeightConfig()
    {
        if (!ModConfig.Config.ScavBots.AdditionalOptions.EnableScavEqualEquipmentTiering) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Scav Equipment Weights to 1");
        for (var i = 0; i <= 7; i++)
        {
            var equipmentData = _botEquipmentHelper.GetTierEquipment(i, true);
            foreach (var (slotName, data) in equipmentData.Scav.Equipment)
            {
                if (data.Count == 0) continue;
                foreach (var (mongoId, weight) in data)
                {
                    data[mongoId] = 1;
                }
            }
        }
    }
    private void ScavLevelDeltas()
    {
        if (!ModConfig.Config.CustomScavLevelDeltas.Enable) return;
        
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Scav Level Deltas");
        _tierInformation.Tiers[0].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier1.Min;
        _tierInformation.Tiers[0].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier1.Max;
        _tierInformation.Tiers[1].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier2.Min;
        _tierInformation.Tiers[1].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier2.Max;
        _tierInformation.Tiers[2].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier3.Min;
        _tierInformation.Tiers[2].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier3.Max;
        _tierInformation.Tiers[3].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier4.Min;
        _tierInformation.Tiers[3].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier4.Max;
        _tierInformation.Tiers[4].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier5.Min;
        _tierInformation.Tiers[4].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier5.Max;
        _tierInformation.Tiers[5].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier6.Min;
        _tierInformation.Tiers[5].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier6.Max;
        _tierInformation.Tiers[6].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier7.Min;
        _tierInformation.Tiers[6].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier7.Max;
    }
    private void ScavPlateClasses()
    {
        var botConfigEquipment = _botConfig.Equipment;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Scav Plate Classes");
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            if (!typeof(ScavBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType)) continue;
            botConfigEquipment[botType].FilterPlatesByLevel = true;
            botConfigEquipment[botType].ArmorPlateWeighting = new List<ArmorPlateWeights>();
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 1,
                    Max = 10
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Scav.Tier1
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 11,
                    Max = 20
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Scav.Tier2
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 21,
                    Max = 30
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Scav.Tier3
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 31,
                    Max = 40
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Scav.Tier4
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 41,
                    Max = 50
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Scav.Tier5
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 51,
                    Max = 60
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Scav.Tier6
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 61,
                    Max = 100
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.Scav.Tier7
            });
        }
    }
    #endregion
    private void BossConfigs()
    {
        if (!ModConfig.Config.BossBots.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("--Configuring Boss Bots");
        BossLoot();
        BossPlateClasses();
    }
    #region BossConfigs
    private void BossLoot()
    {
        if (!ModConfig.Config.BossBots.LootConfig.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Disabling Boss Loot");
        var bossBotTypes = typeof(BossBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
        foreach (var botType in bossBotTypes)
        {
            _botConfig.DisableLootOnBotTypes.Add(botType);
        }
    }
    private void BossPlateClasses()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Boss Plate Classes");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            if (!typeof(BossBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType)) continue;
            botConfigEquipment[botType].FilterPlatesByLevel = true;
            botConfigEquipment[botType].ArmorPlateWeighting = new List<ArmorPlateWeights>();
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 1,
                    Max = 10
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier1
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 11,
                    Max = 20
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier2
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 21,
                    Max = 30
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier3
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 31,
                    Max = 40
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier4
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 41,
                    Max = 50
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier5
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 51,
                    Max = 60
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier6
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 61,
                    Max = 100
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier7
            });
        }
    }
    #endregion
    private void FollowerConfigs()
    {
        if (!ModConfig.Config.FollowerBots.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("--Configuring Follower Bots");
        FollowerLoot();
        FollowerPlateClasses();
    }
    #region FollowerConfigs
    private void FollowerLoot()
    {
        if (!ModConfig.Config.FollowerBots.LootConfig.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Disabling Follower Loot");
        var followerBotTypes = typeof(FollowerBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
        foreach (var botType in followerBotTypes)
        {
            _botConfig.DisableLootOnBotTypes.Add(botType);
        }
    }
    private void FollowerPlateClasses()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Follower Plate Classes");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            if (!typeof(FollowerBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType)) continue;
            botConfigEquipment[botType].FilterPlatesByLevel = true;
            botConfigEquipment[botType].ArmorPlateWeighting = new List<ArmorPlateWeights>();
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 1,
                    Max = 10
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier1
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 11,
                    Max = 20
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier2
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 21,
                    Max = 30
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier3
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 31,
                    Max = 40
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier4
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 41,
                    Max = 50
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier5
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 51,
                    Max = 60
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier6
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 61,
                    Max = 100
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier7
            });
        }
    }
    #endregion
    private void SpecialConfigs()
    {
        if (!ModConfig.Config.SpecialBots.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("--Configuring Special Bots");
        SpecialLoot();
        SpecialPlateClasses();
    }
    #region SpecialConfigs
    private void SpecialLoot()
    {
        if (!ModConfig.Config.SpecialBots.LootConfig.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Disabling Special Bot Loot");
        var specialBotTypes = typeof(SpecialBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
        foreach (var botType in specialBotTypes)
        {
            _botConfig.DisableLootOnBotTypes.Add(botType);
        }
    }
    private void SpecialPlateClasses()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Special Bot Plate Classes");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            if (!typeof(SpecialBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType)) continue;
            botConfigEquipment[botType].FilterPlatesByLevel = true;
            botConfigEquipment[botType].ArmorPlateWeighting = new List<ArmorPlateWeights>();
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 1,
                    Max = 10
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier1
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 11,
                    Max = 20
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier2
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 21,
                    Max = 30
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier3
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 31,
                    Max = 40
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier4
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 41,
                    Max = 50
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier5
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 51,
                    Max = 60
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier6
            });
            botConfigEquipment[botType].ArmorPlateWeighting.Add(new ArmorPlateWeights()
            {
                LevelRange = new MinMax<int>()
                {
                    Min = 61,
                    Max = 100
                },
                Values = ModConfig.Config.GeneralConfig.PlateClasses.BossAndSpecial.Tier7
            });
        }
    }
    #endregion
    private void AllBotConfigs()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("--Configuring Remainder of Bot Configs");
        SetLevelDeltas();
        RemoveRandomization();
        SetBotLevels();
        SetWeaponDurability();
        AdjustNVGs();
        SetItemResourceRandomization();
        SetWeaponModLimits();
        AmmoStackCompatibility();
    }
    #region AllBotConfigs
    private void SetLevelDeltas()
    {
        if (!ModConfig.Config.CustomLevelDeltas.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Custom Level Deltas");
        _tierInformation.Tiers[0].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier1.Min;
        _tierInformation.Tiers[0].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier1.Max;
        _tierInformation.Tiers[1].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier2.Min;
        _tierInformation.Tiers[1].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier2.Max;
        _tierInformation.Tiers[2].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier3.Min;
        _tierInformation.Tiers[2].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier3.Max;
        _tierInformation.Tiers[3].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier4.Min;
        _tierInformation.Tiers[3].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier4.Max;
        _tierInformation.Tiers[4].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier5.Min;
        _tierInformation.Tiers[4].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier5.Max;
        _tierInformation.Tiers[5].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier6.Min;
        _tierInformation.Tiers[5].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier6.Max;
        _tierInformation.Tiers[6].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier7.Min;
        _tierInformation.Tiers[6].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier7.Max;
    }
    private void RemoveRandomization()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Removing Bot Randomisation");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, _) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            botConfigEquipment[botType]!.Randomisation = new List<RandomisationDetails>();
            botConfigEquipment[botType]!.WeightingAdjustmentsByBotLevel = new List<WeightingAdjustmentDetails>();
        }
    }
    private void SetBotLevels()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Bot Levels");
        var allBotTypes = _databaseService.GetBots().Types;
        foreach (var (bot, botData) in allBotTypes)
        {
            allBotTypes[bot]!.BotExperience.Level.Min = 1;
            allBotTypes[bot]!.BotExperience.Level.Max = 79;
        }
    }
    private void SetWeaponDurability()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Bot Weapon Durability");
        var botDurability = _botConfig.Durability;
        foreach (var (botType, data) in botDurability.BotDurabilities)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            if (typeof(ScavBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType) && ModConfig.Config.ScavBots.WeaponDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Weapon.HighestMax = ModConfig.Config.ScavBots.WeaponDurability.Max;
                botDurability.BotDurabilities[botType].Weapon.LowestMax = ModConfig.Config.ScavBots.WeaponDurability.Min;
                botDurability.BotDurabilities[botType].Weapon.MaxDelta = ModConfig.Config.ScavBots.WeaponDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Weapon.MinDelta = ModConfig.Config.ScavBots.WeaponDurability.MinDelta;
                botDurability.BotDurabilities[botType].Weapon.MinLimitPercent = ModConfig.Config.ScavBots.WeaponDurability.MinLimitPercent;
            }
            else if (typeof(BossBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType) && ModConfig.Config.BossBots.WeaponDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Weapon.HighestMax = ModConfig.Config.BossBots.WeaponDurability.Max;
                botDurability.BotDurabilities[botType].Weapon.LowestMax = ModConfig.Config.BossBots.WeaponDurability.Min;
                botDurability.BotDurabilities[botType].Weapon.MaxDelta = ModConfig.Config.BossBots.WeaponDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Weapon.MinDelta = ModConfig.Config.BossBots.WeaponDurability.MinDelta;
                botDurability.BotDurabilities[botType].Weapon.MinLimitPercent = ModConfig.Config.BossBots.WeaponDurability.MinLimitPercent;
            }
            else if (typeof(FollowerBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType) && ModConfig.Config.FollowerBots.WeaponDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Weapon.HighestMax = ModConfig.Config.FollowerBots.WeaponDurability.Max;
                botDurability.BotDurabilities[botType].Weapon.LowestMax = ModConfig.Config.FollowerBots.WeaponDurability.Min;
                botDurability.BotDurabilities[botType].Weapon.MaxDelta = ModConfig.Config.FollowerBots.WeaponDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Weapon.MinDelta = ModConfig.Config.FollowerBots.WeaponDurability.MinDelta;
                botDurability.BotDurabilities[botType].Weapon.MinLimitPercent = ModConfig.Config.FollowerBots.WeaponDurability.MinLimitPercent;
            }
            else if (typeof(SpecialBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType) && ModConfig.Config.SpecialBots.WeaponDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Weapon.HighestMax = ModConfig.Config.SpecialBots.WeaponDurability.Max;
                botDurability.BotDurabilities[botType].Weapon.LowestMax = ModConfig.Config.SpecialBots.WeaponDurability.Min;
                botDurability.BotDurabilities[botType].Weapon.MaxDelta = ModConfig.Config.SpecialBots.WeaponDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Weapon.MinDelta = ModConfig.Config.SpecialBots.WeaponDurability.MinDelta;
                botDurability.BotDurabilities[botType].Weapon.MinLimitPercent = ModConfig.Config.SpecialBots.WeaponDurability.MinLimitPercent;
            }
            else _apbsLogger.Error($"[DURABILITY] Unknown bot type: {botType}");
        }

        if (!ModConfig.Config.PmcBots.WeaponDurability.Enable) return;
        
        botDurability.Pmc.Weapon.HighestMax = ModConfig.Config.PmcBots.WeaponDurability.Max;
        botDurability.Pmc.Weapon.LowestMax = ModConfig.Config.PmcBots.WeaponDurability.Min;
        botDurability.Pmc.Weapon.MaxDelta = ModConfig.Config.PmcBots.WeaponDurability.MaxDelta;
        botDurability.Pmc.Weapon.MinDelta = ModConfig.Config.PmcBots.WeaponDurability.MinDelta;
        botDurability.Pmc.Weapon.MinLimitPercent = ModConfig.Config.PmcBots.WeaponDurability.MinLimitPercent;

    }
    private void AdjustNVGs()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Bot Laser, Flashlight, and Nvg Activity Chances");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, _) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            
            botConfigEquipment[botType]!.FaceShieldIsActiveChancePercent = 90;
            botConfigEquipment[botType]!.LightIsActiveDayChancePercent = 7;
            botConfigEquipment[botType]!.LightIsActiveNightChancePercent = 25;
            botConfigEquipment[botType]!.LaserIsActiveChancePercent = 50;
            botConfigEquipment[botType]!.NvgIsActiveChanceDayPercent = 0;
            botConfigEquipment[botType]!.NvgIsActiveChanceNightPercent = 95;
        }
    }
    private void SetItemResourceRandomization()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Bot Food and Medical Item Randomisation");
        // Loop the bot types instead of the botconfig, even though the actual values exist in the bot config - I need proper bot names
        var botTable = _databaseService.GetBots().Types;
        foreach (var (botType, data) in botTable)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            
            var setValues = false;
            var foodMaxChance = 100;
            var medMaxChange = 100;
            var foodResourcePercent = 60;
            var medResourcePercent = 60;

            if (typeof(PmcBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType) && ModConfig.Config.PmcBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.PmcBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.PmcBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.PmcBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.PmcBots.ResourceRandomization.MedRateUsagePercent;
            }
            else if (typeof(ScavBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType) && ModConfig.Config.ScavBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.ScavBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.ScavBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.ScavBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.ScavBots.ResourceRandomization.MedRateUsagePercent;
            }
            else if (typeof(BossBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType) && ModConfig.Config.BossBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.BossBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.BossBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.BossBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.BossBots.ResourceRandomization.MedRateUsagePercent;
            }
            else if (typeof(FollowerBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType) && ModConfig.Config.FollowerBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.FollowerBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.FollowerBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.FollowerBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.FollowerBots.ResourceRandomization.MedRateUsagePercent;
            }
            else if (typeof(SpecialBots).GetFields().Select(x => x.GetValue(null)).Cast<string>().Contains(botType) && ModConfig.Config.SpecialBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.SpecialBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.SpecialBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.SpecialBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.SpecialBots.ResourceRandomization.MedRateUsagePercent;
            }
            else _apbsLogger.Error($"[RESOURCERANDOMIZATION] Unknown bot type: {botType}");

            if (!setValues) continue;

            if (!_botConfig.LootItemResourceRandomization.TryGetValue(botType, out var randomizdResourceDetails))
            {
                _botConfig.LootItemResourceRandomization[botType] = new RandomisedResourceDetails()
                {
                    Food = new RandomisedResourceValues()
                    {
                        ChanceMaxResourcePercent = foodMaxChance,
                        ResourcePercent = foodResourcePercent,
                    },
                    Meds = new RandomisedResourceValues()
                    {
                        ChanceMaxResourcePercent = medMaxChange,
                        ResourcePercent = medResourcePercent,
                    }
                };
            }
            else
            {
                randomizdResourceDetails.Food.ChanceMaxResourcePercent = foodMaxChance;
                randomizdResourceDetails.Food.ResourcePercent = foodResourcePercent;
                randomizdResourceDetails.Meds.ChanceMaxResourcePercent = medMaxChange;
                randomizdResourceDetails.Meds.ResourcePercent = medResourcePercent;
            }
        }
    }
    private void SetWeaponModLimits()
    {
        if (!ModConfig.Config.GeneralConfig.ForceWeaponModLimits) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Bot Weapon Mod Limits");

        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, data) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;

            botConfigEquipment[botType]!.WeaponModLimits ??= new ModLimits()
            {
                ScopeLimit = 2,
                LightLaserLimit = 1
            };
            botConfigEquipment[botType]!.WeaponModLimits!.ScopeLimit = ModConfig.Config.GeneralConfig.ScopeLimit;
            botConfigEquipment[botType]!.WeaponModLimits!.LightLaserLimit = ModConfig.Config.GeneralConfig.TacticalLimit;
        }
    }
    private void AmmoStackCompatibility()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Bot Secure Container Ammo Stack Compatibility");
        _botConfig.SecureContainerAmmoStackCount =
            ModConfig.Config.CompatibilityConfig.GeneralSecureContainerAmmoStacks;
    }
    #endregion
    private void AllBotsConfigsBypassEnableCheck()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("--Configuring Remainder of Bot Configs that bypass enablement checks");
        NormalizeHealth();
        NormalizeSkills();
    }
    #region AllBotsConfigsBypassingEnableCheck
    private void NormalizeHealth()
    {
        if (!ModConfig.Config.NormalizedHealthPool.Enable) return;

        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Normalising Bot Health");
        var botTable = _databaseService.GetBots().Types;
        foreach (var (botType, data) in botTable)
        {
            if (ModConfig.Config.NormalizedHealthPool.ExcludedBots.Contains(botType)) continue;
            if (data is null)
            {
                _apbsLogger.Error($"[HEALTH NORMALIZATION] Bot type is unknown: {botType}");
                continue;
            }
            var bodyParts = data.BotHealth.BodyParts;
            foreach (var bodyPart in bodyParts)
            {
                bodyPart.Head.Min = ModConfig.Config.NormalizedHealthPool.HealthHead > 0 ? ModConfig.Config.NormalizedHealthPool.HealthHead : 35;
                bodyPart.Head.Max = ModConfig.Config.NormalizedHealthPool.HealthHead > 0 ? ModConfig.Config.NormalizedHealthPool.HealthHead : 35;
                bodyPart.Chest.Min = ModConfig.Config.NormalizedHealthPool.HealthChest > 0 ? ModConfig.Config.NormalizedHealthPool.HealthHead : 85;
                bodyPart.Chest.Max = ModConfig.Config.NormalizedHealthPool.HealthChest > 0 ? ModConfig.Config.NormalizedHealthPool.HealthHead : 85;
                bodyPart.Stomach.Min = ModConfig.Config.NormalizedHealthPool.HealthStomach > 0 ? ModConfig.Config.NormalizedHealthPool.HealthStomach : 70;
                bodyPart.Stomach.Max = ModConfig.Config.NormalizedHealthPool.HealthStomach > 0 ? ModConfig.Config.NormalizedHealthPool.HealthStomach : 70;
                bodyPart.LeftArm.Min = ModConfig.Config.NormalizedHealthPool.HealthLeftArm > 0 ? ModConfig.Config.NormalizedHealthPool.HealthLeftArm : 60;
                bodyPart.LeftArm.Max = ModConfig.Config.NormalizedHealthPool.HealthLeftArm > 0 ? ModConfig.Config.NormalizedHealthPool.HealthLeftArm : 60;
                bodyPart.RightArm.Min = ModConfig.Config.NormalizedHealthPool.HealthRightArm > 0 ? ModConfig.Config.NormalizedHealthPool.HealthRightArm : 60;
                bodyPart.RightArm.Max = ModConfig.Config.NormalizedHealthPool.HealthRightArm > 0 ? ModConfig.Config.NormalizedHealthPool.HealthRightArm : 60;
                bodyPart.LeftLeg.Min = ModConfig.Config.NormalizedHealthPool.HealthLeftLeg > 0 ? ModConfig.Config.NormalizedHealthPool.HealthLeftLeg : 65;
                bodyPart.LeftLeg.Max = ModConfig.Config.NormalizedHealthPool.HealthLeftLeg > 0 ? ModConfig.Config.NormalizedHealthPool.HealthLeftLeg : 65;
                bodyPart.RightLeg.Min = ModConfig.Config.NormalizedHealthPool.HealthRightLeg > 0 ? ModConfig.Config.NormalizedHealthPool.HealthRightLeg : 65;
                bodyPart.RightLeg.Max = ModConfig.Config.NormalizedHealthPool.HealthRightLeg > 0 ? ModConfig.Config.NormalizedHealthPool.HealthRightLeg : 65;
            }
        }
    }
    private void NormalizeSkills()
    {
        if (!ModConfig.Config.NormalizedHealthPool.NormalizeSkills) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Bot Skills");
        var botTable = _databaseService.GetBots().Types;
        foreach (var (botType, data) in botTable)
        {
            if (data is null)
            {
                _apbsLogger.Error($"[SKILL NORMALIZATION] Bot type is unknown: {botType}");
                continue;
            }
            if (botType is "usec" or "bear" or "pmcusec" or "pmcbear") continue;
            foreach (var (skill, minMaxData) in data.BotSkills.Common)
            {
                if (skill == "Strength" || !(minMaxData.Max > 100)) continue;
                if (skill is "BotReload" or "BotSound")
                {
                    _apbsLogger.Debug($"[SKILL NORMALIZATION] Removed Skill: {skill} from {botType}");
                    data.BotSkills.Common.Remove(skill);
                }
                
                minMaxData.Min = 0;
                minMaxData.Max = 1;
            }
        }
    }
    #endregion
    private void SpecialHandlingConfigs()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("--Configuring Settings that require special handling");
        RemoveTatm();
        SetPlateChances();
        ForceStock();
        ForceDustCover();
        ForceScopes();
        MuzzleChances();
    }
    
    #region AllBotsConfigsBypassingEnableCheck
    private void RemoveTatm()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Removing T7 Thermals from Database");
        for (var i = 1; i <= 7; i++)
        {
            var modsData = _botEquipmentHelper.GetTierMods(i, true);
            if (modsData.TryGetValue(ItemTpl.MOUNT_NOROTOS_TITANIUM_ADVANCED_TACTICAL, out var tatmMods))
            {
                if (ModConfig.Config.GeneralConfig.EnableT7Thermals &&
                    i >= ModConfig.Config.GeneralConfig.StartTier) continue;
                
                _apbsLogger.Debug($"[THERMAL] Removed T7's from tier {i}");
                tatmMods.Remove(ItemTpl.MOUNT_PVS7_WILCOX_ADAPTER);
            }
        }
    }
    private void SetPlateChances()
    {
        if (!ModConfig.Config.GeneralConfig.PlateChances.Enable) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Setting Bot Plate Chances");
        for (var i = 1; i <= 7; i++)
        {
            var chancesData = _botEquipmentHelper.GetTierChances(i, true);
            if (chancesData == null)
                continue;
            
            var indexPosition = i - 1;
            
            foreach (var botProp in typeof(ChancesTierData).GetProperties())
            {
                var botType = botProp.Name;
                var data = botProp.GetValue(chancesData) as BotChancesData;
                if (data?.Chances == null)
                    continue;
                
                foreach (var chanceProp in typeof(ApbsChances).GetProperties())
                {
                    var keyName = chanceProp.Name;
                    if (!keyName.Contains("EquipmentMods", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var chancesDict = chanceProp.GetValue(data.Chances) as Dictionary<string, double>;
                    if (chancesDict == null)
                        continue;

                    if (botType == "pmcUSEC" || botType == "pmcBEAR")
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.PmcMainPlateChance[indexPosition], "back_plate", "front_plate");
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.PmcSidePlateChance[indexPosition], "left_side_plate", "right_side_plate");
                    }
                    if (botType == "followerbirdeye" || botType == "followerbigpipe" || botType.Contains("boss"))
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.BossMainPlateChance[indexPosition], "back_plate", "front_plate");
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.BossSidePlateChance[indexPosition], "left_side_plate", "right_side_plate");
                    }
                    if (botType == "scav")
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.ScavMainPlateChance[indexPosition], "back_plate", "front_plate");
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.ScavSidePlateChance[indexPosition], "left_side_plate", "right_side_plate");
                    }
                    if (botType == "exusec" || botType == "pmcbot" || botType.Contains("sectant"))
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.SpecialMainPlateChance[indexPosition], "back_plate", "front_plate");
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.SpecialSidePlateChance[indexPosition], "left_side_plate", "right_side_plate");
                    }
                    if (botType == "default")
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.FollowerMainPlateChance[indexPosition], "back_plate", "front_plate");
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.FollowerSidePlateChance[indexPosition], "left_side_plate", "right_side_plate");
                    }
                }
            }
        }
    }
    private void ForceStock()
    {
        if (!ModConfig.Config.GeneralConfig.ForceStock) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Forcing Stocks on Bot Weapons");
        for (var i = 0; i <= 7; i++)
        {
            var chancesData = _botEquipmentHelper.GetTierChances(i, true);
            if (chancesData == null)
                continue;
            
            foreach (var botProp in typeof(ChancesTierData).GetProperties())
            {
                var botType = botProp.Name;
                var data = botProp.GetValue(chancesData) as BotChancesData;
                if (data?.Chances == null)
                    continue;
                
                foreach (var chanceProp in typeof(ApbsChances).GetProperties())
                {
                    var keyName = chanceProp.Name;
                    if (keyName.Contains("Equipment", StringComparison.OrdinalIgnoreCase) || keyName == "Generation")
                        continue;

                    var chancesDict = chanceProp.GetValue(data.Chances) as Dictionary<string, double>;
                    if (chancesDict == null)
                        continue;
                    
                    SetModChance(chancesDict, 100, "mod_stock", "mod_stock_000", "mod_stock_001", "mod_stock_akms", "mod_stock_axis");
                }
            }
        }
    }
    private void ForceDustCover()
    {
        if (!ModConfig.Config.GeneralConfig.ForceDustCover) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Forcing Dust Covers on Bot Weapons");
        for (var i = 0; i <= 7; i++)
        {
            var chancesData = _botEquipmentHelper.GetTierChances(i, true);
            if (chancesData == null)
                continue;
            
            foreach (var botProp in typeof(ChancesTierData).GetProperties())
            {
                var botType = botProp.Name;
                var data = botProp.GetValue(chancesData) as BotChancesData;
                if (data?.Chances == null)
                    continue;
                
                foreach (var chanceProp in typeof(ApbsChances).GetProperties())
                {
                    var keyName = chanceProp.Name;
                    if (keyName.Contains("Equipment", StringComparison.OrdinalIgnoreCase) || keyName == "Generation")
                        continue;

                    var chancesDict = chanceProp.GetValue(data.Chances) as Dictionary<string, double>;
                    if (chancesDict == null)
                        continue;
                    
                    SetModChance(chancesDict, 100, "mod_reciever");
                }
            }
        }
    }
    private void ForceScopes()
    {
        if (!ModConfig.Config.GeneralConfig.ForceScopeSlot) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Forcing Optics on Bot Weapons");
        for (var i = 0; i <= 7; i++)
        {
            var chancesData = _botEquipmentHelper.GetTierChances(i, true);
            if (chancesData == null)
                continue;
            
            foreach (var botProp in typeof(ChancesTierData).GetProperties())
            {
                var botType = botProp.Name;
                var data = botProp.GetValue(chancesData) as BotChancesData;
                if (data?.Chances == null)
                    continue;
                
                foreach (var chanceProp in typeof(ApbsChances).GetProperties())
                {
                    var keyName = chanceProp.Name;
                    if (keyName.Contains("Equipment", StringComparison.OrdinalIgnoreCase) || keyName == "Generation")
                        continue;

                    var chancesDict = chanceProp.GetValue(data.Chances) as Dictionary<string, double>;
                    if (chancesDict == null)
                        continue;
                    
                    SetModChance(chancesDict, 100, "mod_scope");
                }
            }
        }
    }
    private void MuzzleChances()
    {
        if (!ModConfig.Config.GeneralConfig.ForceMuzzle) return;
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("Forcing Muzzles on Bot Weapons");
        for (var i = 1; i <= 7; i++)
        {
            var chancesData = _botEquipmentHelper.GetTierChances(i, true);
            if (chancesData == null)
                continue;

            var indexPosition = i - 1;

            foreach (var botProp in typeof(ChancesTierData).GetProperties())
            {
                var botType = botProp.Name;
                var data = botProp.GetValue(chancesData) as BotChancesData;
                if (data?.Chances == null)
                    continue;

                foreach (var chanceProp in typeof(ApbsChances).GetProperties())
                {
                    var keyName = chanceProp.Name;
                    if (keyName.Contains("Equipment", StringComparison.OrdinalIgnoreCase) || keyName == "Generation")
                        continue;

                    var chancesDict = chanceProp.GetValue(data.Chances) as Dictionary<string, double>;
                    if (chancesDict == null)
                        continue;

                    var muzzleChance = ModConfig.Config.GeneralConfig.MuzzleChance[indexPosition];
                    SetModChance(chancesDict, muzzleChance, "mod_muzzle", "mod_muzzle_000", "mod_muzzle_001");
                }
            }
        }
    }
    
    private void SetModChance(Dictionary<string, double> dict, double value, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (dict.ContainsKey(key))
                dict[key] = value;
        }
    }
    #endregion
}