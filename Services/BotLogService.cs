using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace _progressiveBotSystem.Services;

[Injectable(InjectionType.Singleton)]
public class BotLogService(
    ApbsLogger apbsLogger,
    BotLogHelper botLogHelper,
    BotActivityHelper botActivityHelper)
{
    
    private static readonly HashSet<string> ScavRoles = typeof(ScavBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> BossRoles = typeof(BossBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> FollowerRoles = typeof(FollowerBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> SpecialRoles = typeof(SpecialBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> PmcRoles = typeof(PmcBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> EventRoles = typeof(EventBots).GetFields().Select(x => (string)x.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    
    public void StartBotLogging(IEnumerable<BotBase?> botData)
    {
        try
        {
            foreach (var bot in botData ?? [])
            {
                var botLogData = botLogHelper.GetBotDetails(bot);
                var logMessages = botLogHelper.GetLogMessage(botLogData);
                var enabledStringText = botActivityHelper.IsBotEnabled(botLogData.Role) ? "APBS Bot" : "Vanilla Bot";
            
                var logType = LoggingFolders.UnhandledBots;
                if (ScavRoles.Contains(botLogData.Role.ToLowerInvariant()))
                {
                    logType = LoggingFolders.Scav;
                }
                if (BossRoles.Contains(botLogData.Role.ToLowerInvariant()) || FollowerRoles.Contains(botLogData.Role.ToLowerInvariant()))
                {
                    logType = LoggingFolders.Boss;
                }
                if (SpecialRoles.Contains(botLogData.Role.ToLowerInvariant()))
                {
                    logType = LoggingFolders.Special;
                }
                if (PmcRoles.Contains(botLogData.Role.ToLowerInvariant()))
                {
                    logType = LoggingFolders.Pmc;
                }
                if (EventRoles.Contains(botLogData.Role.ToLowerInvariant()))
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
                    $"------------------------------------------------------------------------------------------------------------------------");
            }
        }
        catch (Exception ex)
        {
            apbsLogger.Warning($"[BotLogService] Failed logging due to an exception. This is non-critical.");
            apbsLogger.Warning($"{ex}");
        }
    }
}