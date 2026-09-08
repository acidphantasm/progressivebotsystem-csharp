using ProgressiveBotSystem.Constants;
using ProgressiveBotSystem.Helpers;
using ProgressiveBotSystem.Models;
using ProgressiveBotSystem.Models.Enums;
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
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> _bossRoles = typeof(BossBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> _followerRoles = typeof(FollowerBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> _specialRoles = typeof(SpecialBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> _pmcRoles = typeof(PmcBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> _eventRoles = typeof(EventBots)
        .GetFields()
        .Select(x => (string)x.GetValue(null)!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public void StartBotLogging(IEnumerable<BotBase?> botData)
    {
        try
        {
            foreach (var bot in botData)
            {
                var role = bot?.Info?.Settings?.Role ?? "Unknown";

                if (role == "Unknown")
                {
                    continue;
                }

                var isApbsBot = botActivityHelper.IsBotEnabled(role);

                var botLogData = botLogHelper.GetBotDetails(bot, isApbsBot);

                botLogData.BotType = GetBotType(botLogData.Role);
                botLogData.IsApbsBot = isApbsBot;

                WriteBotLog(botLogData);
            }
        }
        catch (Exception ex)
        {
            apbsLogger.Warning($"[BotLogService] Failed logging due to an exception. This is non-critical. {ex.Message}");
        }
    }

    private BotLogType GetBotType(string role)
    {
        if (_scavRoles.Contains(role))
        {
            return BotLogType.Scav;
        }

        if (_bossRoles.Contains(role) || _followerRoles.Contains(role))
        {
            return BotLogType.Boss;
        }

        if (_specialRoles.Contains(role))
        {
            return BotLogType.Special;
        }

        if (_pmcRoles.Contains(role))
        {
            return BotLogType.Pmc;
        }

        if (_eventRoles.Contains(role))
        {
            return BotLogType.Event;
        }

        return BotLogType.Unhandled;
    }

    private void WriteBotLog(BotLogData botLogData)
    {
        var logFolder = botLogData.BotType switch
        {
            BotLogType.Scav => LoggingFolders.Scav,
            BotLogType.Boss => LoggingFolders.Boss,
            BotLogType.Special => LoggingFolders.Special,
            BotLogType.Pmc => LoggingFolders.Pmc,
            BotLogType.Event => LoggingFolders.Event,
            _ => LoggingFolders.UnhandledBots,
        };

        apbsLogger.Bot(logFolder, botLogData);
    }
}
