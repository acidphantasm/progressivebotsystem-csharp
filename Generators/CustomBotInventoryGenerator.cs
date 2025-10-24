using System.Collections.Frozen;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Models.Enums;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Models.Spt.Logging;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace _progressiveBotSystem.Generators;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 90010)]
public class CustomBotInventoryGenerator
{
    public CustomBotInventoryGenerator(
        ISptLogger<CustomBotInventoryGenerator> logger,
        RandomUtil randomUtil,
        ProfileActivityService profileActivityService,
        BotLootGenerator botLootGenerator,
        BotGeneratorHelper botGeneratorHelper,
        ProfileHelper profileHelper,
        BotHelper botHelper,
        WeightedRandomHelper weightedRandomHelper,
        ItemHelper itemHelper,
        WeatherHelper weatherHelper,
        ServerLocalisationService serverLocalisationService,
        BotEquipmentFilterService botEquipmentFilterService,
        BotEquipmentModPoolService botEquipmentModPoolService,
        BotInventoryContainerService botInventoryContainerService,
        ConfigServer configServer,
        BotInventoryGenerator botInventoryGenerator,
        BotEquipmentHelper botEquipmentHelper,
        CustomBotWeaponGenerator customBotWeaponGenerator,
        CustomBotEquipmentModGenerator customBotEquipmentModGenerator)
    {
        _logger = logger;
        _randomUtil = randomUtil;
        _profileActivityService = profileActivityService;
        _botLootGenerator = botLootGenerator;
        _botGeneratorHelper = botGeneratorHelper;
        _profileHelper = profileHelper;
        _botHelper = botHelper;
        _weightedRandomHelper = weightedRandomHelper;
        _itemHelper = itemHelper;
        _weatherHelper = weatherHelper;
        _serverLocalisationService = serverLocalisationService;
        _botEquipmentFilterService = botEquipmentFilterService;
        _botEquipmentModPoolService = botEquipmentModPoolService;
        _botInventoryContainerService = botInventoryContainerService;
        _botInventoryGenerator = botInventoryGenerator;
        _botEquipmentHelper = botEquipmentHelper;
        _customBotWeaponGenerator = customBotWeaponGenerator;
        _customBotEquipmentModGenerator = customBotEquipmentModGenerator;
        
        _botConfig = configServer.GetConfig<BotConfig>();
        _pmcConfig = configServer.GetConfig<PmcConfig>();
    }
    
    private readonly ISptLogger<CustomBotInventoryGenerator> _logger;
    private readonly RandomUtil _randomUtil;
    private readonly ProfileActivityService _profileActivityService;
    private readonly BotLootGenerator _botLootGenerator;
    private readonly BotGeneratorHelper _botGeneratorHelper;
    private readonly ProfileHelper _profileHelper;
    private readonly BotHelper _botHelper;
    private readonly WeightedRandomHelper _weightedRandomHelper;
    private readonly ItemHelper _itemHelper;
    private readonly WeatherHelper _weatherHelper;
    private readonly ServerLocalisationService _serverLocalisationService;
    private readonly BotEquipmentFilterService _botEquipmentFilterService;
    private readonly BotEquipmentModPoolService _botEquipmentModPoolService;
    private readonly BotInventoryContainerService _botInventoryContainerService;
    private readonly BotInventoryGenerator _botInventoryGenerator;
    
    private readonly BotEquipmentHelper _botEquipmentHelper;
    private readonly CustomBotWeaponGenerator _customBotWeaponGenerator;
    private readonly CustomBotEquipmentModGenerator _customBotEquipmentModGenerator;
    
    private readonly BotConfig _botConfig;
    private readonly PmcConfig _pmcConfig;
    
    
    private readonly FrozenSet<ApbsEquipmentSlots> _equipmentSlotsWithInventory =
    [
        ApbsEquipmentSlots.Pockets,
        ApbsEquipmentSlots.TacticalVest,
        ApbsEquipmentSlots.Backpack,
        ApbsEquipmentSlots.SecuredContainer,
        ApbsEquipmentSlots.ArmouredRig
    ];
    
    private readonly FrozenSet<ApbsEquipmentSlots> _excludedEquipmentSlots =
    [
        ApbsEquipmentSlots.Pockets,
        ApbsEquipmentSlots.FirstPrimaryWeapon_LongRange,
        ApbsEquipmentSlots.FirstPrimaryWeapon_ShortRange,
        ApbsEquipmentSlots.SecondPrimaryWeapon_LongRange,
        ApbsEquipmentSlots.SecondPrimaryWeapon_ShortRange,
        ApbsEquipmentSlots.Holster,
        ApbsEquipmentSlots.ArmorVest,
        ApbsEquipmentSlots.TacticalVest,
        ApbsEquipmentSlots.FaceCover,
        ApbsEquipmentSlots.Headwear,
        ApbsEquipmentSlots.Earpiece,
        ApbsEquipmentSlots.ArmouredRig
    ];
    
    private readonly FrozenSet<string> _slotsToCheck = [nameof(EquipmentSlots.Pockets), nameof(EquipmentSlots.SecuredContainer)];
    
    public void GenerateAndAddEquipmentToBot(
        MongoId botId,
        MongoId sessionId,
        BotTypeInventory templateInventory,
        ApbsChances wornItemChances,
        BotBaseInventory botInventory,
        BotGenerationDetails botGenerationDetails,
        GetRaidConfigurationRequestData? raidConfig,
        int tierNumber,
        QuestData? questData)
    {
        if (
            !_botConfig.Equipment.TryGetValue(
                _botGeneratorHelper.GetBotEquipmentRole(botGenerationDetails.RoleLowercase),
                out var botEquipConfig
            )
        )
        {
            _logger.Error($"Bot Equipment generation failed, unable to find equipment filters for: {botGenerationDetails.RoleLowercase}");

            return;
        }
        var randomisationDetails = _botHelper.GetBotRandomizationDetails(botGenerationDetails.BotLevel, botEquipConfig);

        // Is PMC + generating armband + armband forcing is enabled
        if (_pmcConfig.ForceArmband.Enabled && botGenerationDetails.IsPmc)
        {
            // Replace armband pool with single tpl from config
            if (templateInventory.Equipment.TryGetValue(EquipmentSlots.ArmBand, out var armbands))
            {
                // Get tpl based on pmc side
                var armbandTpl =
                    botGenerationDetails.RoleLowercase == "pmcusec" ? _pmcConfig.ForceArmband.Usec : _pmcConfig.ForceArmband.Bear;

                armbands.Clear();
                armbands.Add(armbandTpl, 1);

                // Force armband spawn to 100%
                wornItemChances.EquipmentChances["Armband"] = 100;
            }
        }

        // Get profile of player generating bots, we use their level later on
        var pmcProfile = _profileHelper.GetPmcProfile(sessionId);
        var botEquipmentRole = _botGeneratorHelper.GetBotEquipmentRole(botGenerationDetails.RoleLowercase);
        
        var equipmentPool = _botEquipmentHelper.GetEquipmentByBotRole(botGenerationDetails.RoleLowercase, tierNumber);
        var modPool = _botEquipmentHelper.GetModsByBotRole(botGenerationDetails.RoleLowercase, tierNumber);

        // Iterate over all equipment slots of bot, do it in specific order to reduce conflicts
        // e.g. ArmorVest should be generated after TacticalVest
        // or FACE_COVER before HEADWEAR
        foreach (var (equipmentSlot, itemsWithWeightPool) in equipmentPool)
        {
            // Skip some slots as they need to be done in a specific order + with specific parameter values
            // e.g. Weapons
            if (_excludedEquipmentSlots.Contains(equipmentSlot))
            {
                continue;
            }
            
            GenerateEquipment(
                new ApbsGenerateEquipmentProperties
                {
                    BotId = botId,
                    RootEquipmentSlot = equipmentSlot,
                    RootEquipmentPool = itemsWithWeightPool,
                    ModPool = modPool,
                    SpawnChances = wornItemChances,
                    BotData = new ApbsBotData
                    {
                        Role = botGenerationDetails.RoleLowercase,
                        Level = botGenerationDetails.BotLevel,
                        EquipmentRole = botEquipmentRole,
                        Tier = tierNumber,
                    },
                    Inventory = botInventory,
                    BotEquipmentConfig = botEquipConfig,
                    RandomisationDetails = randomisationDetails,
                    GeneratingPlayerLevel = pmcProfile?.Info?.Level ?? 1,
                }, questData
            );
        }

        // Generate below in specific order
        GenerateEquipment(
            new ApbsGenerateEquipmentProperties
            {
                BotId = botId,
                RootEquipmentSlot = ApbsEquipmentSlots.Pockets,
                // Unheard profiles have unique sized pockets
                RootEquipmentPool = GetPocketPoolByGameEdition(
                    botGenerationDetails.GameVersion,
                    equipmentPool[ApbsEquipmentSlots.Pockets],
                    botGenerationDetails.IsPmc
                ),
                ModPool = modPool,
                SpawnChances = wornItemChances,
                BotData = new ApbsBotData
                {
                    Role = botGenerationDetails.RoleLowercase,
                    Level = botGenerationDetails.BotLevel,
                    EquipmentRole = botEquipmentRole,
                    Tier = tierNumber,
                },
                Inventory = botInventory,
                BotEquipmentConfig = botEquipConfig,
                RandomisationDetails = randomisationDetails,
                GenerateModsBlacklist = [ItemTpl.POCKETS_1X4_TUE, ItemTpl.POCKETS_LARGE],
                GeneratingPlayerLevel = pmcProfile?.Info?.Level ?? 1,
            }, questData
        );

        GenerateEquipment(
            new ApbsGenerateEquipmentProperties
            {
                BotId = botId,
                RootEquipmentSlot = ApbsEquipmentSlots.FaceCover,
                RootEquipmentPool = equipmentPool[ApbsEquipmentSlots.FaceCover],
                ModPool = modPool,
                SpawnChances = wornItemChances,
                BotData = new ApbsBotData
                {
                    Role = botGenerationDetails.RoleLowercase,
                    Level = botGenerationDetails.BotLevel,
                    EquipmentRole = botEquipmentRole,
                    Tier = tierNumber,
                },
                Inventory = botInventory,
                BotEquipmentConfig = botEquipConfig,
                RandomisationDetails = randomisationDetails,
                GeneratingPlayerLevel = pmcProfile?.Info?.Level ?? 1,
            }, questData
        );

        GenerateEquipment(
            new ApbsGenerateEquipmentProperties
            {
                BotId = botId,
                RootEquipmentSlot = ApbsEquipmentSlots.Headwear,
                RootEquipmentPool = equipmentPool[ApbsEquipmentSlots.Headwear],
                ModPool = modPool,
                SpawnChances = wornItemChances,
                BotData = new ApbsBotData
                {
                    Role = botGenerationDetails.RoleLowercase,
                    Level = botGenerationDetails.BotLevel,
                    EquipmentRole = botEquipmentRole,
                    Tier = tierNumber,
                },
                Inventory = botInventory,
                BotEquipmentConfig = botEquipConfig,
                RandomisationDetails = randomisationDetails,
                GeneratingPlayerLevel = pmcProfile?.Info?.Level ?? 1,
            }, questData
        );

        GenerateEquipment(
            new ApbsGenerateEquipmentProperties
            {
                BotId = botId,
                RootEquipmentSlot = ApbsEquipmentSlots.Earpiece,
                RootEquipmentPool = equipmentPool[ApbsEquipmentSlots.Earpiece],
                ModPool = modPool,
                SpawnChances = wornItemChances,
                BotData = new ApbsBotData
                {
                    Role = botGenerationDetails.RoleLowercase,
                    Level = botGenerationDetails.BotLevel,
                    EquipmentRole = botEquipmentRole,
                    Tier = tierNumber,
                },
                Inventory = botInventory,
                BotEquipmentConfig = botEquipConfig,
                RandomisationDetails = randomisationDetails,
                GeneratingPlayerLevel = pmcProfile?.Info?.Level ?? 1,
            }, questData
        );

        var hasArmorVest = GenerateEquipment(
            new ApbsGenerateEquipmentProperties
            {
                BotId = botId,
                RootEquipmentSlot = ApbsEquipmentSlots.ArmorVest,
                RootEquipmentPool = equipmentPool[ApbsEquipmentSlots.ArmorVest],
                ModPool = modPool,
                SpawnChances = wornItemChances,
                BotData = new ApbsBotData
                {
                    Role = botGenerationDetails.RoleLowercase,
                    Level = botGenerationDetails.BotLevel,
                    EquipmentRole = botEquipmentRole,
                    Tier = tierNumber,
                },
                Inventory = botInventory,
                BotEquipmentConfig = botEquipConfig,
                RandomisationDetails = randomisationDetails,
                GeneratingPlayerLevel = pmcProfile?.Info?.Level ?? 1,
            }, questData
        );

        // Bot is flagged as always needing a vest
        if (!hasArmorVest)
        {
            wornItemChances.EquipmentChances["TacticalVest"] = 100;
        }

        GenerateEquipment(
            new ApbsGenerateEquipmentProperties
            {
                BotId = botId,
                RootEquipmentSlot = ApbsEquipmentSlots.TacticalVest,
                RootEquipmentPool = equipmentPool[ApbsEquipmentSlots.TacticalVest],
                ModPool = modPool,
                SpawnChances = wornItemChances,
                BotData = new ApbsBotData
                {
                    Role = botGenerationDetails.RoleLowercase,
                    Level = botGenerationDetails.BotLevel,
                    EquipmentRole = botEquipmentRole,
                    Tier = tierNumber,
                },
                Inventory = botInventory,
                BotEquipmentConfig = botEquipConfig,
                RandomisationDetails = randomisationDetails,
                GeneratingPlayerLevel = pmcProfile?.Info?.Level ?? 1,
            }, questData
        );
    }

    public void GenerateAndAddWeaponsToBot(
        MongoId botId,
        BotTypeInventory templateInventory,
        ApbsChances equipmentChances,
        MongoId sessionId,
        BotBaseInventory botInventory,
        BotGenerationDetails botGenerationDetails,
        ApbsGeneration itemGenerationLimitsMinMax,
        int tierNumber,
        QuestData? questData)
    {
        var weaponSlotsToFill = GetDesiredWeaponsForBot(equipmentChances);
        foreach (var desiredWeapons in weaponSlotsToFill)
            // Add weapon to bot if true and bot json has something to put into the slot
        {
            if (desiredWeapons.ShouldSpawn && templateInventory.Equipment[desiredWeapons.Slot].Any())
            {
                AddWeaponAndMagazinesToInventory(
                    botId,
                    sessionId,
                    desiredWeapons,
                    templateInventory,
                    botInventory,
                    equipmentChances,
                    botGenerationDetails,
                    itemGenerationLimitsMinMax,
                    tierNumber,
                    questData
                );
            }
        }
    }
    
    private Dictionary<MongoId, double>? GetPocketPoolByGameEdition(
        string chosenGameVersion,
        Dictionary<MongoId, double> equipmentPool,
        bool isPmc
    )
    {
        return chosenGameVersion == GameEditions.UNHEARD && isPmc
            ? new Dictionary<MongoId, double> { [ItemTpl.POCKETS_1X4_TUE] = 1 }
            : equipmentPool;
    }

    private bool GenerateEquipment(ApbsGenerateEquipmentProperties settings, QuestData? questData)
    {
        double? spawnChance = _slotsToCheck.Contains(settings.RootEquipmentSlot.ToString())
            ? 100
            : settings.SpawnChances.EquipmentChances.GetValueOrDefault(settings.RootEquipmentSlot.ToString());

        if (!spawnChance.HasValue)
        {
            _logger.Warning(_serverLocalisationService.GetText("bot-no_spawn_chance_defined_for_equipment_slot", settings.RootEquipmentSlot));

            return false;
        }

        // Give armoured vest instead of tactical rig if they have no vest
        if (settings.RootEquipmentSlot == ApbsEquipmentSlots.TacticalVest &&
            settings.Inventory.Items.FindIndex(item => item.SlotId == "ArmorVest") == -1)
        {
            settings.RootEquipmentPool = _botEquipmentHelper.GetEquipmentByBotRoleAndSlot(settings.BotData.Role,
                settings.BotData.Tier, ApbsEquipmentSlots.ArmouredRig);
        }
        
        // Roll dice on equipment item
        var shouldSpawn = _randomUtil.GetChance100(spawnChance.Value);
        if (shouldSpawn && settings.RootEquipmentPool?.Count != 0)
        {
            TemplateItem? pickedItemDb = null;
            var found = false;

            // Limit attempts to find a compatible item as it's expensive to check them all
            var maxAttempts = settings.RootEquipmentPool.Count;
            var attempts = 0;
            while (!found)
            {
                if (settings.RootEquipmentPool.Count == 0)
                {
                    return false;
                }

                var chosenItemTpl = _weightedRandomHelper.GetWeightedValue(settings.RootEquipmentPool);
                var dbResult = _itemHelper.GetItem(chosenItemTpl);

                if (!dbResult.Key)
                {
                    _logger.Error(_serverLocalisationService.GetText("bot-missing_item_template", chosenItemTpl));
                    if (_logger.IsLogEnabled(LogLevel.Debug))
                    {
                        _logger.Debug($"EquipmentSlot-> {settings.RootEquipmentSlot}");
                    }

                    // Remove picked item
                    settings.RootEquipmentPool.Remove(chosenItemTpl);

                    attempts++;
                    continue;
                }

                // Is the chosen item compatible with other items equipped
                var compatibilityResult = _botGeneratorHelper.IsItemIncompatibleWithCurrentItems(
                    settings.Inventory.Items,
                    chosenItemTpl,
                    settings.RootEquipmentSlot.ToString()
                );
                if (compatibilityResult.Incompatible ?? false)
                {
                    // Tried x different items that failed, stop
                    if (attempts > maxAttempts)
                    {
                        return false;
                    }

                    // Remove picked item from pool
                    settings.RootEquipmentPool.Remove(chosenItemTpl);

                    // Increment times tried
                    attempts++;
                }
                else
                {
                    // Success
                    found = true;
                    pickedItemDb = dbResult.Value;
                }
            }
            
            // Create root item
            var id = new MongoId();
            Item item = new()
            {
                Id = id,
                Template = pickedItemDb.Id,
                ParentId = settings.Inventory.Equipment,
                SlotId = settings.RootEquipmentSlot.ToString(),
                Upd = _botGeneratorHelper.GenerateExtraPropertiesForItem(pickedItemDb, settings.BotData.Role, true),
            };

            var botEquipBlacklist = _botEquipmentFilterService.GetBotEquipmentBlacklist(
                settings.BotData.EquipmentRole,
                settings.GeneratingPlayerLevel.GetValueOrDefault(1)
            );

            // Edge case: Filter the armor items mod pool if bot exists in config dict + config has armor slot
            if (
                _botConfig.Equipment.ContainsKey(settings.BotData.EquipmentRole)
                && settings.RandomisationDetails?.RandomisedArmorSlots != null
                && settings.RandomisationDetails.RandomisedArmorSlots.Contains(settings.RootEquipmentSlot.ToString())
            )
            // Filter out mods from relevant blacklist
            {
                settings.ModPool[pickedItemDb.Id] = _botInventoryGenerator.GetFilteredDynamicModsForItem(pickedItemDb.Id, botEquipBlacklist.Equipment);
            }

            var itemIsOnGenerateModBlacklist =
                settings.GenerateModsBlacklist != null && settings.GenerateModsBlacklist.Contains(pickedItemDb.Id);
            // Does item have slots for sub-mods to be inserted into
            if (pickedItemDb.Properties?.Slots is not null && pickedItemDb.Properties.Slots.Any() && !itemIsOnGenerateModBlacklist)
            {
                var childItemsToAdd = _customBotEquipmentModGenerator.GenerateModsForEquipment(
                    [item],
                    id,
                    pickedItemDb,
                    settings,
                    botEquipBlacklist
                );
                settings.Inventory.Items.AddRange(childItemsToAdd);
            }
            else
            {
                // No slots, add root item only
                settings.Inventory.Items.Add(item);
            }

            // Cache container ready for items to be added in
            if (_equipmentSlotsWithInventory.Contains(settings.RootEquipmentSlot))
            {
                if (settings.RootEquipmentSlot == ApbsEquipmentSlots.ArmouredRig)
                    settings.RootEquipmentSlot = ApbsEquipmentSlots.TacticalVest;
                
                var newRootEquipmentSlot = Enum.Parse<EquipmentSlots>(settings.RootEquipmentSlot.ToString());
                _botInventoryContainerService.AddEmptyContainerToBot(settings.BotId, newRootEquipmentSlot, item);
            }
            
            
            return true;
        }
        
        return false;
    }
    
    private IEnumerable<DesiredWeapons> GetDesiredWeaponsForBot(ApbsChances equipmentChances)
    {
        var shouldSpawnPrimary = _randomUtil.GetChance100(equipmentChances.EquipmentChances["FirstPrimaryWeapon"]);
        return
        [
            new DesiredWeapons { Slot = EquipmentSlots.FirstPrimaryWeapon, ShouldSpawn = shouldSpawnPrimary },
            new DesiredWeapons
            {
                Slot = EquipmentSlots.SecondPrimaryWeapon,
                ShouldSpawn = shouldSpawnPrimary && _randomUtil.GetChance100(equipmentChances.EquipmentChances["SecondPrimaryWeapon"]),
            },
            new DesiredWeapons
            {
                Slot = EquipmentSlots.Holster,
                ShouldSpawn = !shouldSpawnPrimary || _randomUtil.GetChance100(equipmentChances.EquipmentChances["Holster"]), // No primary = force pistol
            },
        ];
    }
    
    private void AddWeaponAndMagazinesToInventory(
        MongoId botId,
        MongoId sessionId,
        DesiredWeapons weaponSlot,
        BotTypeInventory templateInventory,
        BotBaseInventory botInventory,
        ApbsChances equipmentChances,
        BotGenerationDetails botGenerationDetails,
        ApbsGeneration itemGenerationWeights,
        int tierNumber,
        QuestData? questData
    )
    {
        var generatedWeapon = _customBotWeaponGenerator.GenerateRandomWeapon(
            sessionId,
            weaponSlot.Slot.ToString(),
            templateInventory,
            botGenerationDetails,
            botInventory.Equipment.Value,
            equipmentChances,
            tierNumber,
            questData
        );

        botInventory.Items.AddRange(generatedWeapon.Weapon);

        _customBotWeaponGenerator.AddExtraMagazinesToInventory(
            botId,
            generatedWeapon,
            itemGenerationWeights.Items.Magazines,
            botInventory,
            botGenerationDetails.RoleLowercase
        );
    }
}