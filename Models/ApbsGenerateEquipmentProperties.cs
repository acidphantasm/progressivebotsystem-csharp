using System.Text.Json.Serialization;
using _progressiveBotSystem.Models.Enums;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace _progressiveBotSystem.Models;

public record ApbsGenerateEquipmentProperties
{
    public MongoId BotId { get; set; }

    /// <summary>
    ///     Root Slot being generated
    /// </summary>
    [JsonPropertyName("rootEquipmentSlot")]
    public ApbsEquipmentSlots RootEquipmentSlot { get; set; }

    /// <summary>
    ///     Equipment pool for root slot being generated
    /// </summary>
    [JsonPropertyName("rootEquipmentPool")]
    public Dictionary<MongoId, double>? RootEquipmentPool { get; set; }

    [JsonPropertyName("modPool")]
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>? ModPool { get; set; }

    /// <summary>
    ///     Dictionary of mod items and their chance to spawn for this bot type
    /// </summary>
    [JsonPropertyName("spawnChances")]
    public ApbsChances? SpawnChances { get; set; }

    /// <summary>
    ///     Bot-specific properties
    /// </summary>
    [JsonPropertyName("botData")]
    public ApbsBotData BotData { get; set; }

    [JsonPropertyName("inventory")]
    public BotBaseInventory? Inventory { get; set; }

    [JsonPropertyName("botEquipmentConfig")]
    public EquipmentFilters? BotEquipmentConfig { get; set; }

    /// <summary>
    ///     Settings from bot.json to adjust how item is generated
    /// </summary>
    [JsonPropertyName("randomisationDetails")]
    public RandomisationDetails? RandomisationDetails { get; set; }

    /// <summary>
    ///     OPTIONAL - Do not generate mods for tpls in this array
    /// </summary>
    [JsonPropertyName("generateModsBlacklist")]
    public HashSet<MongoId>? GenerateModsBlacklist { get; set; }

    [JsonPropertyName("generatingPlayerLevel")]
    public double? GeneratingPlayerLevel { get; set; }
}