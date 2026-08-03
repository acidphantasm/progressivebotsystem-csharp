using ProgressiveBotSystem.Globals;
using ProgressiveBotSystem.Models;
using ProgressiveBotSystem.Services;
using ProgressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.HttpResponse;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Profile;
using SPTarkov.Server.Core.Utils;

namespace ProgressiveBotSystem.Routers;

[Injectable]
public class StaticRouterHooks : StaticRouter
{
    public StaticRouterHooks(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        ApbsLogger apbsLogger,
        BotLogService botLogService,
        ProfileHelper profileHelper,
        ProfileActivityService profileActivityService,
        CustomBotLootCacheService customBotLootCacheService) : base(
        jsonUtil,
        GetCustomRoutes()
    )
    {
        _jsonUtil = jsonUtil;
        _apbsLogger = apbsLogger;
        _botLogService = botLogService;
        _profileHelper = profileHelper;
        _profileActivityService = profileActivityService;
        _customBotLootCacheService = customBotLootCacheService;
    }
    
    private static ApbsLogger? _apbsLogger;
    private static JsonUtil? _jsonUtil;
    private static BotLogService? _botLogService;
    private static ProfileHelper? _profileHelper;
    private static ProfileActivityService? _profileActivityService;
    private static CustomBotLootCacheService? _customBotLootCacheService;

    private static List<RouteAction> GetCustomRoutes()
    {
        return
        [
            new RouteAction<GenerateBotsRequestData>(
                "/client/game/bot/generate",
                async (
                    url,
                    info,
                    sessionId,
                    output,
                    token
                ) =>
                {
                    if (ModConfig.Config.Debug.EnableBotEquipmentLog)
                    {
                        try
                        {
                            var outputData = _jsonUtil.Deserialize<GetBodyResponseData<IEnumerable<BotBase?>>>(output);

                            if (outputData?.Data != null)
                            {
                                // Fire and forget
                                _ = Task.Run(() => _botLogService.StartBotLogging(outputData.Data));
                            }
                        }
                        catch (Exception ex)
                        {
                            _apbsLogger.Error($"Failed to deserialize bots: {ex}");
                        }
                    }
                    return output;
                }),
            
            new RouteAction<StartLocalRaidRequestData>(
                "/client/match/local/start",
                async (
                    url,
                    info,
                    sessionId,
                    output,
                    token
                ) =>
                {
                    try
                    {
                        var fullProfile = _profileHelper.GetFullProfile(sessionId);
                        var profileActivityRaidData = _profileActivityService.GetProfileActivityRaidData(sessionId);
                    
                        RaidInformation.CurrentSessionId = fullProfile.ProfileInfo.ProfileId;
                    
                        var prestigeLevel = fullProfile.CharacterData.PmcData.Info.PrestigeLevel ?? 0;
                        RaidInformation.HighestPrestigeLevel =
                            prestigeLevel >= RaidInformation.HighestPrestigeLevel
                                ? prestigeLevel
                                : RaidInformation.HighestPrestigeLevel;
                    
                        RaidInformation.CurrentRaidLevel = fullProfile.CharacterData.PmcData.Info.Level ?? 1;
                    
                        RaidInformation.RaidLocation = info.Location;
                        RaidInformation.NightTime = profileActivityRaidData.RaidConfiguration.IsNightRaid;
                        RaidInformation.IsInRaid = true;

                        _apbsLogger.Debug($"Current SessionID: {RaidInformation.CurrentSessionId}");
                        _apbsLogger.Debug($"Highest Prestige Level: {RaidInformation.HighestPrestigeLevel}");
                        _apbsLogger.Debug($"Current Raid Level: {RaidInformation.CurrentRaidLevel}");
                        _apbsLogger.Debug($"Night Raid: {RaidInformation.NightTime}");
                        _apbsLogger.Debug($"In Raid: {RaidInformation.IsInRaid}");
                    }
                    catch (Exception ex)
                    {
                        _apbsLogger.Error("Match Start Router hook failed.");
                    }
                    return output;
                }),
            
            new RouteAction<EndLocalRaidRequestData>(
                "/client/match/local/end",
                async (
                    url,
                    info,
                    sessionId,
                    output,
                    token
                ) =>
                {
                    RaidInformation.IsInRaid = false;
                    _customBotLootCacheService.ClearApbsCache();
                    
                    _apbsLogger.Debug($"In Raid: {RaidInformation.IsInRaid}");
                    return output;
                }),
            
            new RouteAction<EmptyRequestData>(
                "/client/game/start",
                async (
                    url,
                    info,
                    sessionId,
                    output,
                    token
                ) =>
                {
                    try
                    {
                        var fullProfile = _profileHelper.GetFullProfile(sessionId);
                        RaidInformation.FreshProfile = fullProfile.ProfileInfo.IsWiped.Value;
                        _apbsLogger.Debug($"Fresh Profile: {RaidInformation.FreshProfile}");
                    }
                    catch (Exception ex)
                    {
                        _apbsLogger.Error("Game Start Router hook failed.");
                    }
                    return output;
                }),
            
            new RouteAction<EmptyRequestData>(
                "/client/profile/status",
                async (
                    url,
                    info,
                    sessionId,
                    output,
                    token
                ) =>
                {
                    _apbsLogger.Debug("/client/profile/status");
                    try
                    {
                        var fullProfile = _profileHelper.GetFullProfile(sessionId);
                        RaidInformation.FreshProfile = fullProfile.ProfileInfo.IsWiped.Value;
                        _apbsLogger.Debug($"Fresh Profile: {RaidInformation.FreshProfile}");
                    }
                    catch (Exception ex)
                    {
                        _apbsLogger.Error("Profile Status hook failed.");
                    }
                    return output;
                })
        ];
    }
}