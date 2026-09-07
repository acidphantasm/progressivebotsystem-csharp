using System.Text.Json.Serialization;

namespace ProgressiveBotSystem.Models;

public class WeightedTierChance
{
    [JsonPropertyName("tier")]
    public int Tier { get; set; }

    [JsonPropertyName("weight")]
    public int Weight { get; set; }
}

public class TierData
{
    public int Tier { get; set; }
    public int PlayerMinLevel { get; set; }
    public int PlayerMaxLevel { get; set; }
    public int BotMinLevelVariance { get; set; }
    public int BotMaxLevelVariance { get; set; }
    public int ScavMinLevelVariance { get; set; }
    public int ScavMaxLevelVariance { get; set; }

    public List<WeightedTierChance> PmcLevelWeights { get; set; } = [];
    public List<WeightedTierChance> ScavLevelWeights { get; set; } = [];
}
