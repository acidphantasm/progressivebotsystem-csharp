namespace ProgressiveBotSystem.Patches;

using System.Reflection;
using Globals;
using HarmonyLib;
using Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Server;

[Injectable]
public class SetBotAppearancePatch : AbstractPatch
{
    private static WeightedRandomHelper _weightedRandomHelper = default!;
    private static GlobalTable _globalTable = default!;
    private static TemplateTable _templateTable = default!;
    private static SeasonalEventService _seasonalEventService = default!;
    private static BotEquipmentHelper _botEquipmentHelper = default!;
    private static TierHelper _tierHelper = default!;

    public SetBotAppearancePatch(
        WeightedRandomHelper weightedRandomHelper,
        GlobalTable globalTable,
        TemplateTable templateTable,
        SeasonalEventService seasonalEventService,
        BotEquipmentHelper botEquipmentHelper,
        TierHelper tierHelper
    )
    {
        _weightedRandomHelper = weightedRandomHelper;
        _globalTable = globalTable;
        _templateTable = templateTable;
        _seasonalEventService = seasonalEventService;
        _botEquipmentHelper = botEquipmentHelper;
        _tierHelper = tierHelper;
    }

    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(BotGenerator), "SetBotAppearance");

    [PatchPrefix]
    public static bool Prefix(
        BotBase bot,
        Appearance appearance,
        BotGenerationDetails botGenerationDetails
    )
    {
        if (!botGenerationDetails.IsPmc)
        {
            return true;
        }

        var botLevel = bot.Info?.Level ?? 0;
        var tier = ModConfig.Config.GeneralConfig.BlickyMode
            ? 0
            : _tierHelper.GetTierByLevel(botLevel);
        var weatherSeason = _seasonalEventService.GetActiveWeatherSeason();
        var getSeasonalData = ModConfig.Config.PmcBots.AdditionalOptions.SeasonalPmcAppearance;
        var appearanceData = _botEquipmentHelper.GetAppearanceByBotRole(
            botGenerationDetails.Role,
            tier,
            weatherSeason,
            getSeasonalData
        );

        bot.Customization.Head = _weightedRandomHelper.GetWeightedValue(appearanceData.Head);
        bot.Customization.Feet = _weightedRandomHelper.GetWeightedValue(appearanceData.Feet);
        bot.Customization.Body = _weightedRandomHelper.GetWeightedValue(appearanceData.Body);

        var bodyGlobalDictDb = _globalTable.Configuration.Customization.Body;
        var chosenBodyTemplate = _templateTable.Customization[bot.Customization.Body.Value];

        // Some bodies have matching hands, look up body to see if this is the case
        var chosenBody = bodyGlobalDictDb.FirstOrDefault(c =>
            c.Key == chosenBodyTemplate?.Name.Trim()
        );
        bot.Customization.Hands =
            chosenBody.Value?.IsNotRandom ?? false
                ? chosenBody.Value.Hands // Has fixed hands for chosen body, update to match
                : _weightedRandomHelper.GetWeightedValue(appearanceData.Hands); // Hands can be random, choose any from weighted dict

        return false;
    }
}
