using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 90010)]
public class BotConfigHelper(
    DatabaseService databaseService,
    ConfigServer configServer,
    ApbsLogger apbsLogger,
    BotActivityHelper botActivityHelper,
    ItemHelper itemHelper,
    TierInformation tierInformation,
    DataLoader dataLoader)
    : IOnLoad
{
    private readonly BotConfig _botConfig = configServer.GetConfig<BotConfig>();
    private readonly PmcConfig _pmcConfig = configServer.GetConfig<PmcConfig>();
    
    
    private static readonly HashSet<string> ScavRoles = typeof(ScavBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> BossRoles = typeof(BossBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> FollowerRoles = typeof(FollowerBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> SpecialRoles = typeof(SpecialBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> PmcRoles = typeof(PmcBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);

    private readonly Dictionary<MongoId, double> _pmcItemLimits = new()
    {
        ["5448e8d04bdc2ddf718b4569"] = 1,
        ["5448e8d64bdc2dce718b4568"] = 1,
        ["5448f39d4bdc2d0a728b4568"] = 1,
        ["5448f3a64bdc2d60728b456a"] = 2,
        ["5448f3ac4bdc2dce718b4569"] = 1,
        ["5448f3a14bdc2d27728b4569"] = 1,
        ["5c99f98d86f7745c314214b3"] = 1,
        ["5c164d2286f774194c5e69fa"] = 1,
        ["550aa4cd4bdc2dd8348b456c"] = 2,
        ["55818add4bdc2d5b648b456f"] = 1,
        ["55818ad54bdc2ddc698b4569"] = 1,
        ["55818aeb4bdc2ddc698b456a"] = 1,
        ["55818ae44bdc2dde698b456c"] = 1,
        ["55818af64bdc2d5b648b4570"] = 1,
        ["5448e54d4bdc2dcc718b4568"] = 1,
        ["5447e1d04bdc2dff2f8b4567"] = 1,
        ["5a341c4686f77469e155819e"] = 1,
        ["55818b164bdc2ddc698b456c"] = 2,
        ["5448bc234bdc2d3c308b4569"] = 2,
        ["543be5dd4bdc2deb348b4569"] = 1,
        ["543be5cb4bdc2deb348b4568"] = 2,
        ["5485a8684bdc2da71d8b4567"] = 2,
        ["5d650c3e815116009f6201d2"] = 2,
        ["543be6564bdc2df4348b4568"] = 4
    };
    
    public Task OnLoad()
    {
        apbsLogger.Debug("BotConfigHelper.OnLoad()");
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
        apbsLogger.Debug("BotConfigHelper.ReapplyConfig()");
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
        apbsLogger.Debug("--Configuring PMC Bots");
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
        apbsLogger.Debug("Setting Pmc Item Limits");
        _botConfig.ItemSpawnLimits["pmc"] = _pmcItemLimits;
    }
    private void PmcLoot()
    {
        _pmcConfig.LooseWeaponInBackpackLootMinMax.Min = 0;
        _pmcConfig.LooseWeaponInBackpackLootMinMax.Max = 0;

        if (ModConfig.Config.PmcBots.LootConfig.Enable)
        {
            apbsLogger.Debug("Setting Pmc Loot");
            foreach (var item in ModConfig.Config.PmcBots.LootConfig.Blacklist ?? [])
            {
                _pmcConfig.BackpackLoot.Blacklist.Add(item);
                _pmcConfig.VestLoot.Blacklist.Add(item);
                _pmcConfig.PocketLoot.Blacklist.Add(item);
            }
        }
        else
        {
            apbsLogger.Debug("Disabling Pmc Loot");
            _botConfig.DisableLootOnBotTypes.Add("pmcusec");
            _botConfig.DisableLootOnBotTypes.Add("pmcbear");
        }

        foreach (var bot in (List<string>)["pmcbear", "pmcusec"])
        {
            databaseService.GetBots().Types[bot]!.BotInventory.Items.Backpack.Clear();
            databaseService.GetBots().Types[bot]!.BotInventory.Items.Pockets.Clear();
            databaseService.GetBots().Types[bot]!.BotInventory.Items.TacticalVest.Clear();
            databaseService.GetBots().Types[bot]!.BotInventory.Items.SpecialLoot.Clear();
        }
    }
    private void PmcScopeWhitelist()
    {
        apbsLogger.Debug("Setting Pmc Scope Whitelist");
        if (!_botConfig.Equipment.TryGetValue("pmc", out var pmcEquipment)) return;
        if (pmcEquipment is null) return;
        
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
        apbsLogger.Debug("Setting Pmc Required Slots");
        if (!_botConfig.Equipment.TryGetValue("pmc", out var pmcEquipment)) return;
        if (pmcEquipment is null) return;
        
        pmcEquipment.WeaponSlotIdsToMakeRequired = new HashSet<string>();
        pmcEquipment.WeaponSlotIdsToMakeRequired.Add("mod_stock");
        pmcEquipment.WeaponSlotIdsToMakeRequired.Add("mod_reciever");
    }
    private void PmcGameVersionWeights()
    {
        if (!ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.Enable) return;
        apbsLogger.Debug("Setting Pmc Game Version Weights");
        _pmcConfig.GameVersionWeight["standard"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.Standard;
        _pmcConfig.GameVersionWeight["left_behind"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.LeftBehind;
        _pmcConfig.GameVersionWeight["prepare_for_escape"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.PrepareForEscape;
        _pmcConfig.GameVersionWeight["edge_of_darkness"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.EdgeOfDarkness;
        _pmcConfig.GameVersionWeight["unheard_edition"] = ModConfig.Config.PmcBots.AdditionalOptions.GameVersionWeighting.UnheardEdition;
    }
    private void PmcPlateClasses()
    {
        apbsLogger.Debug("Setting Pmc Plate Classes");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            if (!PmcRoles.Contains(botType)) continue;
            
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
        apbsLogger.Debug("--Configuring Scav Bots");
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

        apbsLogger.Debug("Setting Scav Key Loot");
        if (!databaseService.GetBots().Types.TryGetValue("assault", out var assaultBot))
        {
            apbsLogger.Warning("[ScavKeyConfig] Assault bot type not found. What did you do?");
        }

        if (!databaseService.GetBots().Types.TryGetValue("marksman", out var marksmanBot))
        {
            apbsLogger.Warning("[ScavKeyConfig] Marksman bot type not found. What did you do?");
        }

        var itemValueCollection = databaseService.GetItems().Values;
        var filteredKeyItems = itemValueCollection.Where(item => itemHelper.IsOfBaseclass(item.Id, GetKeyConfig()));

        var assaultBotCount = 0;
        var marksmanBotCount = 0;
        foreach (var item in filteredKeyItems ?? [])
        {
            if (VanillaItemConstants.LabyrinthKeys.Contains(item.Id)) continue;
            if (assaultBot is not null && assaultBot.BotInventory.Items.Backpack.TryGetValue(item.Id, out var assaultItemWeight)) assaultItemWeight = 1;
            else if (assaultBot is not null)
            {
                assaultBot.BotInventory.Items.Backpack.Add(item.Id, 1);
                assaultBotCount++;
            }
            
            if (marksmanBot is not null && marksmanBot.BotInventory.Items.Backpack.TryGetValue(item.Id, out var marksmanItemWeight)) marksmanItemWeight = 1;
            else if (marksmanBot is not null)
            {
                marksmanBot.BotInventory.Items.Backpack.Add(item.Id, 1);
                marksmanBotCount++;
            }
        }
        apbsLogger.Debug($"Added {assaultBotCount} keys to Scavs and {marksmanBotCount} keys to Marksman. Key Class Config: {GetKeyConfig()}");
    }
    private MongoId GetKeyConfig()
    {
        return ModConfig.Config.ScavBots.KeyConfig.AddAllKeysToScavs ? BaseClasses.KEY : ModConfig.Config.ScavBots.KeyConfig.AddOnlyMechanicalKeysToScavs ? BaseClasses.KEY_MECHANICAL : BaseClasses.KEYCARD;
    }
    private void ScavLoot()
    {
        if (!ModConfig.Config.ScavBots.LootConfig.Enable)
        {
            apbsLogger.Debug("Disabling Scav Loot");
            foreach (var bot in ScavRoles)
            {
                _botConfig.DisableLootOnBotTypes.Add(bot);
            }
        }
        else
        {
            var bots = databaseService.GetBots().Types;
            foreach (var (botType, data) in bots)
            {
                if (!ScavRoles.Contains(botType)) continue;
                foreach (var item in ModConfig.Config.ScavBots.LootConfig.Blacklist ?? [])
                {
                    if (data is null) continue;
                    
                    data.BotInventory.Items.TacticalVest.Remove(item);
                    data.BotInventory.Items.Pockets.Remove(item);
                    data.BotInventory.Items.Backpack.Remove(item);
                }
            }
        }
    }
    private void ScavIdenticalWeightConfig()
    {
        if (!ModConfig.Config.ScavBots.AdditionalOptions.EnableScavEqualEquipmentTiering) return;
        apbsLogger.Debug("Setting Scav Equipment Weights to 1");
        for (var i = 1; i <= 7; i++)
        {
            var equipmentData = GetTierEquipmentData(i);
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
        
        apbsLogger.Debug("Setting Scav Level Deltas");
        tierInformation.Tiers[0].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier1.Min;
        tierInformation.Tiers[0].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier1.Max;
        
        tierInformation.Tiers[1].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier2.Min;
        tierInformation.Tiers[1].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier2.Max;
        
        tierInformation.Tiers[2].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier3.Min;
        tierInformation.Tiers[2].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier3.Max;
        
        tierInformation.Tiers[3].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier4.Min;
        tierInformation.Tiers[3].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier4.Max;
        
        tierInformation.Tiers[4].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier5.Min;
        tierInformation.Tiers[4].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier5.Max;
        
        tierInformation.Tiers[5].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier6.Min;
        tierInformation.Tiers[5].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier6.Max;
        
        tierInformation.Tiers[6].ScavMinLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier7.Min;
        tierInformation.Tiers[6].ScavMaxLevelVariance = ModConfig.Config.CustomScavLevelDeltas.Tier7.Max;
    }
    private void ScavPlateClasses()
    {
        var botConfigEquipment = _botConfig.Equipment;
        apbsLogger.Debug("Setting Scav Plate Classes");
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            if (!ScavRoles.Contains(botType)) continue;
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
        apbsLogger.Debug("--Configuring Boss Bots");
        BossLoot();
        BossPlateClasses();
    }
    #region BossConfigs
    private void BossLoot()
    {
        if (ModConfig.Config.BossBots.LootConfig.Enable)
        {
            apbsLogger.Debug("Disabling Boss Loot");
            foreach (var botType in BossRoles)
            {
                _botConfig.DisableLootOnBotTypes.Add(botType);
            }
        }
        else
        {
            var bots = databaseService.GetBots().Types;
            foreach (var (botType, data) in bots)
            {
                if (!BossRoles.Contains(botType)) continue;
                foreach (var item in ModConfig.Config.BossBots.LootConfig.Blacklist ?? [])
                {
                    if (data is null) continue;
                    
                    data.BotInventory.Items.TacticalVest.Remove(item);
                    data.BotInventory.Items.Pockets.Remove(item);
                    data.BotInventory.Items.Backpack.Remove(item);
                }
            }
        }
    }
    private void BossPlateClasses()
    {
        apbsLogger.Debug("Setting Boss Plate Classes");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            if (!BossRoles.Contains(botType)) continue;
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
        apbsLogger.Debug("--Configuring Follower Bots");
        FollowerLoot();
        FollowerPlateClasses();
    }
    #region FollowerConfigs
    private void FollowerLoot()
    {
        if (ModConfig.Config.FollowerBots.LootConfig.Enable)
        {
            apbsLogger.Debug("Disabling Follower Loot");
            foreach (var botType in FollowerRoles)
            {
                _botConfig.DisableLootOnBotTypes.Add(botType);
            }
        }
        else
        {
            var bots = databaseService.GetBots().Types;
            foreach (var (botType, data) in bots)
            {
                if (!FollowerRoles.Contains(botType)) continue;
                foreach (var item in ModConfig.Config.FollowerBots.LootConfig.Blacklist ?? [])
                {
                    if (data is null) continue;
                    
                    data.BotInventory.Items.TacticalVest.Remove(item);
                    data.BotInventory.Items.Pockets.Remove(item);
                    data.BotInventory.Items.Backpack.Remove(item);
                }
            }
        }
    }
    private void FollowerPlateClasses()
    {
        apbsLogger.Debug("Setting Follower Plate Classes");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            if (!FollowerRoles.Contains(botType)) continue;
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
        apbsLogger.Debug("--Configuring Special Bots");
        SpecialLoot();
        SpecialPlateClasses();
    }
    #region SpecialConfigs
    private void SpecialLoot()
    {
        if (ModConfig.Config.SpecialBots.LootConfig.Enable)
        {
            apbsLogger.Debug("Disabling Special Bot Loot");
            foreach (var botType in SpecialRoles)
            {
                _botConfig.DisableLootOnBotTypes.Add(botType);
            }
        }
        else
        {
            var bots = databaseService.GetBots().Types;
            foreach (var (botType, data) in bots)
            {
                if (!SpecialRoles.Contains(botType)) continue;
                foreach (var item in ModConfig.Config.SpecialBots.LootConfig.Blacklist ?? [])
                {
                    if (data is null) continue;
                    
                    data.BotInventory.Items.TacticalVest.Remove(item);
                    data.BotInventory.Items.Pockets.Remove(item);
                    data.BotInventory.Items.Backpack.Remove(item);
                }
            }
        }
    }
    private void SpecialPlateClasses()
    {
        apbsLogger.Debug("Setting Special Bot Plate Classes");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, equipmentFilters) in botConfigEquipment)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            if (!SpecialRoles.Contains(botType)) continue;
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
        apbsLogger.Debug("--Configuring Remainder of Bot Configs");
        SetLevelDeltas();
        RemoveRandomization();
        SetBotLevels();
        SetWeaponDurability();
        SetArmourDurability();
        AdjustNVGs();
        SetItemResourceRandomization();
        SetWeaponModLimits();
        AmmoStackCompatibility();
    }
    #region AllBotConfigs
    private void SetLevelDeltas()
    {
        if (!ModConfig.Config.CustomLevelDeltas.Enable) return;
        apbsLogger.Debug("Setting Custom Level Deltas");
        tierInformation.Tiers[0].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier1.Min;
        tierInformation.Tiers[0].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier1.Max;
        tierInformation.Tiers[1].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier2.Min;
        tierInformation.Tiers[1].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier2.Max;
        tierInformation.Tiers[2].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier3.Min;
        tierInformation.Tiers[2].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier3.Max;
        tierInformation.Tiers[3].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier4.Min;
        tierInformation.Tiers[3].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier4.Max;
        tierInformation.Tiers[4].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier5.Min;
        tierInformation.Tiers[4].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier5.Max;
        tierInformation.Tiers[5].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier6.Min;
        tierInformation.Tiers[5].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier6.Max;
        tierInformation.Tiers[6].BotMinLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier7.Min;
        tierInformation.Tiers[6].BotMaxLevelVariance = ModConfig.Config.CustomLevelDeltas.Tier7.Max;
    }
    private void RemoveRandomization()
    {
        apbsLogger.Debug("Removing Bot Randomisation");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, _) in botConfigEquipment)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            botConfigEquipment[botType]!.Randomisation = new List<RandomisationDetails>();
            botConfigEquipment[botType]!.WeightingAdjustmentsByBotLevel = new List<WeightingAdjustmentDetails>();
        }
    }
    private void SetBotLevels()
    {
        apbsLogger.Debug("Setting Bot Levels");
        var allBotTypes = databaseService.GetBots().Types;
        foreach (var (bot, botData) in allBotTypes)
        {
            allBotTypes[bot]!.BotExperience.Level.Min = 1;
            allBotTypes[bot]!.BotExperience.Level.Max = 79;
        }
    }
    
    private void SetWeaponDurability()
    {
        apbsLogger.Debug("Setting Bot Weapon Durability");
        var botDurability = _botConfig.Durability;
        foreach (var (botType, data) in botDurability.BotDurabilities)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            if (ScavRoles.Contains(botType) && ModConfig.Config.ScavBots.WeaponDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Weapon.HighestMax = ModConfig.Config.ScavBots.WeaponDurability.Max;
                botDurability.BotDurabilities[botType].Weapon.LowestMax = ModConfig.Config.ScavBots.WeaponDurability.Min;
                botDurability.BotDurabilities[botType].Weapon.MaxDelta = ModConfig.Config.ScavBots.WeaponDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Weapon.MinDelta = ModConfig.Config.ScavBots.WeaponDurability.MinDelta;
                botDurability.BotDurabilities[botType].Weapon.MinLimitPercent = ModConfig.Config.ScavBots.WeaponDurability.MinLimitPercent;
            }
            else if (BossRoles.Contains(botType) && ModConfig.Config.BossBots.WeaponDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Weapon.HighestMax = ModConfig.Config.BossBots.WeaponDurability.Max;
                botDurability.BotDurabilities[botType].Weapon.LowestMax = ModConfig.Config.BossBots.WeaponDurability.Min;
                botDurability.BotDurabilities[botType].Weapon.MaxDelta = ModConfig.Config.BossBots.WeaponDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Weapon.MinDelta = ModConfig.Config.BossBots.WeaponDurability.MinDelta;
                botDurability.BotDurabilities[botType].Weapon.MinLimitPercent = ModConfig.Config.BossBots.WeaponDurability.MinLimitPercent;
            }
            else if (FollowerRoles.Contains(botType) && ModConfig.Config.FollowerBots.WeaponDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Weapon.HighestMax = ModConfig.Config.FollowerBots.WeaponDurability.Max;
                botDurability.BotDurabilities[botType].Weapon.LowestMax = ModConfig.Config.FollowerBots.WeaponDurability.Min;
                botDurability.BotDurabilities[botType].Weapon.MaxDelta = ModConfig.Config.FollowerBots.WeaponDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Weapon.MinDelta = ModConfig.Config.FollowerBots.WeaponDurability.MinDelta;
                botDurability.BotDurabilities[botType].Weapon.MinLimitPercent = ModConfig.Config.FollowerBots.WeaponDurability.MinLimitPercent;
            }
            else if (SpecialRoles.Contains(botType) && ModConfig.Config.SpecialBots.WeaponDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Weapon.HighestMax = ModConfig.Config.SpecialBots.WeaponDurability.Max;
                botDurability.BotDurabilities[botType].Weapon.LowestMax = ModConfig.Config.SpecialBots.WeaponDurability.Min;
                botDurability.BotDurabilities[botType].Weapon.MaxDelta = ModConfig.Config.SpecialBots.WeaponDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Weapon.MinDelta = ModConfig.Config.SpecialBots.WeaponDurability.MinDelta;
                botDurability.BotDurabilities[botType].Weapon.MinLimitPercent = ModConfig.Config.SpecialBots.WeaponDurability.MinLimitPercent;
            }
        }

        if (!ModConfig.Config.PmcBots.WeaponDurability.Enable) return;
        
        botDurability.Pmc.Weapon.HighestMax = ModConfig.Config.PmcBots.WeaponDurability.Max;
        botDurability.Pmc.Weapon.LowestMax = ModConfig.Config.PmcBots.WeaponDurability.Min;
        botDurability.Pmc.Weapon.MaxDelta = ModConfig.Config.PmcBots.WeaponDurability.MaxDelta;
        botDurability.Pmc.Weapon.MinDelta = ModConfig.Config.PmcBots.WeaponDurability.MinDelta;
        botDurability.Pmc.Weapon.MinLimitPercent = ModConfig.Config.PmcBots.WeaponDurability.MinLimitPercent;
    }
    
    private void SetArmourDurability()
    {
        apbsLogger.Debug("Setting Bot Armour Durability");
        var botDurability = _botConfig.Durability;
        foreach (var (botType, data) in botDurability.BotDurabilities)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            if (ScavRoles.Contains(botType) && ModConfig.Config.ScavBots.ArmourDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Armor.HighestMaxPercent = ModConfig.Config.ScavBots.ArmourDurability.Max;
                botDurability.BotDurabilities[botType].Armor.LowestMaxPercent = ModConfig.Config.ScavBots.ArmourDurability.Min;
                botDurability.BotDurabilities[botType].Armor.MaxDelta = ModConfig.Config.ScavBots.ArmourDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Armor.MinDelta = ModConfig.Config.ScavBots.ArmourDurability.MinDelta;
                botDurability.BotDurabilities[botType].Armor.MinLimitPercent = ModConfig.Config.ScavBots.ArmourDurability.MinLimitPercent;
            }
            else if (BossRoles.Contains(botType) && ModConfig.Config.BossBots.ArmourDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Armor.HighestMaxPercent = ModConfig.Config.BossBots.ArmourDurability.Max;
                botDurability.BotDurabilities[botType].Armor.LowestMaxPercent = ModConfig.Config.BossBots.ArmourDurability.Min;
                botDurability.BotDurabilities[botType].Armor.MaxDelta = ModConfig.Config.BossBots.ArmourDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Armor.MinDelta = ModConfig.Config.BossBots.ArmourDurability.MinDelta;
                botDurability.BotDurabilities[botType].Armor.MinLimitPercent = ModConfig.Config.BossBots.ArmourDurability.MinLimitPercent;
            }
            else if (FollowerRoles.Contains(botType) && ModConfig.Config.FollowerBots.ArmourDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Armor.HighestMaxPercent = ModConfig.Config.FollowerBots.ArmourDurability.Max;
                botDurability.BotDurabilities[botType].Armor.LowestMaxPercent = ModConfig.Config.FollowerBots.ArmourDurability.Min;
                botDurability.BotDurabilities[botType].Armor.MaxDelta = ModConfig.Config.FollowerBots.ArmourDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Armor.MinDelta = ModConfig.Config.FollowerBots.ArmourDurability.MinDelta;
                botDurability.BotDurabilities[botType].Armor.MinLimitPercent = ModConfig.Config.FollowerBots.ArmourDurability.MinLimitPercent;
            }
            else if (SpecialRoles.Contains(botType) && ModConfig.Config.SpecialBots.ArmourDurability.Enable)
            {
                botDurability.BotDurabilities[botType].Armor.HighestMaxPercent = ModConfig.Config.SpecialBots.ArmourDurability.Max;
                botDurability.BotDurabilities[botType].Armor.LowestMaxPercent = ModConfig.Config.SpecialBots.ArmourDurability.Min;
                botDurability.BotDurabilities[botType].Armor.MaxDelta = ModConfig.Config.SpecialBots.ArmourDurability.MaxDelta;
                botDurability.BotDurabilities[botType].Armor.MinDelta = ModConfig.Config.SpecialBots.ArmourDurability.MinDelta;
                botDurability.BotDurabilities[botType].Armor.MinLimitPercent = ModConfig.Config.SpecialBots.ArmourDurability.MinLimitPercent;
            }
        }

        if (!ModConfig.Config.PmcBots.WeaponDurability.Enable) return;
        
        botDurability.Pmc.Armor.HighestMaxPercent = ModConfig.Config.PmcBots.ArmourDurability.Max;
        botDurability.Pmc.Armor.LowestMaxPercent = ModConfig.Config.PmcBots.ArmourDurability.Min;
        botDurability.Pmc.Armor.MaxDelta = ModConfig.Config.PmcBots.ArmourDurability.MaxDelta;
        botDurability.Pmc.Armor.MinDelta = ModConfig.Config.PmcBots.ArmourDurability.MinDelta;
        botDurability.Pmc.Armor.MinLimitPercent = ModConfig.Config.PmcBots.ArmourDurability.MinLimitPercent;
    }
    
    private void AdjustNVGs()
    {
        apbsLogger.Debug("Setting Bot Laser, Flashlight, and Nvg Activity Chances");
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, _) in botConfigEquipment)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            
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
        apbsLogger.Debug("Setting Bot Food and Medical Item Randomisation");
        // Loop the bot types instead of the botconfig, even though the actual values exist in the bot config - I need proper bot names
        var botTable = databaseService.GetBots().Types;
        foreach (var (botType, data) in botTable)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;
            
            var setValues = false;
            var foodMaxChance = 100;
            var medMaxChange = 100;
            var foodResourcePercent = 60;
            var medResourcePercent = 60;

            if (PmcRoles.Contains(botType) && ModConfig.Config.PmcBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.PmcBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.PmcBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.PmcBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.PmcBots.ResourceRandomization.MedRateUsagePercent;
            }
            else if (ScavRoles.Contains(botType) && ModConfig.Config.ScavBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.ScavBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.ScavBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.ScavBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.ScavBots.ResourceRandomization.MedRateUsagePercent;
            }
            else if (BossRoles.Contains(botType) && ModConfig.Config.BossBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.BossBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.BossBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.BossBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.BossBots.ResourceRandomization.MedRateUsagePercent;
            }
            else if (FollowerRoles.Contains(botType) && ModConfig.Config.FollowerBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.FollowerBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.FollowerBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.FollowerBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.FollowerBots.ResourceRandomization.MedRateUsagePercent;
            }
            else if (SpecialRoles.Contains(botType) && ModConfig.Config.SpecialBots.ResourceRandomization.Enable)
            {
                setValues = true;
                foodMaxChance = ModConfig.Config.SpecialBots.ResourceRandomization.FoodRateMaxChance;
                medMaxChange = ModConfig.Config.SpecialBots.ResourceRandomization.MedRateMaxChance;
                foodResourcePercent = ModConfig.Config.SpecialBots.ResourceRandomization.FoodRateUsagePercent;
                medResourcePercent = ModConfig.Config.SpecialBots.ResourceRandomization.MedRateUsagePercent;
            }

            if (!setValues) continue;

            if (!_botConfig.LootItemResourceRandomization.TryGetValue(botType, out var randomisedResourceDetails))
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
                randomisedResourceDetails.Food.ChanceMaxResourcePercent = foodMaxChance;
                randomisedResourceDetails.Food.ResourcePercent = foodResourcePercent;
                randomisedResourceDetails.Meds.ChanceMaxResourcePercent = medMaxChange;
                randomisedResourceDetails.Meds.ResourcePercent = medResourcePercent;
            }
        }
    }
    private void SetWeaponModLimits()
    {
        if (!ModConfig.Config.GeneralConfig.ForceWeaponModLimits) return;
        apbsLogger.Debug("Setting Bot Weapon Mod Limits");

        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, data) in botConfigEquipment)
        {
            if (!botActivityHelper.IsBotEnabled(botType)) continue;

            botConfigEquipment[botType].WeaponModLimits ??= new ModLimits()
            {
                ScopeLimit = 2,
                LightLaserLimit = 1
            };
            
            botConfigEquipment[botType]!.WeaponModLimits!.ScopeLimit = ModConfig.Config.GeneralConfig.ScopeLimit;
            botConfigEquipment[botType]!.WeaponModLimits!.LightLaserLimit = ModConfig.Config.GeneralConfig.TacticalLimit;
        }
        
        botConfigEquipment["pmc"].WeaponModLimits.ScopeLimit = ModConfig.Config.GeneralConfig.ScopeLimit;
        botConfigEquipment["pmc"].WeaponModLimits.LightLaserLimit = ModConfig.Config.GeneralConfig.TacticalLimit;
    }
    private void AmmoStackCompatibility()
    {
        apbsLogger.Debug("Setting Bot Secure Container Ammo Stack Compatibility");
        
        _botConfig.SecureContainerAmmoStackCount =
            ModConfig.Config.CompatibilityConfig.GeneralSecureContainerAmmoStacks;
    }
    #endregion
    private void AllBotsConfigsBypassEnableCheck()
    {
        apbsLogger.Debug("--Configuring Remainder of Bot Configs that bypass enablement checks");
        NormalizeHealth();
        NormalizeSkills();
    }
    #region AllBotsConfigsBypassingEnableCheck
    private void NormalizeHealth()
    {
        if (!ModConfig.Config.NormalizedHealthPool.Enable) return;

        apbsLogger.Debug("Normalising Bot Health");
        var botTable = databaseService.GetBots().Types;
        foreach (var (botType, data) in botTable)
        {
            var excluded = ModConfig.Config.NormalizedHealthPool.ExcludedBots;

            var isBear = botType == "bear";
            var isUsec = botType == "usec";

            var shouldSkip =
                excluded.Contains(botType) ||
                (isBear && excluded.Contains("pmcbear")) ||
                (isUsec && excluded.Contains("pmcusec"));

            if (shouldSkip) continue;
            
            if (data is null)
            {
                apbsLogger.Error($"[HEALTH NORMALIZATION] Bot type is unknown: {botType}");
                continue;
            }
            var bodyParts = data.BotHealth.BodyParts;
            foreach (var bodyPart in bodyParts)
            {
                bodyPart.Head.Min = ModConfig.Config.NormalizedHealthPool.HealthHead > 0 ? ModConfig.Config.NormalizedHealthPool.HealthHead : 35;
                bodyPart.Head.Max = ModConfig.Config.NormalizedHealthPool.HealthHead > 0 ? ModConfig.Config.NormalizedHealthPool.HealthHead : 35;
                bodyPart.Chest.Min = ModConfig.Config.NormalizedHealthPool.HealthChest > 0 ? ModConfig.Config.NormalizedHealthPool.HealthChest : 85;
                bodyPart.Chest.Max = ModConfig.Config.NormalizedHealthPool.HealthChest > 0 ? ModConfig.Config.NormalizedHealthPool.HealthChest : 85;
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
        apbsLogger.Debug("Setting Bot Skills");
        var botTable = databaseService.GetBots().Types;
        foreach (var (botType, data) in botTable)
        {
            if (data is null)
            {
                apbsLogger.Error($"[SKILL NORMALIZATION] Bot type is unknown: {botType}");
                continue;
            }
            if (botType is "usec" or "bear" or "pmcusec" or "pmcbear") continue;
            foreach (var (skill, minMaxData) in data.BotSkills.Common)
            {
                if (skill == "Strength" || !(minMaxData.Max > 100)) continue;
                if (skill is "BotReload" or "BotSound")
                {
                    apbsLogger.Debug($"[SKILL NORMALIZATION] Removed Skill: {skill} from {botType}");
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
        apbsLogger.Debug("--Configuring Settings that require special handling");
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
        apbsLogger.Debug("Removing T7 Thermals from Database");
        for (var i = 1; i <= 7; i++)
        {
            if (ModConfig.Config.GeneralConfig.EnableT7Thermals && i >= ModConfig.Config.GeneralConfig.StartTier) continue;

            var modsData = GetTierModsData(i);
            
            if (!modsData.TryGetValue(ItemTpl.MOUNT_NOROTOS_TITANIUM_ADVANCED_TACTICAL, out var tatmMods)) continue;
            if (!tatmMods.TryGetValue("mod_nvg", out var nvgSlot)) continue;
            if (!nvgSlot.Remove(ItemTpl.MOUNT_PVS7_WILCOX_ADAPTER)) continue;
            
            apbsLogger.Debug($"[THERMAL] Removed T7’s from Tier{i}");
        }
    }

    private void SetPlateChances()
    {
        if (!ModConfig.Config.GeneralConfig.PlateChances.Enable) return;
        apbsLogger.Debug("Setting Bot Plate Chances");
        for (var i = 1; i <= 7; i++)
        {
            var chancesData = GetTierChancesData(i);
            if (chancesData == null)
                continue;
            
            var indexPosition = i - 1;
            
            foreach (var botProp in typeof(ChancesTierData).GetProperties())
            {
                var botType = botProp.Name.ToLowerInvariant();
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
                    
                    if (botType == "pmcusec" || botType == "pmcbear")
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.PmcMainPlateChance[indexPosition], ["back_plate", "front_plate"], botType);
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.PmcSidePlateChance[indexPosition], ["left_side_plate", "right_side_plate"], botType);
                    }
                    if (botType == "followerbirdeye" || botType == "followerbigpipe" || botType.Contains("boss"))
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.BossMainPlateChance[indexPosition], ["back_plate", "front_plate"], botType);
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.BossSidePlateChance[indexPosition], ["left_side_plate", "right_side_plate"], botType);
                    }
                    if (botType == "scav")
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.ScavMainPlateChance[indexPosition], ["back_plate", "front_plate"], botType);
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.ScavSidePlateChance[indexPosition], ["left_side_plate", "right_side_plate"], botType);
                    }
                    if (botType == "exusec" || botType == "pmcbot" || botType.Contains("sectant"))
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.SpecialMainPlateChance[indexPosition], ["back_plate", "front_plate"], botType);
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.SpecialSidePlateChance[indexPosition], ["left_side_plate", "right_side_plate"], botType);
                    }
                    if (botType == "default")
                    {
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.FollowerMainPlateChance[indexPosition], ["back_plate", "front_plate"], botType);
                        SetModChance(chancesDict, ModConfig.Config.GeneralConfig.PlateChances.FollowerSidePlateChance[indexPosition], ["left_side_plate", "right_side_plate"], botType);
                    }
                }
            }
        }
    }
    private void ForceStock()
    {
        if (!ModConfig.Config.GeneralConfig.ForceStock) return;
        apbsLogger.Debug("Forcing Stocks on Bot Weapons");
        for (var i = 1; i <= 7; i++)
        {
            var chancesData = GetTierChancesData(i);
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
                    
                    SetModChance(chancesDict, 100, ["mod_stock", "mod_stock_000", "mod_stock_001", "mod_stock_akms", "mod_stock_axis"], botType);
                }
            }
        }
    }
    private void ForceDustCover()
    {
        if (!ModConfig.Config.GeneralConfig.ForceDustCover) return;
        apbsLogger.Debug("Forcing Dust Covers on Bot Weapons");
        for (var i = 1; i <= 7; i++)
        {
            var chancesData = GetTierChancesData(i);
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
                    
                    SetModChance(chancesDict, 100, ["mod_reciever"], botType);
                }
            }
        }
    }
    private void ForceScopes()
    {
        if (!ModConfig.Config.GeneralConfig.ForceScopeSlot) return;
        apbsLogger.Debug("Forcing Optics on Bot Weapons");
        for (var i = 1; i <= 7; i++)
        {
            var chancesData = GetTierChancesData(i);
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
                    
                    SetModChance(chancesDict, 100, ["mod_scope"], botType);
                }
            }
        }
    }
    private void MuzzleChances()
    {
        if (!ModConfig.Config.GeneralConfig.ForceMuzzle) return;
        apbsLogger.Debug("Forcing Muzzles on Bot Weapons");
        for (var i = 1; i <= 7; i++)
        {
            var chancesData = GetTierChancesData(i);
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
                    SetModChance(chancesDict, muzzleChance, ["mod_muzzle", "mod_muzzle_000", "mod_muzzle_001"], botType);
                }
            }
        }
    }
    
    private void SetModChance(Dictionary<string, double> dict, double value, string[] keys, string botType)
    {
        foreach (var key in keys)
        {
            dict[key] = value;
        }
    }
    #endregion
    
    #region Helpers
    /// <summary>
    ///     Returns the ChancesTierData for the given tier.
    ///     Throws if the tier is not between 1 and 7.
    /// </summary>
    private ChancesTierData GetTierChancesData(int tier)
    {
        if (!dataLoader.AllTierDataDirty.Tiers.TryGetValue(tier, out var tierData))
            throw new ArgumentOutOfRangeException(nameof(tier), $"Tier {tier} is invalid.");

        return tierData.ChancesData;
    }

    /// <summary>
    ///     Returns the AmmoTierData for the given tier.
    ///     Throws if the tier is not between 1 and 7.
    /// </summary>
    private AmmoTierData GetTierAmmoData(int tier)
    {
        if (!dataLoader.AllTierDataDirty.Tiers.TryGetValue(tier, out var tierData))
            throw new ArgumentOutOfRangeException(nameof(tier), $"Tier {tier} is invalid.");

        return tierData.AmmoData;
    }

    /// <summary>
    ///     Returns the Mods dictionary for the given tier.
    ///     Throws if the tier is not between 1 and 7.
    /// </summary>
    private Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> GetTierModsData(int tier)
    {
        if (!dataLoader.AllTierDataDirty.Tiers.TryGetValue(tier, out var tierData))
            throw new ArgumentOutOfRangeException(nameof(tier), $"Tier {tier} is invalid.");

        return tierData.ModsData;
    }

    /// <summary>
    ///     Returns the EquipmentTierData for the given tier.
    ///     Throws if the tier is not between 1 and 7.
    /// </summary>
    private EquipmentTierData GetTierEquipmentData(int tier)
    {
        if (!dataLoader.AllTierDataDirty.Tiers.TryGetValue(tier, out var tierData))
            throw new ArgumentOutOfRangeException(nameof(tier), $"Tier {tier} is invalid.");

        return tierData.EquipmentData;
    }
    
    #endregion
}