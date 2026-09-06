using ProgressiveBotSystem.Constants;
using ProgressiveBotSystem.Helpers;
using ProgressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace ProgressiveBotSystem.Services;

[Injectable(InjectionType.Singleton)]
public class BotLogService(ApbsLogger apbsLogger, BotLogHelper botLogHelper, BotActivityHelper botActivityHelper)
{
    private static readonly HashSet<string> _scavRoles = typeof(ScavBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> _bossRoles = typeof(BossBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> _followerRoles = typeof(FollowerBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> _specialRoles = typeof(SpecialBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> _pmcRoles = typeof(PmcBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> _eventRoles = typeof(EventBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.Ordinal);

    public void StartBotLogging(IEnumerable<BotBase?> botData)
    {
        try
        {
            foreach (var bot in botData)
            {
                var botLogData = botLogHelper.GetBotDetails(bot);
                var logMessages = botLogHelper.GetLogMessage(botLogData);
                var enabledStringText = botActivityHelper.IsBotEnabled(botLogData.Role) ? "APBS Bot" : "Vanilla Bot";

                var logType = LoggingFolders.UnhandledBots;
                if (_scavRoles.Contains(botLogData.Role.ToLowerInvariant()))
                {
                    logType = LoggingFolders.Scav;
                }
                if (_bossRoles.Contains(botLogData.Role.ToLowerInvariant()) || _followerRoles.Contains(botLogData.Role.ToLowerInvariant()))
                {
                    logType = LoggingFolders.Boss;
                }
                if (_specialRoles.Contains(botLogData.Role.ToLowerInvariant()))
                {
                    logType = LoggingFolders.Special;
                }
                if (_pmcRoles.Contains(botLogData.Role.ToLowerInvariant()))
                {
                    logType = LoggingFolders.Pmc;
                }
                if (_eventRoles.Contains(botLogData.Role.ToLowerInvariant()))
                {
                    logType = LoggingFolders.Event;
                }

                apbsLogger.Bot(
                    logType,
                    $"{enabledStringText}",
                    "----------------------------------------------Bot spawned from cache-----------------------------------------------------",
                    $"| {logMessages[0]}",
                    $"| {logMessages[1]}",
                    $"| {logMessages[2]} {logMessages[3]}",
                    "------------------------------------------------------------------------------------------------------------------------"
                );
            }
        }
        catch (Exception ex)
        {
            apbsLogger.Warning("[BotLogService] Failed logging due to an exception. This is non-critical.");
            apbsLogger.Warning($"{ex}");
        }
    }
}
