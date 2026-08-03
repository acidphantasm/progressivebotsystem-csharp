using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using HarmonyLib;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using ProgressiveBotSystem.Generators;
using ProgressiveBotSystem.Globals;
using ProgressiveBotSystem.Helpers;
using ProgressiveBotSystem.Models;
using ProgressiveBotSystem.Utils;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Services.Bot;

namespace ProgressiveBotSystem.Patches;

[Injectable]
public class GenerateInventoryPatch : AbstractPatch
{
    private static BotActivityHelper _botActivityHelper = default!;
    private static CustomBotInventoryGenerator _customBotInventoryGenerator = default!;
    private static BotInventoryContainerService _botInventoryContainerService = default!;
    private static BotQuestHelper _botQuestHelper = default!;
    private static BotEquipmentHelper _botEquipmentHelper = default!;
    private static RandomUtil _randomUtil = default!;
    private static ApbsLogger _apbsLogger = default!;
    private static CustomBotLootGenerator _customBotLootGenerator = default!;

    public GenerateInventoryPatch(
        BotActivityHelper botActivityHelper,
        CustomBotInventoryGenerator customBotInventoryGenerator,
        BotInventoryContainerService botInventoryContainerService,
        BotQuestHelper botQuestHelper,
        BotEquipmentHelper botEquipmentHelper,
        RandomUtil randomUtil,
        ApbsLogger apbsLogger,
        CustomBotLootGenerator customBotLootGenerator)
    {
        _botActivityHelper = botActivityHelper;
        _customBotInventoryGenerator = customBotInventoryGenerator;
        _botInventoryContainerService = botInventoryContainerService;
        _botQuestHelper = botQuestHelper;
        _botEquipmentHelper = botEquipmentHelper;
        _randomUtil = randomUtil;
        _apbsLogger = apbsLogger;
        _customBotLootGenerator = customBotLootGenerator;
    }
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotInventoryGenerator), nameof(BotInventoryGenerator.GenerateInventory));
    }

    [PatchPrefix]
    public static bool Prefix(BotInventoryGenerator __instance, ref BotBaseInventory __result, MongoId botId, MongoId sessionId, BotType botJsonTemplate, BotGenerationDetails botGenerationDetails)
    {
        if (RaidInformation.FreshProfile || RaidInformation.CurrentSessionId is null || !_botActivityHelper.IsBotEnabled(botGenerationDetails.Role))
            return true;
        
        var templateInventory = botJsonTemplate.BotInventory;
        var botInventory = __instance.GenerateInventoryBase();

        // Get initial tier
        var tierNumber = (int)botGenerationDetails.GetExtensionData()["Tier"];
        
        // Check if bot should quest, and select one if possible
        QuestData? questData = null;
        var shouldQuest = _botQuestHelper.ShouldBotHaveQuest(botGenerationDetails.IsPmc);
        if (shouldQuest)
        {
            questData = _botQuestHelper.SelectQuest(botGenerationDetails.BotLevel, RaidInformation.RaidLocation);
            if (questData is null) shouldQuest = false;
            else
            {
                _apbsLogger.Debug($"[QUEST] Level{botGenerationDetails.BotLevel} PMC was assigned quest: {questData.QuestName}");
            }
        }

        // If bot shouldn't be questing, check if it should be living in poverty
        if (botGenerationDetails.IsPmc && !shouldQuest && ModConfig.Config.PmcBots.PovertyConfig.Enable && !ModConfig.Config.GeneralConfig.BlickyMode &&
            tierNumber > 1 && _randomUtil.GetChance100(ModConfig.Config.PmcBots.PovertyConfig.Chance))
        {
            var minTier = Math.Max(1, tierNumber - 3);
            var maxTier = Math.Max(1, tierNumber - 1);
            var newTierNumber = _randomUtil.GetInt(minTier, maxTier);
            _apbsLogger.Debug($"[POVERTY] Level{botGenerationDetails.BotLevel} PMC was flagged to be in poverty. Old Tier: {tierNumber}, New Tier: {newTierNumber}");
            tierNumber = newTierNumber;
            botGenerationDetails.ExtensionData["Tier"] = tierNumber;
        }
        
        // Pull chances and generation by the tier number - this follows poverty to ensure you get the right data
        var chancesData = _botEquipmentHelper.GetChancesByBotRole(botGenerationDetails.RoleLowercase, tierNumber);
        var chances = new ApbsChances
        {
            EquipmentChances = new Dictionary<string, double>(chancesData.EquipmentChances),
            EquipmentModsChances = new Dictionary<string, double>(chancesData.EquipmentModsChances),
            WeaponModsChances = new Dictionary<string, double>(chancesData.WeaponModsChances),
            AssaultCarbineChances = new Dictionary<string, double>(chancesData.AssaultCarbineChances),
            SniperRifleChances = new Dictionary<string, double>(chancesData.SniperRifleChances),
            MarksmanRifleChances = new Dictionary<string, double>(chancesData.MarksmanRifleChances),
            AssaultRifleChances = new Dictionary<string, double>(chancesData.AssaultRifleChances),
            MachineGunChances = new Dictionary<string, double>(chancesData.MachineGunChances),
            SubmachineGunChances = new Dictionary<string, double>(chancesData.SubmachineGunChances),
            HandgunChances = new Dictionary<string, double>(chancesData.HandgunChances),
            RevolverChances = new Dictionary<string, double>(chancesData.RevolverChances),
            ShotgunChances = new Dictionary<string, double>(chancesData.ShotgunChances),
            Generation = chancesData.Generation // shared reference
        };
        var generation = chances.Generation;

        // Finally check if they are questing, and if that quest is Fishing Gear. That quest requires a second weapon.
        if (shouldQuest && questData?.QuestName == "Fishing Gear")
        {
            chances.EquipmentChances["SecondPrimaryWeapon"] = 100;
        }
        
        // Have custom generator build equipment and weapons
        _customBotInventoryGenerator.GenerateAndAddEquipmentToBot(botId, sessionId, templateInventory, chances, botInventory, botGenerationDetails, tierNumber, questData);
        _customBotInventoryGenerator.GenerateAndAddWeaponsToBot(botId, templateInventory, chances, sessionId, botInventory, botGenerationDetails, generation, tierNumber, questData);
        
        _customBotLootGenerator.GenerateLoot(botId, sessionId, botJsonTemplate, botGenerationDetails, botInventory, botGenerationDetails.BotLevel, tierNumber);
        
        if (botGenerationDetails.ClearBotContainerCacheAfterGeneration)
        {
            _botInventoryContainerService.ClearCache(botId);
        }
        
        __result = botInventory;
        return false;
    }
}