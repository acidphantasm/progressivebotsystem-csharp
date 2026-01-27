using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Services;
using _progressiveBotSystem.Utils;
using _progressiveBotSystem.Web.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.HttpResponse;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Routers;

[Injectable]
public class StaticRouterHooks : StaticRouter
{
    public StaticRouterHooks(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        ApbsLogger apbsLogger,
        BotLogService botLogService) : base(
        jsonUtil,
        GetCustomRoutes()
    )
    {
        _jsonUtil = jsonUtil;
        _apbsLogger = apbsLogger;
        _botLogService = botLogService;
    }
    
    private static ApbsLogger? _apbsLogger;
    private static JsonUtil? _jsonUtil;
    private static BotLogService? _botLogService;

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
                    output
                ) =>
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
                    return output;
                }),
            
            new RouteAction<StartLocalRaidRequestData>(
                "/client/match/local/start",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) =>
                {
                    try
                    {
                        var fullProfile = ServiceLocator.ServiceProvider.GetService<ProfileHelper>()!.GetFullProfile(sessionId);
                        var profileActivityRaidData = ServiceLocator.ServiceProvider.GetService<ProfileActivityService>()!.GetProfileActivityRaidData(sessionId);
                    
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
                    output
                ) =>
                {
                    RaidInformation.IsInRaid = false;
                    
                    _apbsLogger.Debug($"In Raid: {RaidInformation.IsInRaid}");
                    return output;
                }),
            
            new RouteAction<EmptyRequestData>(
                "/client/game/start",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) =>
                {
                    try
                    {
                        var fullProfile = ServiceLocator.ServiceProvider.GetService<ProfileHelper>().GetFullProfile(sessionId);
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
                    output
                ) =>
                {
                    _apbsLogger.Debug("/client/profile/status");
                    try
                    {
                        var fullProfile = ServiceLocator.ServiceProvider.GetService<ProfileHelper>().GetFullProfile(sessionId);
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