using ProgressiveBotSystem.Globals;
using ProgressiveBotSystem.Models;
using ProgressiveBotSystem.Models.Enums;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace ProgressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class TierHelper(TierInformation tierInformation, DateHelper dateHelper, RandomUtil randomUtil)
{
    private TierData GetTierInfo(int level)
    {
        var tiers = tierInformation.Tiers;
        var matchingData = tiers.First(x => level >= x.PlayerMinLevel && level <= x.PlayerMaxLevel);

        if (!dateHelper.IsAprilFoolsEnabled())
        {
            return matchingData;
        }

        var ordered = tiers.OrderBy(x => x.Tier).ToList();
        var index = ordered.FindIndex(x => x.Tier == matchingData.Tier);
        var invertedIndex = ordered.Count - 1 - index;

        return ordered[invertedIndex];
    }

    public int GetTierByLevel(int level)
    {
        return GetTierInfo(level).Tier;
    }

    public MinMax<int> GetBotLevelRange(int playerLevel, bool isScav)
    {
        var tierData = GetTierInfo(playerLevel);
        if (ModConfig.Config.LevelPickingMode == BotLevelPickingMode.Weighted)
        {
            var weights = isScav ? tierData.ScavLevelWeights : tierData.PmcLevelWeights;
            if (weights is { Count: > 0 })
            {
                var targetTier = PickWeightedTier(weights);
                var targetTierData = tierInformation.Tiers.FirstOrDefault(t => t.Tier == targetTier);

                if (targetTierData is not null)
                {
                    return new MinMax<int>(targetTierData.PlayerMinLevel, targetTierData.PlayerMaxLevel);
                }
            }
        }

        var lowerDeviation = isScav ? tierData.ScavMinLevelVariance : tierData.BotMinLevelVariance;
        var upperDeviation = isScav ? tierData.ScavMaxLevelVariance : tierData.BotMaxLevelVariance;

        var minLevel = playerLevel - lowerDeviation;
        var maxLevel = playerLevel + upperDeviation;

        return new MinMax<int>(minLevel, maxLevel);
    }

    private int PickWeightedTier(List<WeightedTierChance> weights)
    {
        var totalWeight = weights.Sum(w => w.Weight);
        if (totalWeight <= 0)
        {
            return weights[0].Tier;
        }

        var roll = randomUtil.GetInt(1, totalWeight);
        var cumulative = 0;

        foreach (var entry in weights)
        {
            cumulative += entry.Weight;
            if (roll <= cumulative)
            {
                return entry.Tier;
            }
        }

        return weights[^1].Tier;
    }
}
