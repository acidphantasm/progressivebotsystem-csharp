using System.Collections.Concurrent;

namespace ProgressiveBotSystem.Models;

public class RaidInformation
{
    private static readonly ConcurrentDictionary<string, int> RaidLevels = new();
    private static readonly ConcurrentDictionary<string, int> RaidPrestigeLevels = new();

    public static bool FreshProfile { get; set; } = false;
    public static string? CurrentSessionId { get; set; }
    public static string? RaidLocation { get; set; }
    public static bool NightTime { get; set; } = false;
    public static bool IsInRaid { get; set; } = false;

    public static int PlayerCount
    {
        get { return RaidLevels.Count; }
    }

    public static int CurrentRaidLevel
    {
        get { return RaidLevels.IsEmpty ? 1 : (int)Math.Round(RaidLevels.Values.Average()); }
    }

    public static int HighestPrestigeLevel
    {
        get { return RaidPrestigeLevels.IsEmpty ? 0 : RaidPrestigeLevels.Values.Max(); }
    }

    public static void AddOrUpdatePlayerLevel(string sessionId, int level)
    {
        RaidLevels[sessionId] = level;
    }

    public static void AddOrUpdatePlayerPrestige(string sessionId, int prestigeLevel)
    {
        RaidPrestigeLevels[sessionId] = prestigeLevel;
    }

    public static void ClearRaidLevels()
    {
        RaidLevels.Clear();
        RaidPrestigeLevels.Clear();
    }
}
