using System.Text.Json;
using ProgressiveBotSystem.Models;

namespace ProgressiveBotSystem.Helpers;

public static class ConfigHelper
{
    private static readonly Dictionary<string, int> _requiredArrayLengths = new()
    {
        ["generalConfig.muzzleChance"] = 7,
        ["generalConfig.plateChances.pmcMainPlateChance"] = 7,
        ["generalConfig.plateChances.pmcSidePlateChance"] = 7,
        ["generalConfig.plateChances.scavMainPlateChance"] = 7,
        ["generalConfig.plateChances.scavSidePlateChance"] = 7,
        ["generalConfig.plateChances.bossMainPlateChance"] = 7,
        ["generalConfig.plateChances.bossSidePlateChance"] = 7,
        ["generalConfig.plateChances.followerMainPlateChance"] = 7,
        ["generalConfig.plateChances.followerSidePlateChance"] = 7,
        ["generalConfig.plateChances.specialMainPlateChance"] = 7,
        ["generalConfig.plateChances.specialSidePlateChance"] = 7,
    };

    private static void RepairArrays(ApbsServerConfig config, Dictionary<string, int> diskArrayLengths)
    {
        var defaults = new ApbsServerConfig();
        var repairActions = new Dictionary<string, Action>
        {
            ["generalConfig.muzzleChance"] = () => config.GeneralConfig.MuzzleChance = defaults.GeneralConfig.MuzzleChance,
            ["generalConfig.plateChances.pmcMainPlateChance"] = () =>
                config.GeneralConfig.PlateChances.PmcMainPlateChance = defaults.GeneralConfig.PlateChances.PmcMainPlateChance,
            ["generalConfig.plateChances.pmcSidePlateChance"] = () =>
                config.GeneralConfig.PlateChances.PmcSidePlateChance = defaults.GeneralConfig.PlateChances.PmcSidePlateChance,
            ["generalConfig.plateChances.scavMainPlateChance"] = () =>
                config.GeneralConfig.PlateChances.ScavMainPlateChance = defaults.GeneralConfig.PlateChances.ScavMainPlateChance,
            ["generalConfig.plateChances.scavSidePlateChance"] = () =>
                config.GeneralConfig.PlateChances.ScavSidePlateChance = defaults.GeneralConfig.PlateChances.ScavSidePlateChance,
            ["generalConfig.plateChances.bossMainPlateChance"] = () =>
                config.GeneralConfig.PlateChances.BossMainPlateChance = defaults.GeneralConfig.PlateChances.BossMainPlateChance,
            ["generalConfig.plateChances.bossSidePlateChance"] = () =>
                config.GeneralConfig.PlateChances.BossSidePlateChance = defaults.GeneralConfig.PlateChances.BossSidePlateChance,
            ["generalConfig.plateChances.followerMainPlateChance"] = () =>
                config.GeneralConfig.PlateChances.FollowerMainPlateChance = defaults.GeneralConfig.PlateChances.FollowerMainPlateChance,
            ["generalConfig.plateChances.followerSidePlateChance"] = () =>
                config.GeneralConfig.PlateChances.FollowerSidePlateChance = defaults.GeneralConfig.PlateChances.FollowerSidePlateChance,
            ["generalConfig.plateChances.specialMainPlateChance"] = () =>
                config.GeneralConfig.PlateChances.SpecialMainPlateChance = defaults.GeneralConfig.PlateChances.SpecialMainPlateChance,
            ["generalConfig.plateChances.specialSidePlateChance"] = () =>
                config.GeneralConfig.PlateChances.SpecialSidePlateChance = defaults.GeneralConfig.PlateChances.SpecialSidePlateChance,
        };

        foreach (var kvp in _requiredArrayLengths)
        {
            if (diskArrayLengths.TryGetValue(kvp.Key, out var diskLength) && diskLength != kvp.Value)
            {
                repairActions[kvp.Key]();
            }
        }
    }

    public static bool IsJsonOutdated(string rawJson, string rawDefaultJson, ApbsServerConfig? config = null)
    {
        var (diskKeys, diskArrayLengths) = ParseDiskJson(rawJson);
        var defaultKeys = ParseDefaultJson(rawDefaultJson);

        var hasMissingKeys = defaultKeys.Any(k => !diskKeys.Contains(k));
        var hasExtraKeys = diskKeys.Any(k => !defaultKeys.Contains(k));
        var hasInvalidArrays = _requiredArrayLengths.Any(kvp =>
            diskArrayLengths.TryGetValue(kvp.Key, out var diskLength) && diskLength != kvp.Value
        );

        if (config != null && hasInvalidArrays)
        {
            RepairArrays(config, diskArrayLengths);
        }

        return hasMissingKeys || hasInvalidArrays || hasExtraKeys;
    }

    /// <summary>
    ///     Bruh this is confusing as shit and I re-read this doc like 6 times, but it finally works
    ///     https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader
    /// </summary>
    /// <param name="json"></param>
    private static (HashSet<string> keyPaths, Dictionary<string, int> arrayLengths) ParseDiskJson(string json)
    {
        var keyPaths = new HashSet<string>();
        var arrayLengths = new Dictionary<string, int>();
        using var document = JsonDocument.Parse(json);

        ParseJsonElement(document.RootElement, string.Empty, keyPaths, arrayLengths);

        return (keyPaths, arrayLengths);
    }

    private static HashSet<string> ParseDefaultJson(string json)
    {
        var keyPaths = new HashSet<string>();
        using var document = JsonDocument.Parse(json);

        ParseJsonElement(document.RootElement, string.Empty, keyPaths, null);

        return keyPaths;
    }

    private static void ParseJsonElement(JsonElement element, string path, HashSet<string> keyPaths, Dictionary<string, int>? arrayLengths)
    {
        foreach (var property in element.EnumerateObject())
        {
            var propertyPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    ParseJsonElement(property.Value, propertyPath, keyPaths, arrayLengths);
                    break;

                case JsonValueKind.Array:
                    keyPaths.Add(propertyPath);
                    if (arrayLengths != null && _requiredArrayLengths.ContainsKey(propertyPath))
                    {
                        arrayLengths[propertyPath] = property.Value.GetArrayLength();
                    }
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            ParseJsonElement(item, propertyPath, keyPaths, arrayLengths);
                        }
                    }
                    break;

                default:
                    keyPaths.Add(propertyPath);
                    break;
            }
        }
    }
}
