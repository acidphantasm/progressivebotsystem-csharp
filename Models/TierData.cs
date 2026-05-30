namespace ProgressiveBotSystem.Models;

public class TierData
{
    public int Tier { get; set; }
    public int PlayerMinLevel { get; set; }
    public int PlayerMaxLevel { get; set; }
    public int BotMinLevelVariance { get; set; }
    public int BotMaxLevelVariance { get; set; }
    public int ScavMinLevelVariance { get; set; }
    public int ScavMaxLevelVariance { get; set; }
}