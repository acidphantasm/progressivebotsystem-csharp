using System.Reflection;
using System.Text.Json;
using ProgressiveBotSystem.Constants;
using ProgressiveBotSystem.Models;
using ProgressiveBotSystem.Models.Enums;
using SPTarkov.DI.Annotations;

namespace ProgressiveBotSystem.Services;

[Injectable(InjectionType.Singleton)]
public class BotLogReaderService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly string _logDirectory;

    public BotLogReaderService()
    {
        _logDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "logs");
    }

    private async Task<List<BotLogData>> GetBotsAsync(BotLogType type)
    {
        var filePath = GetFilePath(type);

        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<BotLogData>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<BotLogData>> GetAllBotsAsync()
    {
        var results = new List<BotLogData>();

        foreach (var type in Enum.GetValues<BotLogType>())
        {
            if (type is BotLogType.Unknown)
            {
                continue;
            }

            results.AddRange(await GetBotsAsync(type));
        }

        return results.OrderByDescending(x => x.Timestamp).ToList();
    }

    private string GetFilePath(BotLogType type)
    {
        var folder = type switch
        {
            BotLogType.Scav => LoggingFolders.Scav,
            BotLogType.Boss => LoggingFolders.Boss,
            BotLogType.Special => LoggingFolders.Special,
            BotLogType.Pmc => LoggingFolders.Pmc,
            BotLogType.Event => LoggingFolders.Event,
            BotLogType.Unhandled => LoggingFolders.UnhandledBots,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        return Path.Combine(_logDirectory, $"{folder}.json");
    }

    public Task ClearLogsAsync()
    {
        foreach (var type in Enum.GetValues<BotLogType>())
        {
            if (type is BotLogType.Unknown)
            {
                continue;
            }

            var filePath = GetFilePath(type);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // catch is specifically to not throw if someone is running a raid while someone clears the logs (and a bot is currently being written)
            }
        }

        return Task.CompletedTask;
    }
}
