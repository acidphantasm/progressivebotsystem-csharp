using _progressiveBotSystem.Globals;
using SPTarkov.DI.Annotations;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class TierHelper(
    TierInformation tierInformation,
    DateHelper dateHelper)
{
    private TierData GetTierInfo(int level)
    {
        var tiers = tierInformation.Tiers;
        var matchingData = tiers.First(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel);

        if (!dateHelper.IsAprilFools())
            return matchingData;

        var ordered = tiers.OrderBy(x => x.Tier).ToList();
        var index = ordered.FindIndex(x => x.Tier == matchingData.Tier);
        var invertedIndex = ordered.Count - 1 - index;

        return ordered[invertedIndex];
    }
    public int GetTierByLevel(int level)
    {
        return GetTierInfo(level).Tier;
    }

    public int GetTierUpperLevelDeviation(int level)
    {
        return GetTierInfo(level).BotMaxLevelVariance;
    }

    public int GetTierLowerLevelDeviation(int level)
    {
        return GetTierInfo(level).BotMinLevelVariance;
    }

    public int GetScavTierUpperLevelDeviation(int level)
    {
        return GetTierInfo(level).ScavMaxLevelVariance;
    }

    public int GetScavTierLowerLevelDeviation(int level)
    {
        return GetTierInfo(level).ScavMinLevelVariance;
    }
}