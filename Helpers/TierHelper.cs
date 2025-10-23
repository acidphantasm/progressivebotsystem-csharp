using _progressiveBotSystem.Globals;

namespace _progressiveBotSystem.Utils;

public static class TierHelper
{
    public static int GetTierByLevel(int level)
    {
        return new TierInformation().Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).Tier;
    }
    
    public static int GetTierUpperLevelDeviation(int level)
    {
        return new TierInformation().Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).BotMaxLevelVariance;
    }
    public static int GetTierLowerLevelDeviation(int level)
    {
        return new TierInformation().Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).BotMinLevelVariance;
    }
    public static int GetScavTierUpperLevelDeviation(int level)
    {
        return new TierInformation().Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).ScavMaxLevelVariance;
    }
    public static int GetScavTierLowerLevelDeviation(int level)
    {
        return new TierInformation().Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).ScavMinLevelVariance;
    }
}