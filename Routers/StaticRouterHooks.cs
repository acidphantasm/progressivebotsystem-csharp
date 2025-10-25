using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Routers;

[Injectable]
public class StaticRouterHooks : StaticRouter
{
    private static ApbsLogger? _apbsLogger;

    public StaticRouterHooks(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        ApbsLogger apbsLogger) : base(
        jsonUtil,
        GetCustomRoutes()
    )
    {
        _apbsLogger = apbsLogger;
    }

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
                    if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("/client/game/bot/generate");
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
                    var fullProfile = ServiceLocator.ServiceProvider.GetService<ProfileHelper>()!.GetFullProfile(sessionId);
                    var profileActivityRaidData = ServiceLocator.ServiceProvider.GetService<ProfileActivityService>()!.GetProfileActivityRaidData(sessionId);
                    
                    RaidInformation.CurrentSessionId = fullProfile.ProfileInfo.ProfileId;
                    
                    var prestigeLevel = fullProfile.CharacterData.PmcData.Info.PrestigeLevel ?? 0;
                    RaidInformation.HighestPrestigeLevel =
                        prestigeLevel >= RaidInformation.HighestPrestigeLevel
                            ? prestigeLevel
                            : RaidInformation.HighestPrestigeLevel;
                    
                    RaidInformation.RaidLocation = info.Location;
                    RaidInformation.NightTime = profileActivityRaidData.RaidConfiguration.IsNightRaid;
                    
                    if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("/client/match/local/start");
                    if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug($"Current SessionID: {RaidInformation.CurrentSessionId}");
                    if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug($"Highest Prestige Level: {RaidInformation.HighestPrestigeLevel}");
                    if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug($"Night Raid: {RaidInformation.NightTime}");
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
                    if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("/client/game/start");
                    try
                    {
                        var fullProfile =
                            ServiceLocator.ServiceProvider.GetService<ProfileHelper>().GetFullProfile(sessionId);
                        RaidInformation.FreshProfile = fullProfile.ProfileInfo.IsWiped;
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
                    if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("/client/profile/status");
                    try
                    {
                        var fullProfile =
                            ServiceLocator.ServiceProvider.GetService<ProfileHelper>().GetFullProfile(sessionId);
                        RaidInformation.FreshProfile = fullProfile.ProfileInfo.IsWiped;
                    }
                    catch (Exception ex)
                    {
                        _apbsLogger.Error("Game Start Router hook failed.");
                    }
                    return output;
                })
        ];
    }
}