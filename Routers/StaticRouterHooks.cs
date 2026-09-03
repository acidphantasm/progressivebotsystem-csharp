namespace ProgressiveBotSystem.Routers;

using Globals;
using Models;
using Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.HttpResponse;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Profile;
using SPTarkov.Server.Core.Utils;
using Utils;

[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class StaticRouterHooks : StaticRouter
{
    private static ApbsLogger _apbsLogger = null!;
    private static JsonUtil _jsonUtil = null!;
    private static BotLogService _botLogService = null!;
    private static ProfileHelper _profileHelper = null!;
    private static ProfileActivityService _profileActivityService = null!;
    private static CustomBotLootCacheService _customBotLootCacheService = null!;

    public StaticRouterHooks(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        ApbsLogger apbsLogger,
        BotLogService botLogService,
        ProfileHelper profileHelper,
        ProfileActivityService profileActivityService,
        CustomBotLootCacheService customBotLootCacheService
    )
        : base(jsonUtil, GetCustomRoutes())
    {
        _jsonUtil = jsonUtil;
        _apbsLogger = apbsLogger;
        _botLogService = botLogService;
        _profileHelper = profileHelper;
        _profileActivityService = profileActivityService;
        _customBotLootCacheService = customBotLootCacheService;
    }

    private static List<RouteAction> GetCustomRoutes()
    {
        return
        [
            new RouteAction<GenerateBotsRequestData>(
                "/client/game/bot/generate",
                async (url, info, sessionId, output, token) =>
                {
                    if (ModConfig.Config.Debug.EnableBotEquipmentLog)
                    {
                        try
                        {
                            var outputData = _jsonUtil.Deserialize<
                                GetBodyResponseData<IEnumerable<BotBase?>>
                            >(output);

                            if (outputData?.Data != null)
                            {
                                // Fire and forget
                                _ = Task.Run(
                                    () => _botLogService.StartBotLogging(outputData.Data),
                                    token
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            _apbsLogger.Error($"Failed to deserialize bots: {ex}");
                        }
                    }
                    return output!;
                }
            ),
            new RouteAction<StartLocalRaidRequestData>(
                "/client/match/local/start",
                async (url, info, sessionId, output, token) =>
                {
                    try
                    {
                        var fullProfile = _profileHelper.GetFullProfile(sessionId);
                        if (
                            fullProfile.CharacterData?.PmcData?.Info?.MemberCategory
                            == MemberCategory.UnitTest
                        )
                        {
                            return output!;
                        }

                        var profileActivityRaidData =
                            _profileActivityService.GetProfileActivityRaidData(sessionId);

                        RaidInformation.CurrentSessionId = fullProfile.ProfileInfo.ProfileId;

                        var prestigeLevel =
                            fullProfile.CharacterData?.PmcData?.Info?.PrestigeLevel ?? 0;
                        RaidInformation.HighestPrestigeLevel =
                            prestigeLevel >= RaidInformation.HighestPrestigeLevel
                                ? prestigeLevel
                                : RaidInformation.HighestPrestigeLevel;

                        var level = fullProfile.CharacterData?.PmcData?.Info?.Level ?? 1;
                        RaidInformation.AddOrUpdatePlayerLevel(sessionId, level);

                        RaidInformation.RaidLocation = info.Location;
                        RaidInformation.NightTime = profileActivityRaidData
                            .RaidConfiguration
                            .IsNightRaid;
                        RaidInformation.IsInRaid = true;

                        _apbsLogger.Debug($"Current SessionID: {RaidInformation.CurrentSessionId}");
                        _apbsLogger.Debug(
                            $"Highest Prestige Level: {RaidInformation.HighestPrestigeLevel}"
                        );
                        _apbsLogger.Debug(
                            $"Current Raid Level: {RaidInformation.CurrentRaidLevel}"
                        );
                        _apbsLogger.Debug($"Night Raid: {RaidInformation.NightTime}");
                        _apbsLogger.Debug($"In Raid: {RaidInformation.IsInRaid}");
                    }
                    catch (Exception ex)
                    {
                        _apbsLogger.Error("Match Start Router hook failed.");
                    }
                    return output!;
                }
            ),
            new RouteAction<EndLocalRaidRequestData>(
                "/client/match/local/end",
                async (url, info, sessionId, output, token) =>
                {
                    RaidInformation.IsInRaid = false;
                    RaidInformation.ClearRaidLevels();
                    _customBotLootCacheService.ClearApbsCache();

                    _apbsLogger.Debug($"In Raid: {RaidInformation.IsInRaid}");
                    return output!;
                }
            ),
            new RouteAction<EmptyRequestData>(
                "/client/game/start",
                async (url, info, sessionId, output, token) =>
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
                    return output!;
                }
            ),
            new RouteAction<EmptyRequestData>(
                "/client/profile/status",
                async (url, info, sessionId, output, token) =>
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
                    return output!;
                }
            ),
        ];
    }
}
