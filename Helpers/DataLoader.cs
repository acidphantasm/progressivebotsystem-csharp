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
    public required AmmoTierData Tier0AmmoData { get; set; }
    public required AmmoTierData Tier1AmmoData { get; set; }
    public required AmmoTierData Tier2AmmoData { get; set; }
    public required AmmoTierData Tier3AmmoData { get; set; }
    public required AmmoTierData Tier4AmmoData { get; set; }
    public required AmmoTierData Tier5AmmoData { get; set; }
    public required AmmoTierData Tier6AmmoData { get; set; }
    public required AmmoTierData Tier7AmmoData { get; set; }
    public required AppearanceTierData Tier0AppearanceData { get; set; }
    public required AppearanceTierData Tier1AppearanceData { get; set; }
    public required AppearanceTierData Tier2AppearanceData { get; set; }
    public required AppearanceTierData Tier3AppearanceData { get; set; }
    public required AppearanceTierData Tier4AppearanceData { get; set; }
    public required AppearanceTierData Tier5AppearanceData { get; set; }
    public required AppearanceTierData Tier6AppearanceData { get; set; }
    public required AppearanceTierData Tier7AppearanceData { get; set; }
    public required ChancesTierData Tier0ChancesData { get; set; }
    public required ChancesTierData Tier1ChancesData { get; set; }
    public required ChancesTierData Tier2ChancesData { get; set; }
    public required ChancesTierData Tier3ChancesData { get; set; }
    public required ChancesTierData Tier4ChancesData { get; set; }
    public required ChancesTierData Tier5ChancesData { get; set; }
    public required ChancesTierData Tier6ChancesData { get; set; }
    public required ChancesTierData Tier7ChancesData { get; set; }
    public required EquipmentTierData Tier0EquipmentData { get; set; }
    public required EquipmentTierData Tier1EquipmentData { get; set; }
    public required EquipmentTierData Tier2EquipmentData { get; set; }
    public required EquipmentTierData Tier3EquipmentData { get; set; }
    public required EquipmentTierData Tier4EquipmentData { get; set; }
    public required EquipmentTierData Tier5EquipmentData { get; set; }
    public required EquipmentTierData Tier6EquipmentData { get; set; }
    public required EquipmentTierData Tier7EquipmentData { get; set; }
    public required Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier0ModsData { get; set; }
    public required Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier1ModsData { get; set; }
    public required Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier2ModsData { get; set; }
    public required Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier3ModsData { get; set; }
    public required Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier4ModsData { get; set; }
    public required Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier5ModsData { get; set; }
    public required Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier6ModsData { get; set; }
    public required Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier7ModsData { get; set; }
    
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
        else await AssignJsonData(pathToMod);
        
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
        Tier0AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(pathToMod, "Data", "Ammo", "Tier0_ammo.json")) ?? throw new ArgumentNullException();
        Tier1AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(pathToMod, "Data", "Ammo", "Tier1_ammo.json")) ?? throw new ArgumentNullException();
        Tier2AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(pathToMod, "Data", "Ammo", "Tier2_ammo.json")) ?? throw new ArgumentNullException();
        Tier3AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(pathToMod, "Data", "Ammo", "Tier3_ammo.json")) ?? throw new ArgumentNullException();
        Tier4AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(pathToMod, "Data", "Ammo", "Tier4_ammo.json")) ?? throw new ArgumentNullException();
        Tier5AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(pathToMod, "Data", "Ammo", "Tier5_ammo.json")) ?? throw new ArgumentNullException();
        Tier6AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(pathToMod, "Data", "Ammo", "Tier6_ammo.json")) ?? throw new ArgumentNullException();
        Tier7AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(pathToMod, "Data", "Ammo", "Tier7_ammo.json")) ?? throw new ArgumentNullException();

        Tier0AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(pathToMod, "Data", "Appearance", "Tier0_appearance.json")) ?? throw new ArgumentNullException();
        Tier1AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(pathToMod, "Data", "Appearance", "Tier1_appearance.json")) ?? throw new ArgumentNullException();
        Tier2AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(pathToMod, "Data", "Appearance", "Tier2_appearance.json")) ?? throw new ArgumentNullException();
        Tier3AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(pathToMod, "Data", "Appearance", "Tier3_appearance.json")) ?? throw new ArgumentNullException();
        Tier4AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(pathToMod, "Data", "Appearance", "Tier4_appearance.json")) ?? throw new ArgumentNullException();
        Tier5AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(pathToMod, "Data", "Appearance", "Tier5_appearance.json")) ?? throw new ArgumentNullException();
        Tier6AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(pathToMod, "Data", "Appearance", "Tier6_appearance.json")) ?? throw new ArgumentNullException();
        Tier7AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(pathToMod, "Data", "Appearance", "Tier7_appearance.json")) ?? throw new ArgumentNullException();

        Tier0ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(pathToMod, "Data", "Chances", "Tier0_chances.json")) ?? throw new ArgumentNullException();
        Tier1ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(pathToMod, "Data", "Chances", "Tier1_chances.json")) ?? throw new ArgumentNullException();
        Tier2ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(pathToMod, "Data", "Chances", "Tier2_chances.json")) ?? throw new ArgumentNullException();
        Tier3ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(pathToMod, "Data", "Chances", "Tier3_chances.json")) ?? throw new ArgumentNullException();
        Tier4ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(pathToMod, "Data", "Chances", "Tier4_chances.json")) ?? throw new ArgumentNullException();
        Tier5ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(pathToMod, "Data", "Chances", "Tier5_chances.json")) ?? throw new ArgumentNullException();
        Tier6ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(pathToMod, "Data", "Chances", "Tier6_chances.json")) ?? throw new ArgumentNullException();
        Tier7ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(pathToMod, "Data", "Chances", "Tier7_chances.json")) ?? throw new ArgumentNullException();

        Tier0EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(pathToMod, "Data", "Equipment", "Tier0_equipment.json")) ?? throw new ArgumentNullException();
        Tier1EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(pathToMod, "Data", "Equipment", "Tier1_equipment.json")) ?? throw new ArgumentNullException();
        Tier2EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(pathToMod, "Data", "Equipment", "Tier2_equipment.json")) ?? throw new ArgumentNullException();
        Tier3EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(pathToMod, "Data", "Equipment", "Tier3_equipment.json")) ?? throw new ArgumentNullException();
        Tier4EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(pathToMod, "Data", "Equipment", "Tier4_equipment.json")) ?? throw new ArgumentNullException();
        Tier5EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(pathToMod, "Data", "Equipment", "Tier5_equipment.json")) ?? throw new ArgumentNullException();
        Tier6EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(pathToMod, "Data", "Equipment", "Tier6_equipment.json")) ?? throw new ArgumentNullException();
        Tier7EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(pathToMod, "Data", "Equipment", "Tier7_equipment.json")) ?? throw new ArgumentNullException();

        Tier0ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(pathToMod, "Data", "Mods", "Tier0_mods.json")) ?? throw new ArgumentNullException();
        Tier1ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(pathToMod, "Data", "Mods", "Tier1_mods.json")) ?? throw new ArgumentNullException();
        Tier2ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(pathToMod, "Data", "Mods", "Tier2_mods.json")) ?? throw new ArgumentNullException();
        Tier3ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(pathToMod, "Data", "Mods", "Tier3_mods.json")) ?? throw new ArgumentNullException();
        Tier4ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(pathToMod, "Data", "Mods", "Tier4_mods.json")) ?? throw new ArgumentNullException();
        Tier5ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(pathToMod, "Data", "Mods", "Tier5_mods.json")) ?? throw new ArgumentNullException();
        Tier6ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(pathToMod, "Data", "Mods", "Tier6_mods.json")) ?? throw new ArgumentNullException();
        Tier7ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(pathToMod, "Data", "Mods", "Tier7_mods.json")) ?? throw new ArgumentNullException();
        
        apbsLogger.Success("Database Loaded");
    }
    
    public async Task AssignJsonDataFromPreset(string pathToMod)
    {
        var fullPathToPreset = Path.Combine(pathToMod, "Presets", $"{ModConfig.Config.PresetName}");
        if (!ValidatePresetFolder(fullPathToPreset, out var errorMessage))
        {
            apbsLogger.Error($"Loading original APBS Database instead...Configured Preset Is Invalid: {ModConfig.Config.PresetName}");
            apbsLogger.Error($"{errorMessage}");
            ModConfig.Config.UsePreset = false;
            await AssignJsonData(pathToMod);
            return;
        }
        
        Tier0AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(pathToMod, "Data", "Ammo", "Tier0_ammo.json")) ?? throw new ArgumentNullException();
        Tier1AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(fullPathToPreset, "Ammo", "Tier1_ammo.json")) ?? throw new ArgumentNullException();
        Tier2AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(fullPathToPreset, "Ammo", "Tier2_ammo.json")) ?? throw new ArgumentNullException();
        Tier3AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(fullPathToPreset, "Ammo", "Tier3_ammo.json")) ?? throw new ArgumentNullException();
        Tier4AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(fullPathToPreset, "Ammo", "Tier4_ammo.json")) ?? throw new ArgumentNullException();
        Tier5AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(fullPathToPreset, "Ammo", "Tier5_ammo.json")) ?? throw new ArgumentNullException();
        Tier6AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(fullPathToPreset, "Ammo", "Tier6_ammo.json")) ?? throw new ArgumentNullException();
        Tier7AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(Path.Combine(fullPathToPreset, "Ammo", "Tier7_ammo.json")) ?? throw new ArgumentNullException();
        
        Tier0AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(pathToMod, "Data", "Appearance", "Tier0_appearance.json")) ?? throw new ArgumentNullException();
        Tier1AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(fullPathToPreset, "Appearance", "Tier1_appearance.json")) ?? throw new ArgumentNullException();
        Tier2AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(fullPathToPreset, "Appearance", "Tier2_appearance.json")) ?? throw new ArgumentNullException();
        Tier3AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(fullPathToPreset, "Appearance", "Tier3_appearance.json")) ?? throw new ArgumentNullException();
        Tier4AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(fullPathToPreset, "Appearance", "Tier4_appearance.json")) ?? throw new ArgumentNullException();
        Tier5AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(fullPathToPreset, "Appearance", "Tier5_appearance.json")) ?? throw new ArgumentNullException();
        Tier6AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(fullPathToPreset, "Appearance", "Tier6_appearance.json")) ?? throw new ArgumentNullException();
        Tier7AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(Path.Combine(fullPathToPreset, "Appearance", "Tier7_appearance.json")) ?? throw new ArgumentNullException();

        Tier0ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(pathToMod, "Data", "Chances", "Tier0_chances.json")) ?? throw new ArgumentNullException();
        Tier1ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(fullPathToPreset, "Chances", "Tier1_chances.json")) ?? throw new ArgumentNullException();
        Tier2ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(fullPathToPreset, "Chances", "Tier2_chances.json")) ?? throw new ArgumentNullException();
        Tier3ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(fullPathToPreset, "Chances", "Tier3_chances.json")) ?? throw new ArgumentNullException();
        Tier4ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(fullPathToPreset, "Chances", "Tier4_chances.json")) ?? throw new ArgumentNullException();
        Tier5ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(fullPathToPreset, "Chances", "Tier5_chances.json")) ?? throw new ArgumentNullException();
        Tier6ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(fullPathToPreset, "Chances", "Tier6_chances.json")) ?? throw new ArgumentNullException();
        Tier7ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(Path.Combine(fullPathToPreset, "Chances", "Tier7_chances.json")) ?? throw new ArgumentNullException();

        Tier0EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(pathToMod, "Data", "Equipment", "Tier0_equipment.json")) ?? throw new ArgumentNullException();
        Tier1EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(fullPathToPreset, "Equipment", "Tier1_equipment.json")) ?? throw new ArgumentNullException();
        Tier2EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(fullPathToPreset, "Equipment", "Tier2_equipment.json")) ?? throw new ArgumentNullException();
        Tier3EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(fullPathToPreset, "Equipment", "Tier3_equipment.json")) ?? throw new ArgumentNullException();
        Tier4EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(fullPathToPreset, "Equipment", "Tier4_equipment.json")) ?? throw new ArgumentNullException();
        Tier5EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(fullPathToPreset, "Equipment", "Tier5_equipment.json")) ?? throw new ArgumentNullException();
        Tier6EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(fullPathToPreset, "Equipment", "Tier6_equipment.json")) ?? throw new ArgumentNullException();
        Tier7EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(Path.Combine(fullPathToPreset, "Equipment", "Tier7_equipment.json")) ?? throw new ArgumentNullException();
        
        Tier0ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(pathToMod, "Data", "Mods", "Tier0_mods.json")) ?? throw new ArgumentNullException();
        Tier1ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(fullPathToPreset, "Mods", "Tier1_mods.json")) ?? throw new ArgumentNullException();
        Tier2ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(fullPathToPreset, "Mods", "Tier2_mods.json")) ?? throw new ArgumentNullException();
        Tier3ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(fullPathToPreset, "Mods", "Tier3_mods.json")) ?? throw new ArgumentNullException();
        Tier4ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(fullPathToPreset, "Mods", "Tier4_mods.json")) ?? throw new ArgumentNullException();
        Tier5ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(fullPathToPreset, "Mods", "Tier5_mods.json")) ?? throw new ArgumentNullException();
        Tier6ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(fullPathToPreset, "Mods", "Tier6_mods.json")) ?? throw new ArgumentNullException();
        Tier7ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(Path.Combine(fullPathToPreset, "Mods", "Tier7_mods.json")) ?? throw new ArgumentNullException();
        
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
    
    public async Task SavePresetChangesToDisk(string pathToMod)
    {
        var fullPathToPreset = Path.Combine(pathToMod, "Presets", ModConfig.Config.PresetName);
        if (!Directory.Exists(fullPathToPreset)) return;
        if (!ValidatePresetFolder(fullPathToPreset, out var errorMessage))
        {
            apbsLogger.Error($"Loading original APBS Database instead...Configured Preset Is Invalid: {ModConfig.Config.PresetName}");
            apbsLogger.Error($"{errorMessage}");
            ModConfig.Config.UsePreset = false;
            await AssignJsonData(pathToMod);
            return;
        }

        async Task SerializeAndWrite<T>(string subfolder, string fileName, T data)
        {
            var folderPath = Path.Combine(fullPathToPreset, subfolder);
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter(), new MongoIdDictionaryKeyConverter() } 
            };

            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        // Equipment
        await SerializeAndWrite("Equipment", "Tier1_equipment.json", Tier1EquipmentData);
        await SerializeAndWrite("Equipment", "Tier2_equipment.json", Tier2EquipmentData);
        await SerializeAndWrite("Equipment", "Tier3_equipment.json", Tier3EquipmentData);
        await SerializeAndWrite("Equipment", "Tier4_equipment.json", Tier4EquipmentData);
        await SerializeAndWrite("Equipment", "Tier5_equipment.json", Tier5EquipmentData);
        await SerializeAndWrite("Equipment", "Tier6_equipment.json", Tier6EquipmentData);
        await SerializeAndWrite("Equipment", "Tier7_equipment.json", Tier7EquipmentData);

        // Ammo
        await SerializeAndWrite("Ammo", "Tier1_ammo.json", Tier1AmmoData);
        await SerializeAndWrite("Ammo", "Tier2_ammo.json", Tier2AmmoData);
        await SerializeAndWrite("Ammo", "Tier3_ammo.json", Tier3AmmoData);
        await SerializeAndWrite("Ammo", "Tier4_ammo.json", Tier4AmmoData);
        await SerializeAndWrite("Ammo", "Tier5_ammo.json", Tier5AmmoData);
        await SerializeAndWrite("Ammo", "Tier6_ammo.json", Tier6AmmoData);
        await SerializeAndWrite("Ammo", "Tier7_ammo.json", Tier7AmmoData);

        // Appearance
        await SerializeAndWrite("Appearance", "Tier1_appearance.json", Tier1AppearanceData);
        await SerializeAndWrite("Appearance", "Tier2_appearance.json", Tier2AppearanceData);
        await SerializeAndWrite("Appearance", "Tier3_appearance.json", Tier3AppearanceData);
        await SerializeAndWrite("Appearance", "Tier4_appearance.json", Tier4AppearanceData);
        await SerializeAndWrite("Appearance", "Tier5_appearance.json", Tier5AppearanceData);
        await SerializeAndWrite("Appearance", "Tier6_appearance.json", Tier6AppearanceData);
        await SerializeAndWrite("Appearance", "Tier7_appearance.json", Tier7AppearanceData);

        // Chances
        await SerializeAndWrite("Chances", "Tier1_chances.json", Tier1ChancesData);
        await SerializeAndWrite("Chances", "Tier2_chances.json", Tier2ChancesData);
        await SerializeAndWrite("Chances", "Tier3_chances.json", Tier3ChancesData);
        await SerializeAndWrite("Chances", "Tier4_chances.json", Tier4ChancesData);
        await SerializeAndWrite("Chances", "Tier5_chances.json", Tier5ChancesData);
        await SerializeAndWrite("Chances", "Tier6_chances.json", Tier6ChancesData);
        await SerializeAndWrite("Chances", "Tier7_chances.json", Tier7ChancesData);

        // Mods
        await SerializeAndWrite("Mods", "Tier1_mods.json", Tier1ModsData);
        await SerializeAndWrite("Mods", "Tier2_mods.json", Tier2ModsData);
        await SerializeAndWrite("Mods", "Tier3_mods.json", Tier3ModsData);
        await SerializeAndWrite("Mods", "Tier4_mods.json", Tier4ModsData);
        await SerializeAndWrite("Mods", "Tier5_mods.json", Tier5ModsData);
        await SerializeAndWrite("Mods", "Tier6_mods.json", Tier6ModsData);
        await SerializeAndWrite("Mods", "Tier7_mods.json", Tier7ModsData);
        
        apbsLogger.Warning($"Preset: {ModConfig.Config.PresetName} changes saved to disk.");
    }
}