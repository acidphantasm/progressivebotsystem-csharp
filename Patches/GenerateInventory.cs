using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using _progressiveBotSystem.Generators;
using _progressiveBotSystem.Globals;
using HarmonyLib;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Services;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;

namespace _progressiveBotSystem.Patches;

public class GenerateInventory_Patch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotInventoryGenerator), nameof(BotInventoryGenerator.GenerateInventory));
    }

    [PatchPrefix]
    public static bool Prefix(BotInventoryGenerator __instance, ref BotBaseInventory __result, MongoId botId, MongoId sessionId, BotType botJsonTemplate, BotGenerationDetails botGenerationDetails)
    {
        var botActivityHelper = ServiceLocator.ServiceProvider.GetService<BotActivityHelper>();
        var customBotInventoryGenerator = ServiceLocator.ServiceProvider.GetService<CustomBotInventoryGenerator>();
        var profileActivityService = ServiceLocator.ServiceProvider.GetService<ProfileActivityService>();
        var botLootGenerator = ServiceLocator.ServiceProvider.GetService<BotLootGenerator>();
        var botInventoryContainerService = ServiceLocator.ServiceProvider.GetService<BotInventoryContainerService>();
        var botQuestHelper = ServiceLocator.ServiceProvider.GetService<BotQuestHelper>();
        var botEquipmentHelper = ServiceLocator.ServiceProvider.GetService<BotEquipmentHelper>();
        var randomUtil = ServiceLocator.ServiceProvider.GetService<RandomUtil>();
        var apbsLogger = ServiceLocator.ServiceProvider.GetService<ApbsLogger>();
        var tierHelper = ServiceLocator.ServiceProvider.GetService<TierHelper>();
        
        if (botActivityHelper is null || !botActivityHelper.IsBotEnabled(botGenerationDetails.Role)) return true;

        if (customBotInventoryGenerator is null ||
            profileActivityService is null ||
            botLootGenerator is null ||
            botInventoryContainerService is null ||
            botQuestHelper is null ||
            botEquipmentHelper is null ||
            randomUtil is null)
        {
            Console.WriteLine("Generate Inventory Failed");
            return true;
        }
        
        var templateInventory = botJsonTemplate.BotInventory;
        var botInventory = __instance.GenerateInventoryBase();

        var raidConfig = profileActivityService.GetProfileActivityRaidData(sessionId)?.RaidConfiguration;

        // Get initial tier
        var tierNumber = tierHelper.GetTierByLevel(botGenerationDetails.BotLevel);
        
        // Check if bot should quest, and select one if possible
        QuestData? questData = null;
        var shouldQuest = botQuestHelper.ShouldBotHaveQuest(botGenerationDetails.IsPmc);
        if (shouldQuest)
        {
            questData = botQuestHelper.SelectQuest(botGenerationDetails.BotLevel, RaidInformation.RaidLocation);
            if (questData is null) shouldQuest = false;
            else
            {
                apbsLogger.Debug($"[QUEST] Level{botGenerationDetails.BotLevel} PMC was assigned quest: {questData.QuestName}");
            }
        }

        // If bot shouldn't be questing, check if it should be living in poverty
        if (botGenerationDetails.IsPmc && !shouldQuest && ModConfig.Config.PmcBots.PovertyConfig.Enable &&
            tierNumber > 1 && randomUtil.GetChance100(ModConfig.Config.PmcBots.PovertyConfig.Chance))
        {
            var minTier = Math.Max(1, tierNumber - 3);
            var maxTier = Math.Max(1, tierNumber - 1);
            var newTierNumber = randomUtil.GetInt(minTier, maxTier);
            apbsLogger.Debug($"[POVERTY] Level{botGenerationDetails.BotLevel} PMC was flagged to be in poverty. Old Tier: {tierNumber}, New Tier: {newTierNumber}");
            tierNumber = newTierNumber;
        }
        
        // Pull chances and generation by the tier number - this follows poverty to ensure you get the right data
        var chances = botEquipmentHelper.GetChancesByBotRole(botGenerationDetails.RoleLowercase, tierNumber);
        var generation = chances.Generation;

        // Finally check if they are questing, and if that quest is Fishing Gear. That quest requires a second weapon.
        if (shouldQuest && questData.QuestName == "Fishing Gear")
        {
            chances.EquipmentChances["SecondPrimaryWeapon"] = 100;
        }
        
        // Have custom generator build equipment and weapons
        customBotInventoryGenerator.GenerateAndAddEquipmentToBot(botId, sessionId, templateInventory, chances, botInventory, botGenerationDetails, raidConfig, tierNumber, questData);
        customBotInventoryGenerator.GenerateAndAddWeaponsToBot(botId, templateInventory, chances, sessionId, botInventory, botGenerationDetails, generation, tierNumber, questData);
        
        botLootGenerator.GenerateLoot(botId, sessionId, botJsonTemplate, botGenerationDetails, botInventory);
        
        if (botGenerationDetails.ClearBotContainerCacheAfterGeneration)
        {
            botInventoryContainerService.ClearCache(botId);
        }

        __result = botInventory;
        return false;
    }
}