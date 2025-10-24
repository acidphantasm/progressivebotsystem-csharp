using System.Reflection;
using System.Text.Json;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 10)]
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
    
    public async Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        
        await AssignJsonData(pathToMod);
        await AssignTierData(pathToMod);
        
        if (ModConfig.Config.EnableDebugLog) apbsLogger.Debug("Database Loaded");
    }

    private async Task AssignJsonData(string pathToMod)
    {
        Tier0AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(pathToMod + "/Data/Ammo/Tier0_ammo.json") ?? throw new ArgumentNullException();
        Tier1AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(pathToMod + "/Data/Ammo/Tier1_ammo.json") ?? throw new ArgumentNullException();
        Tier2AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(pathToMod + "/Data/Ammo/Tier2_ammo.json") ?? throw new ArgumentNullException();
        Tier3AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(pathToMod + "/Data/Ammo/Tier3_ammo.json") ?? throw new ArgumentNullException();
        Tier4AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(pathToMod + "/Data/Ammo/Tier4_ammo.json") ?? throw new ArgumentNullException();
        Tier5AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(pathToMod + "/Data/Ammo/Tier5_ammo.json") ?? throw new ArgumentNullException();
        Tier6AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(pathToMod + "/Data/Ammo/Tier6_ammo.json") ?? throw new ArgumentNullException();
        Tier7AmmoData = await jsonUtil.DeserializeFromFileAsync<AmmoTierData>(pathToMod + "/Data/Ammo/Tier7_ammo.json") ?? throw new ArgumentNullException();
        
        Tier0AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(pathToMod + "/Data/Appearance/Tier0_appearance.json") ?? throw new ArgumentNullException();
        Tier1AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(pathToMod + "/Data/Appearance/Tier1_appearance.json") ?? throw new ArgumentNullException();
        Tier2AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(pathToMod + "/Data/Appearance/Tier2_appearance.json") ?? throw new ArgumentNullException();
        Tier3AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(pathToMod + "/Data/Appearance/Tier3_appearance.json") ?? throw new ArgumentNullException();
        Tier4AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(pathToMod + "/Data/Appearance/Tier4_appearance.json") ?? throw new ArgumentNullException();
        Tier5AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(pathToMod + "/Data/Appearance/Tier5_appearance.json") ?? throw new ArgumentNullException();
        Tier6AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(pathToMod + "/Data/Appearance/Tier6_appearance.json") ?? throw new ArgumentNullException();
        Tier7AppearanceData = await jsonUtil.DeserializeFromFileAsync<AppearanceTierData>(pathToMod + "/Data/Appearance/Tier7_appearance.json") ?? throw new ArgumentNullException();
        
        Tier0ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(pathToMod + "/Data/Chances/Tier0_chances.json") ?? throw new ArgumentNullException();
        Tier1ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(pathToMod + "/Data/Chances/Tier1_chances.json") ?? throw new ArgumentNullException();
        Tier2ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(pathToMod + "/Data/Chances/Tier2_chances.json") ?? throw new ArgumentNullException();
        Tier3ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(pathToMod + "/Data/Chances/Tier3_chances.json") ?? throw new ArgumentNullException();
        Tier4ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(pathToMod + "/Data/Chances/Tier4_chances.json") ?? throw new ArgumentNullException();
        Tier5ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(pathToMod + "/Data/Chances/Tier5_chances.json") ?? throw new ArgumentNullException();
        Tier6ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(pathToMod + "/Data/Chances/Tier6_chances.json") ?? throw new ArgumentNullException();
        Tier7ChancesData = await jsonUtil.DeserializeFromFileAsync<ChancesTierData>(pathToMod + "/Data/Chances/Tier7_chances.json") ?? throw new ArgumentNullException();
        
        Tier0EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(pathToMod + "/Data/Equipment/Tier0_equipment.json") ?? throw new ArgumentNullException();
        Tier1EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(pathToMod + "/Data/Equipment/Tier1_equipment.json") ?? throw new ArgumentNullException();
        Tier2EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(pathToMod + "/Data/Equipment/Tier2_equipment.json") ?? throw new ArgumentNullException();
        Tier3EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(pathToMod + "/Data/Equipment/Tier3_equipment.json") ?? throw new ArgumentNullException();
        Tier4EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(pathToMod + "/Data/Equipment/Tier4_equipment.json") ?? throw new ArgumentNullException();
        Tier5EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(pathToMod + "/Data/Equipment/Tier5_equipment.json") ?? throw new ArgumentNullException();
        Tier6EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(pathToMod + "/Data/Equipment/Tier6_equipment.json") ?? throw new ArgumentNullException();
        Tier7EquipmentData = await jsonUtil.DeserializeFromFileAsync<EquipmentTierData>(pathToMod + "/Data/Equipment/Tier7_equipment.json") ?? throw new ArgumentNullException();
        
        Tier0ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod + "/Data/Mods/Tier0_mods.json") ?? throw new ArgumentNullException();
        Tier1ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod + "/Data/Mods/Tier1_mods.json") ?? throw new ArgumentNullException();
        Tier2ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod + "/Data/Mods/Tier2_mods.json") ?? throw new ArgumentNullException();
        Tier3ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod + "/Data/Mods/Tier3_mods.json") ?? throw new ArgumentNullException();
        Tier4ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod + "/Data/Mods/Tier4_mods.json") ?? throw new ArgumentNullException();
        Tier5ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod + "/Data/Mods/Tier5_mods.json") ?? throw new ArgumentNullException();
        Tier6ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod + "/Data/Mods/Tier6_mods.json") ?? throw new ArgumentNullException();
        Tier7ModsData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod + "/Data/Mods/Tier7_mods.json") ?? throw new ArgumentNullException();
    }

    private async Task AssignTierData(string pathToMod)
    {
        tierInformation.Tiers = await jsonUtil.DeserializeFromFileAsync<List<TierData>>(pathToMod + "/Data/Tiers/TierData.json") ?? throw new ArgumentNullException();
    }
}