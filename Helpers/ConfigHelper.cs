using System.Text;
using System.Text.Json;
using _progressiveBotSystem.Models;

namespace _progressiveBotSystem.Helpers;

public static class ConfigHelper
{
    private static readonly Dictionary<string, int> RequiredArrayLengths = new()
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
            ["generalConfig.plateChances.pmcMainPlateChance"] = () => config.GeneralConfig.PlateChances.PmcMainPlateChance = defaults.GeneralConfig.PlateChances.PmcMainPlateChance,
            ["generalConfig.plateChances.pmcSidePlateChance"] = () => config.GeneralConfig.PlateChances.PmcSidePlateChance = defaults.GeneralConfig.PlateChances.PmcSidePlateChance,
            ["generalConfig.plateChances.scavMainPlateChance"] = () => config.GeneralConfig.PlateChances.ScavMainPlateChance = defaults.GeneralConfig.PlateChances.ScavMainPlateChance,
            ["generalConfig.plateChances.scavSidePlateChance"] = () => config.GeneralConfig.PlateChances.ScavSidePlateChance = defaults.GeneralConfig.PlateChances.ScavSidePlateChance,
            ["generalConfig.plateChances.bossMainPlateChance"] = () => config.GeneralConfig.PlateChances.BossMainPlateChance = defaults.GeneralConfig.PlateChances.BossMainPlateChance,
            ["generalConfig.plateChances.bossSidePlateChance"] = () => config.GeneralConfig.PlateChances.BossSidePlateChance = defaults.GeneralConfig.PlateChances.BossSidePlateChance,
            ["generalConfig.plateChances.followerMainPlateChance"] = () => config.GeneralConfig.PlateChances.FollowerMainPlateChance = defaults.GeneralConfig.PlateChances.FollowerMainPlateChance,
            ["generalConfig.plateChances.followerSidePlateChance"] = () => config.GeneralConfig.PlateChances.FollowerSidePlateChance = defaults.GeneralConfig.PlateChances.FollowerSidePlateChance,
            ["generalConfig.plateChances.specialMainPlateChance"] = () => config.GeneralConfig.PlateChances.SpecialMainPlateChance = defaults.GeneralConfig.PlateChances.SpecialMainPlateChance,
            ["generalConfig.plateChances.specialSidePlateChance"] = () => config.GeneralConfig.PlateChances.SpecialSidePlateChance = defaults.GeneralConfig.PlateChances.SpecialSidePlateChance,
        };

        foreach (var kvp in RequiredArrayLengths)
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
        var hasInvalidArrays = RequiredArrayLengths.Any(kvp => diskArrayLengths.TryGetValue(kvp.Key, out var diskLength) && diskLength != kvp.Value);

        if (config != null && hasInvalidArrays)
        {
            RepairArrays(config, diskArrayLengths);
        }

        return hasMissingKeys || hasInvalidArrays;
    }

    /// <summary>
    /// Bruh this is confusing as shit and I re-read this doc like 6 times, but it finally works
    /// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader
    /// </summary>
    /// <param name="json"></param>
    private static (HashSet<string> keyPaths, Dictionary<string, int> arrayLengths) ParseDiskJson(string json)
    {
        var keyPaths = new HashSet<string>();
        var arrayLengths = new Dictionary<string, int>();
        var pathStack = new Stack<string>();
        string? currentProperty = null;
        var arrayCountStack = new Stack<(string path, int count)>();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    currentProperty = reader.GetString()!;
                    var path = pathStack.Count > 0
                        ? $"{string.Join(".", pathStack.Reverse())}.{currentProperty}"
                        : currentProperty;
                    keyPaths.Add(path);
                    break;
                case JsonTokenType.StartObject:
                    if (currentProperty != null)
                    {
                        pathStack.Push(currentProperty);
                        currentProperty = null;
                    }
                    break;
                case JsonTokenType.EndObject:
                    if (pathStack.Count > 0)
                        pathStack.Pop();
                    break;
                case JsonTokenType.StartArray:
                    if (currentProperty != null)
                    {
                        var arrayPath = pathStack.Count > 0
                            ? $"{string.Join(".", pathStack.Reverse())}.{currentProperty}"
                            : currentProperty;
                        if (RequiredArrayLengths.ContainsKey(arrayPath))
                            arrayCountStack.Push((arrayPath, 0));
                        currentProperty = null;
                    }
                    break;
                case JsonTokenType.EndArray:
                    if (arrayCountStack.Count > 0)
                    {
                        var (p, count) = arrayCountStack.Pop();
                        arrayLengths[p] = count;
                    }
                    break;
                case JsonTokenType.String:
                case JsonTokenType.Number:
                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    if (arrayCountStack.Count > 0)
                    {
                        var (p, count) = arrayCountStack.Pop();
                        arrayCountStack.Push((p, count + 1));
                    }
                    break;
            }
        }
        return (keyPaths, arrayLengths);
    }
    
    /// <summary>
    /// Bruh this is confusing as shit and I re-read this doc like 6 times, but it finally works
    /// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader
    /// </summary>
    /// <param name="json"></param>
    private static HashSet<string> ParseDefaultJson(string json)
    {
        var keyPaths = new HashSet<string>();
        var pathStack = new Stack<string>();
        string? currentProperty = null;
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    currentProperty = reader.GetString()!;
                    var path = pathStack.Count > 0
                        ? $"{string.Join(".", pathStack.Reverse())}.{currentProperty}"
                        : currentProperty;
                    keyPaths.Add(path);
                    break;
                case JsonTokenType.StartObject:
                    if (currentProperty != null)
                    {
                        pathStack.Push(currentProperty);
                        currentProperty = null;
                    }
                    break;
                case JsonTokenType.EndObject:
                    if (pathStack.Count > 0)
                        pathStack.Pop();
                    break;
            }
        }
        return keyPaths;
    }
}