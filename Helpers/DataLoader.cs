using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader + 10)]
public class DataLoader(
    ModHelper modHelper,
    TierInformation tierInformation,
    JsonUtil jsonUtil,
    ApbsLogger apbsLogger): IOnLoad
{
    public required AllTierData AllTierDataClean { get; set; }
    public required AllTierData AllTierDataDirty { get; set; }
    
    private readonly Dictionary<string, string[]> _expectedPresetStructure =
        new()
        {
            { "Ammo", 
                ["Tier1_ammo.json", "Tier2_ammo.json", "Tier3_ammo.json", "Tier4_ammo.json", "Tier5_ammo.json", "Tier6_ammo.json", "Tier7_ammo.json"]
            },
            { "Appearance", 
                ["Tier1_appearance.json", "Tier2_appearance.json", "Tier3_appearance.json", "Tier4_appearance.json", "Tier5_appearance.json", "Tier6_appearance.json", "Tier7_appearance.json"] 
            },
            { "Chances", 
                ["Tier1_chances.json", "Tier2_chances.json", "Tier3_chances.json", "Tier4_chances.json", "Tier5_chances.json", "Tier6_chances.json", "Tier7_chances.json"] 
            },
            { "Equipment", 
                ["Tier1_equipment.json", "Tier2_equipment.json", "Tier3_equipment.json", "Tier4_equipment.json", "Tier5_equipment.json", "Tier6_equipment.json", "Tier7_equipment.json"] 
            },
            { "Mods", 
                ["Tier1_mods.json", "Tier2_mods.json", "Tier3_mods.json", "Tier4_mods.json", "Tier5_mods.json", "Tier6_mods.json", "Tier7_mods.json"] 
            },
        };
    
    public async Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        if (ModConfig.Config.UsePreset) await AssignJsonDataFromPreset(pathToMod);
        else
        {
            await AssignJsonData(pathToMod);
        }
        
        await AssignTierData(pathToMod);
        
        try
        {
            InternalFileValidation(pathToMod);
            apbsLogger.Debug("Database Loaded and hash-verified");
        }
        catch (Exception ex)
        {
            apbsLogger.Error("Data has been tampered with. If you have any issues you did this to yourself. No support.");
        }
        
        AllTierDataDirty = ModConfig.DeepClone(AllTierDataClean);
    }

    private void InternalFileValidation(string pathToMod)
    {
        var dataDir = Path.Combine(pathToMod, "Data");
        var allValid = true;

        foreach (var kvp in JsonFileHashes.Hashes)
        {
            var relativePath = kvp.Key;
            var expectedHash = kvp.Value;

            var fullPath = Path.Combine(dataDir, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!File.Exists(fullPath))
            {
                apbsLogger.Error($"Missing file: {relativePath}");
                allValid = false;
                continue;
            }

            using var sha = SHA256.Create();
            var bytes = File.ReadAllBytes(fullPath);
            var fileHash = Convert.ToHexString(sha.ComputeHash(bytes));

            if (!string.Equals(fileHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                apbsLogger.Error($"Hash mismatch: {relativePath}");
                allValid = false;
            }
        }

        if (!allValid)
            throw new InvalidOperationException();
    }


    public async Task AssignJsonData(string pathToMod)
    {
        var tiers = new Dictionary<int, TierInnerData>();
        var dataRoot = Path.Combine(pathToMod, "Data");

        var tierNumbers = JsonFileHashes.Hashes.Keys
            .Where(k => k.StartsWith("Ammo/Tier", StringComparison.OrdinalIgnoreCase))
            .Select(ExtractTierFromManifestPath)
            .Distinct()
            .OrderBy(t => t);

        foreach (var tier in tierNumbers)
        {
            tiers[tier] = new TierInnerData
            {
                AmmoData = await DeserializeRequired<AmmoTierData>(Path.Combine(dataRoot, "Ammo", $"Tier{tier}_ammo.json")),
                AppearanceData = await DeserializeRequired<AppearanceTierData>(Path.Combine(dataRoot, "Appearance", $"Tier{tier}_appearance.json")),
                ChancesData = await DeserializeRequired<ChancesTierData>(Path.Combine(dataRoot, "Chances", $"Tier{tier}_chances.json")),
                EquipmentData = await DeserializeRequired<EquipmentTierData>(Path.Combine(dataRoot, "Equipment", $"Tier{tier}_equipment.json")),
                ModsData = await DeserializeRequired<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(dataRoot, "Mods", $"Tier{tier}_mods.json"))
            };
        }

        AllTierDataClean = new AllTierData { Tiers = tiers };

        apbsLogger.Success($"Database Loaded");
    }
    
    private static int ExtractTierFromManifestPath(string path)
    {
        var file = Path.GetFileNameWithoutExtension(path);
        var tierPart = file.Split('_')[0];
        return int.Parse(tierPart.Replace("Tier", ""));
    }
    
    private async Task<T> DeserializeRequired<T>(string path)
    {
        return await jsonUtil.DeserializeFromFileAsync<T>(path) ?? throw new InvalidDataException($"Failed to deserialize: {path}");
    }
    
    public async Task AssignJsonDataFromPreset(string pathToMod)
    {
        var fullPathToPreset = Path.Combine(pathToMod, "Presets", $"{ModConfig.Config.PresetName}");
        if (!ValidatePresetFolder(fullPathToPreset, out var errorMessage))
        {
            apbsLogger.Error($"Loading original APBS Database instead...Configured Preset Is Invalid: {ModConfig.Config.PresetName}");
            apbsLogger.Error($"{errorMessage}");
            ModConfig.Config.UsePreset = false;
            ModConfig.Config.PresetName = string.Empty;
            await AssignJsonData(pathToMod);
            return;
        }
        
        var tiers = new Dictionary<int, TierInnerData>();

        var baseDataRoot = Path.Combine(pathToMod, "Data");
        var presetRoot   = fullPathToPreset;

        var tierNumbers = JsonFileHashes.Hashes.Keys
            .Where(k => k.StartsWith("Ammo/Tier", StringComparison.OrdinalIgnoreCase))
            .Select(ExtractTierFromManifestPath)
            .Distinct()
            .OrderBy(t => t);

        foreach (var tier in tierNumbers)
        {
            var root = tier == 0 ? baseDataRoot : presetRoot;

            tiers[tier] = new TierInnerData
            {
                AmmoData = await DeserializeRequired<AmmoTierData>(Path.Combine(root, "Ammo", $"Tier{tier}_ammo.json")),
                AppearanceData = await DeserializeRequired<AppearanceTierData>(Path.Combine(root, "Appearance", $"Tier{tier}_appearance.json")),
                ChancesData = await DeserializeRequired<ChancesTierData>(Path.Combine(root, "Chances", $"Tier{tier}_chances.json")),
                EquipmentData = await DeserializeRequired<EquipmentTierData>(Path.Combine(root, "Equipment", $"Tier{tier}_equipment.json")),
                ModsData = await DeserializeRequired<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(root, "Mods", $"Tier{tier}_mods.json"))
            };
        }

        AllTierDataClean = new AllTierData { Tiers = tiers };

        apbsLogger.Success($"Preset Loaded: {ModConfig.Config.PresetName}");
    }

    private async Task AssignTierData(string pathToMod)
    {
        tierInformation.Tiers = await jsonUtil.DeserializeFromFileAsync<List<TierData>>(pathToMod + "/Data/Tiers/TierData.json") ?? throw new ArgumentNullException();
    }

    private bool ValidatePresetFolder(string fullPathToPresetRoot, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (!Directory.Exists(fullPathToPresetRoot))
        {
            errorMessage = $"Preset Folder Not Found: {fullPathToPresetRoot}";
            return false;
        }

        if (NeedsANewPreset(fullPathToPresetRoot))
        {
            errorMessage = $"Preset is no longer valid. Make a new preset. This is rarely required but it is for you.";
            return false;
        }

        var subFolders = Directory.GetDirectories(fullPathToPresetRoot).Select(Path.GetFileName).ToArray();

        if (!subFolders.OrderBy(f => f).SequenceEqual(_expectedPresetStructure.Keys.OrderBy(f => f)))
        {
            errorMessage = $"Expected folders: [{string.Join(", ", _expectedPresetStructure.Keys)}], but found: [{string.Join(", ", subFolders)}]";
            return false;
        }

        foreach (var kvp in _expectedPresetStructure)
        {
            var folderName = kvp.Key;
            var requiredFiles = kvp.Value;

            var folderPath = Path.Combine(fullPathToPresetRoot, folderName);

            var filesInFolder = Directory.GetFiles(folderPath)
                .Select(Path.GetFileName)
                .ToArray();

            foreach (var requiredFile in requiredFiles)
            {
                if (!filesInFolder.Contains(requiredFile))
                {
                    errorMessage = $"Missing file in '{folderName}': {requiredFile}";
                    return false;
                }
            }

            if (filesInFolder.Length != requiredFiles.Length)
            {
                var extra = filesInFolder.Except(requiredFiles).ToArray();
                errorMessage = extra.Length > 0
                    ? $"Folder '{folderName}' contains extra files: {string.Join(", ", extra)}"
                    : $"Folder '{folderName}' contains fewer files than expected.";
                return false;
            }
        }

        return true;
    }
    
    public async Task<bool> SavePresetChangesToDisk(string pathToMod)
    {
        var fullPathToPreset = Path.Combine(pathToMod, "Presets", ModConfig.Config.PresetName);
        if (!Directory.Exists(fullPathToPreset)) return false;
        if (!ValidatePresetFolder(fullPathToPreset, out var errorMessage))
        {
            apbsLogger.Error($"Loading original APBS Database instead...Configured Preset Is Invalid: {ModConfig.Config.PresetName}");
            apbsLogger.Error($"{errorMessage}");
            ModConfig.Config.UsePreset = false;
            ModConfig.Config.PresetName = string.Empty;
            await AssignJsonData(pathToMod);
            return false;
        }

        async Task SerializeAndWrite<T>(string subfolder, string fileName, T data)
        {
            var folderPath = Path.Combine(fullPathToPreset, subfolder);
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            var json = jsonUtil.Serialize(data);
            await File.WriteAllTextAsync(filePath, json);
        }

        foreach (var kvp in AllTierDataClean.Tiers)
        {
            var tier = kvp.Key;
            var tierData = kvp.Value;

            if (tier == 0) continue;

            await SerializeAndWrite("Equipment", $"Tier{tier}_equipment.json", tierData.EquipmentData);
            await SerializeAndWrite("Ammo",$"Tier{tier}_ammo.json", tierData.AmmoData);
            await SerializeAndWrite("Appearance", $"Tier{tier}_appearance.json", tierData.AppearanceData);
            await SerializeAndWrite("Chances", $"Tier{tier}_chances.json", tierData.ChancesData);
            await SerializeAndWrite("Mods", $"Tier{tier}_mods.json", tierData.ModsData);
        }

        apbsLogger.Warning($"Preset: {ModConfig.Config.PresetName} changes saved to disk.");
        return true;
    }
    
    private bool NeedsANewPreset(string rootPresetFolder)
    {
        var manifestPath = Path.Combine(rootPresetFolder, "manifest.json");
        if (!File.Exists(manifestPath)) return true;

        var manifest = jsonUtil.DeserializeFromFile<PresetManifest>(manifestPath) ?? new PresetManifest() { Version = 0 };
        return manifest.Version != ModConfig.CurrentPresetManifestVersion;
    }
}