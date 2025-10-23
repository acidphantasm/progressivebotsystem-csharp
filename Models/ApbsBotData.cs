using System.Text.Json.Serialization;

namespace _progressiveBotSystem.Models;

public record ApbsBotData
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("equipmentRole")]
    public string? EquipmentRole { get; set; }
    
    [JsonPropertyName("tier")]
    public int Tier { get; set; }
}