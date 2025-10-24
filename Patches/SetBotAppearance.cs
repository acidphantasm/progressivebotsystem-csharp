using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Utils;
using HarmonyLib;
using SPTarkov.Server.Core.Constants;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Services;

namespace _progressiveBotSystem.Patches;

public class SetBotAppearance_Patch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotGenerator),"SetBotAppearance");
    }

    [PatchPrefix]
    public static bool Prefix(BotBase bot, Appearance appearance, BotGenerationDetails botGenerationDetails)
    {
        var weightedRandomHelper = ServiceLocator.ServiceProvider.GetService<WeightedRandomHelper>();
        var databaseService = ServiceLocator.ServiceProvider.GetService<DatabaseService>();
        var seasonalEventService = ServiceLocator.ServiceProvider.GetService<SeasonalEventService>();
        var botEquipmentHelper = ServiceLocator.ServiceProvider.GetService<BotEquipmentHelper>();
        var tierHelper = ServiceLocator.ServiceProvider.GetService<TierHelper>();
        
        if (weightedRandomHelper is null || databaseService is null || botEquipmentHelper is null || seasonalEventService is null) return true;
        if (!botGenerationDetails.IsPmc) return true;

        var botLevel = bot.Info.Level ?? 0;
        var tier = tierHelper.GetTierByLevel(botLevel);
        var weatherSeason = seasonalEventService.GetActiveWeatherSeason();
        var getSeasonalData = ModConfig.Config.PmcBots.AdditionalOptions.SeasonalPmcAppearance ? true : false;
        var appearanceData = botEquipmentHelper.GetAppearanceByBotRole(botGenerationDetails.Role, tier, weatherSeason, getSeasonalData);

        bot.Customization.Head = weightedRandomHelper.GetWeightedValue(appearanceData.Head);
        bot.Customization.Feet = weightedRandomHelper.GetWeightedValue(appearanceData.Feet);
        bot.Customization.Body = weightedRandomHelper.GetWeightedValue(appearanceData.Body);

        var bodyGlobalDictDb = databaseService.GetGlobals().Configuration.Customization.Body;
        var chosenBodyTemplate = databaseService.GetCustomization()[bot.Customization.Body.Value];

        // Some bodies have matching hands, look up body to see if this is the case
        var chosenBody = bodyGlobalDictDb.FirstOrDefault(c => c.Key == chosenBodyTemplate?.Name.Trim());
        bot.Customization.Hands =
            chosenBody.Value?.IsNotRandom ?? false
                ? chosenBody.Value.Hands // Has fixed hands for chosen body, update to match
                : weightedRandomHelper.GetWeightedValue(appearanceData.Hands); // Hands can be random, choose any from weighted dict
        
        return false;
    }
}