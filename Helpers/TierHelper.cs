using _progressiveBotSystem.Globals;
using SPTarkov.DI.Annotations;

namespace _progressiveBotSystem.Utils;

[Injectable(InjectionType.Singleton)]
public class TierHelper
{
    private readonly TierInformation _tierInformation;
    public TierHelper(
        TierInformation tierInformation)
    {
        _tierInformation = tierInformation;
    }
    public int GetTierByLevel(int level)
    {
        return _tierInformation.Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).Tier;
    }
    
    public int GetTierUpperLevelDeviation(int level)
    {
        return _tierInformation.Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).BotMaxLevelVariance;
    }
    public  int GetTierLowerLevelDeviation(int level)
    {
        return _tierInformation.Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).BotMinLevelVariance;
    }
    public int GetScavTierUpperLevelDeviation(int level)
    {
        return _tierInformation.Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).ScavMaxLevelVariance;
    }
    public int GetScavTierLowerLevelDeviation(int level)
    {
        return _tierInformation.Tiers.FirstOrDefault(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel).ScavMinLevelVariance;
    }
}