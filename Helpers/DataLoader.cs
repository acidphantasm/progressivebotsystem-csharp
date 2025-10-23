using System.Reflection;
using System.Text.Json;
using _progressiveBotSystem.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class DataLoader(
    ModHelper modHelper): IOnLoad
{
    public AmmoTierData Tier0AmmoData { get; set; }
    public AmmoTierData Tier1AmmoData { get; set; }
    public AmmoTierData Tier2AmmoData { get; set; }
    public AmmoTierData Tier3AmmoData { get; set; }
    public AmmoTierData Tier4AmmoData { get; set; }
    public AmmoTierData Tier5AmmoData { get; set; }
    public AmmoTierData Tier6AmmoData { get; set; }
    public AmmoTierData Tier7AmmoData { get; set; }
    public AppearanceTierData Tier0AppearanceData { get; set; }
    public AppearanceTierData Tier1AppearanceData { get; set; }
    public AppearanceTierData Tier2AppearanceData { get; set; }
    public AppearanceTierData Tier3AppearanceData { get; set; }
    public AppearanceTierData Tier4AppearanceData { get; set; }
    public AppearanceTierData Tier5AppearanceData { get; set; }
    public AppearanceTierData Tier6AppearanceData { get; set; }
    public AppearanceTierData Tier7AppearanceData { get; set; }
    public ChancesTierData Tier0ChancesData { get; set; }
    public ChancesTierData Tier1ChancesData { get; set; }
    public ChancesTierData Tier2ChancesData { get; set; }
    public ChancesTierData Tier3ChancesData { get; set; }
    public ChancesTierData Tier4ChancesData { get; set; }
    public ChancesTierData Tier5ChancesData { get; set; }
    public ChancesTierData Tier6ChancesData { get; set; }
    public ChancesTierData Tier7ChancesData { get; set; }
    public EquipmentTierData Tier0EquipmentData { get; set; }
    public EquipmentTierData Tier1EquipmentData { get; set; }
    public EquipmentTierData Tier2EquipmentData { get; set; }
    public EquipmentTierData Tier3EquipmentData { get; set; }
    public EquipmentTierData Tier4EquipmentData { get; set; }
    public EquipmentTierData Tier5EquipmentData { get; set; }
    public EquipmentTierData Tier6EquipmentData { get; set; }
    public EquipmentTierData Tier7EquipmentData { get; set; }
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier0ModsData { get; set; }
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier1ModsData { get; set; }
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier2ModsData { get; set; }
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier3ModsData { get; set; }
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier4ModsData { get; set; }
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier5ModsData { get; set; }
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier6ModsData { get; set; }
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> Tier7ModsData { get; set; }
    
    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        
        AssignJsonData(pathToMod);
        return Task.CompletedTask;
    }

    private void AssignJsonData(string pathToMod)
    {
        Tier0AmmoData = modHelper.GetJsonDataFromFile<AmmoTierData>(pathToMod, "Data/Ammo/Tier0_ammo.json");
        Tier1AmmoData = modHelper.GetJsonDataFromFile<AmmoTierData>(pathToMod, "Data/Ammo/Tier1_ammo.json");
        Tier2AmmoData = modHelper.GetJsonDataFromFile<AmmoTierData>(pathToMod, "Data/Ammo/Tier2_ammo.json");
        Tier3AmmoData = modHelper.GetJsonDataFromFile<AmmoTierData>(pathToMod, "Data/Ammo/Tier3_ammo.json");
        Tier4AmmoData = modHelper.GetJsonDataFromFile<AmmoTierData>(pathToMod, "Data/Ammo/Tier4_ammo.json");
        Tier5AmmoData = modHelper.GetJsonDataFromFile<AmmoTierData>(pathToMod, "Data/Ammo/Tier5_ammo.json");
        Tier6AmmoData = modHelper.GetJsonDataFromFile<AmmoTierData>(pathToMod, "Data/Ammo/Tier6_ammo.json");
        Tier7AmmoData = modHelper.GetJsonDataFromFile<AmmoTierData>(pathToMod, "Data/Ammo/Tier7_ammo.json");
        
        Tier0AppearanceData = modHelper.GetJsonDataFromFile<AppearanceTierData>(pathToMod, "Data/Appearance/Tier0_appearance.json");
        Tier1AppearanceData = modHelper.GetJsonDataFromFile<AppearanceTierData>(pathToMod, "Data/Appearance/Tier1_appearance.json");
        Tier2AppearanceData = modHelper.GetJsonDataFromFile<AppearanceTierData>(pathToMod, "Data/Appearance/Tier2_appearance.json");
        Tier3AppearanceData = modHelper.GetJsonDataFromFile<AppearanceTierData>(pathToMod, "Data/Appearance/Tier3_appearance.json");
        Tier4AppearanceData = modHelper.GetJsonDataFromFile<AppearanceTierData>(pathToMod, "Data/Appearance/Tier4_appearance.json");
        Tier5AppearanceData = modHelper.GetJsonDataFromFile<AppearanceTierData>(pathToMod, "Data/Appearance/Tier5_appearance.json");
        Tier6AppearanceData = modHelper.GetJsonDataFromFile<AppearanceTierData>(pathToMod, "Data/Appearance/Tier6_appearance.json");
        Tier7AppearanceData = modHelper.GetJsonDataFromFile<AppearanceTierData>(pathToMod, "Data/Appearance/Tier7_appearance.json");
        
        Tier0ChancesData = modHelper.GetJsonDataFromFile<ChancesTierData>(pathToMod, "Data/Chances/Tier0_chances.json");
        Tier1ChancesData = modHelper.GetJsonDataFromFile<ChancesTierData>(pathToMod, "Data/Chances/Tier1_chances.json");
        Tier2ChancesData = modHelper.GetJsonDataFromFile<ChancesTierData>(pathToMod, "Data/Chances/Tier2_chances.json");
        Tier3ChancesData = modHelper.GetJsonDataFromFile<ChancesTierData>(pathToMod, "Data/Chances/Tier3_chances.json");
        Tier4ChancesData = modHelper.GetJsonDataFromFile<ChancesTierData>(pathToMod, "Data/Chances/Tier4_chances.json");
        Tier5ChancesData = modHelper.GetJsonDataFromFile<ChancesTierData>(pathToMod, "Data/Chances/Tier5_chances.json");
        Tier6ChancesData = modHelper.GetJsonDataFromFile<ChancesTierData>(pathToMod, "Data/Chances/Tier6_chances.json");
        Tier7ChancesData = modHelper.GetJsonDataFromFile<ChancesTierData>(pathToMod, "Data/Chances/Tier7_chances.json");
        
        Tier0EquipmentData = modHelper.GetJsonDataFromFile<EquipmentTierData>(pathToMod, "Data/Equipment/Tier0_equipment.json");
        Tier1EquipmentData = modHelper.GetJsonDataFromFile<EquipmentTierData>(pathToMod, "Data/Equipment/Tier1_equipment.json");
        Tier2EquipmentData = modHelper.GetJsonDataFromFile<EquipmentTierData>(pathToMod, "Data/Equipment/Tier2_equipment.json");
        Tier3EquipmentData = modHelper.GetJsonDataFromFile<EquipmentTierData>(pathToMod, "Data/Equipment/Tier3_equipment.json");
        Tier4EquipmentData = modHelper.GetJsonDataFromFile<EquipmentTierData>(pathToMod, "Data/Equipment/Tier4_equipment.json");
        Tier5EquipmentData = modHelper.GetJsonDataFromFile<EquipmentTierData>(pathToMod, "Data/Equipment/Tier5_equipment.json");
        Tier6EquipmentData = modHelper.GetJsonDataFromFile<EquipmentTierData>(pathToMod, "Data/Equipment/Tier6_equipment.json");
        Tier7EquipmentData = modHelper.GetJsonDataFromFile<EquipmentTierData>(pathToMod, "Data/Equipment/Tier7_equipment.json");
        
        Tier0ModsData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod, "Data/Mods/Tier0_mods.json");
        Tier1ModsData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod, "Data/Mods/Tier1_mods.json");
        Tier2ModsData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod, "Data/Mods/Tier2_mods.json");
        Tier3ModsData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod, "Data/Mods/Tier3_mods.json");
        Tier4ModsData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod, "Data/Mods/Tier4_mods.json");
        Tier5ModsData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod, "Data/Mods/Tier5_mods.json");
        Tier6ModsData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod, "Data/Mods/Tier6_mods.json");
        Tier7ModsData = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(pathToMod, "Data/Mods/Tier7_mods.json");
    }
}