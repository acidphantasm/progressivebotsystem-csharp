namespace ProgressiveBotSystem.Models;

using System.Collections.Concurrent;

public class RaidInformation
{
    public static bool FreshProfile { get; set; } = false;
    public static string? CurrentSessionId { get; set; }
    public static int HighestPrestigeLevel { get; set; } = 0;
    public static string? RaidLocation { get; set; }
    public static bool NightTime { get; set; } = false;
    public static bool IsInRaid { get; set; } = false;
    
    private static readonly ConcurrentDictionary<string, int> RaidLevels = new();

    public static void AddOrUpdatePlayerLevel(string sessionId, int level)
    {
        RaidLevels[sessionId] = level;
    }

    public static void ClearRaidLevels()
    {
        RaidLevels.Clear();
    }

    public static int PlayerCount
    {
        get => RaidLevels.Count;
    }

    public static int CurrentRaidLevel
    {
        get => RaidLevels.IsEmpty ? 1 : (int)Math.Round(RaidLevels.Values.Average());
    }
}