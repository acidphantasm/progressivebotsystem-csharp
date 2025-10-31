namespace _progressiveBotSystem.Globals;

public class RaidInformation
{
    public static bool FreshProfile { get; set; } = false;
    public static string? CurrentSessionId { get; set; }
    public static int HighestPrestigeLevel { get; set; } = 0;
    public static string? RaidLocation { get; set; }
    public static bool NightTime { get; set; } = false;
    public static bool IsInRaid { get; set; } = false;
}