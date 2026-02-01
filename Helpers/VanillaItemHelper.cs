using System.Reflection;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace _progressiveBotSystem.Helpers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader)]
public class VanillaItemHelper(
    ModHelper modHelper,
    DatabaseService databaseService,
    ItemHelper itemHelper,
    JsonUtil jsonUtil,
    ApbsLogger apbsLogger): IOnLoad
{
    private readonly Dictionary<MongoId, string> _primaryWeaponMap = new();
    private readonly Dictionary<MongoId, string> _holsterMap = new();
    private readonly Dictionary<MongoId, string> _scabbardMap = new();
    private readonly Dictionary<MongoId, string> _pocketMap = new();
    private readonly Dictionary<MongoId, string> _secureContainerMap = new();
    private readonly Dictionary<MongoId, string> _armbandMap = new();
    private readonly Dictionary<MongoId, string> _armorvestMap = new();
    private readonly Dictionary<MongoId, string> _backpackMap = new();
    private readonly Dictionary<MongoId, string> _earpieceMap = new();
    private readonly Dictionary<MongoId, string> _eyewearMap = new();
    private readonly Dictionary<MongoId, string> _facecoverMap = new();
    private readonly Dictionary<MongoId, string> _headwearMap = new();
    private readonly Dictionary<MongoId, string> _tacticalVestMap = new();
    private readonly Dictionary<MongoId, string> _armouredRigMap = new();
    
    private readonly Dictionary<MongoId, string> _caliber1143X23AcpMap = new();
    private readonly Dictionary<MongoId, string> _caliber127X55Map = new();
    private readonly Dictionary<MongoId, string> _caliber12GMap = new();
    private readonly Dictionary<MongoId, string> _caliber20GMap = new();
    private readonly Dictionary<MongoId, string> _caliber23X75Map = new();
    private readonly Dictionary<MongoId, string> _caliber366TkmMap = new();
    private readonly Dictionary<MongoId, string> _caliber40MmRuMap = new();
    private readonly Dictionary<MongoId, string> _caliber40X46Map = new();
    private readonly Dictionary<MongoId, string> _caliber46X30Map = new();
    private readonly Dictionary<MongoId, string> _caliber545X39Map = new();
    private readonly Dictionary<MongoId, string> _caliber556X45NatoMap = new();
    private readonly Dictionary<MongoId, string> _caliber57X28Map = new();
    private readonly Dictionary<MongoId, string> _caliber68X51Map = new();
    private readonly Dictionary<MongoId, string> _caliber762X25TtMap = new();
    private readonly Dictionary<MongoId, string> _caliber762X35Map = new();
    private readonly Dictionary<MongoId, string> _caliber762X39Map = new();
    private readonly Dictionary<MongoId, string> _caliber762X51Map = new();
    private readonly Dictionary<MongoId, string> _caliber762X54RMap = new();
    private readonly Dictionary<MongoId, string> _caliber86X70Map = new();
    private readonly Dictionary<MongoId, string> _caliber9X18PmMap = new();
    private readonly Dictionary<MongoId, string> _caliber9X19ParaMap = new();
    private readonly Dictionary<MongoId, string> _caliber9X21Map = new();
    private readonly Dictionary<MongoId, string> _caliber9X33RMap = new();
    private readonly Dictionary<MongoId, string> _caliber9X39Map = new();
    private readonly Dictionary<MongoId, string> _caliber127X33Map = new();
    private readonly Dictionary<MongoId, string> _caliber127X99Map = new();
    private readonly Dictionary<MongoId, string> _caliber725Map = new();
    
    private readonly Dictionary<MongoId, string> _grenadesMap = new();
    private readonly Dictionary<MongoId, string> _healingMap = new();
    private readonly Dictionary<MongoId, string> _drugsMap = new();
    private readonly Dictionary<MongoId, string> _foodMap = new();
    private readonly Dictionary<MongoId, string> _drinkMap = new();
    private readonly Dictionary<MongoId, string> _stimMap = new();
    private readonly Dictionary<MongoId, string> _currencyMap = new();
    
    private readonly Dictionary<MongoId, string> _armourPlates = new();
    private readonly Dictionary<MongoId, string> _mods = new();
    
    private readonly Dictionary<MongoId, string> _usecBodyMap = new();
    private readonly Dictionary<MongoId, string> _usecFeetMap = new();
    private readonly Dictionary<MongoId, string> _usecHandsMap = new();
    private readonly Dictionary<MongoId, string> _usecHeadMap = new();
    private readonly Dictionary<MongoId, string> _bearBodyMap = new();
    private readonly Dictionary<MongoId, string> _bearFeetMap = new();
    private readonly Dictionary<MongoId, string> _bearHandsMap = new();
    private readonly Dictionary<MongoId, string> _bearHeadMap = new();
    
    private Dictionary<string, Dictionary<MongoId, string>> CategoryMaps =>
        new()
        {
            { "PrimaryWeapon", _primaryWeaponMap },
            { "Holster", _holsterMap },
            { "Scabbard", _scabbardMap },
            { "Pockets", _pocketMap },
            { "SecuredContainer", _secureContainerMap },
            { "ArmBand", _armbandMap },
            { "ArmorVest", _armorvestMap },
            { "Backpack", _backpackMap },
            { "Earpiece", _earpieceMap },
            { "Eyewear", _eyewearMap },
            { "FaceCover", _facecoverMap },
            { "Headwear", _headwearMap },
            { "TacticalVest", _tacticalVestMap },
            { "ArmouredRig", _armouredRigMap },

            { "Caliber1143x23ACP", _caliber1143X23AcpMap },
            { "Caliber127x55", _caliber127X55Map },
            { "Caliber12g", _caliber12GMap },
            { "Caliber20g", _caliber20GMap },
            { "Caliber23x75", _caliber23X75Map },
            { "Caliber366TKM", _caliber366TkmMap },
            { "Caliber40mmRU", _caliber40MmRuMap },
            { "Caliber40x46", _caliber40X46Map },
            { "Caliber46x30", _caliber46X30Map },
            { "Caliber545x39", _caliber545X39Map },
            { "Caliber556x45NATO", _caliber556X45NatoMap },
            { "Caliber57x28", _caliber57X28Map },
            { "Caliber68x51", _caliber68X51Map },
            { "Caliber762x25TT", _caliber762X25TtMap },
            { "Caliber762x35", _caliber762X35Map },
            { "Caliber762x39", _caliber762X39Map },
            { "Caliber762x51", _caliber762X51Map },
            { "Caliber762x54R", _caliber762X54RMap },
            { "Caliber86x70", _caliber86X70Map },
            { "Caliber9x18PM", _caliber9X18PmMap },
            { "Caliber9x19PARA", _caliber9X19ParaMap },
            { "Caliber9x21", _caliber9X21Map },
            { "Caliber9x33R", _caliber9X33RMap },
            { "Caliber9x39", _caliber9X39Map },
            { "Caliber127x33", _caliber127X33Map },
            { "Caliber127x99", _caliber127X99Map },
            { "Caliber725", _caliber725Map },
            
            { "Grenades", _grenadesMap },
            { "Healing", _healingMap },
            { "Drugs", _drugsMap },
            { "Food", _foodMap },
            { "Drink", _drinkMap },
            { "Stims", _stimMap },
            { "Currency", _currencyMap },
            
            { "ArmourPlates", _armourPlates },
            { "Mods", _mods },
            
            { "UsecBody", _usecBodyMap },
            { "UsecFeet", _usecFeetMap },
            { "UsecHands", _usecHandsMap },
            { "UsecHead", _usecHeadMap },
            { "BearBody", _bearBodyMap },
            { "BearFeet", _bearFeetMap },
            { "BearHands", _bearHandsMap },
            { "BearHead", _bearHeadMap },
        };


    private readonly List<MongoId> _primaryBaseClasses =
    [
        BaseClasses.ASSAULT_CARBINE,
        BaseClasses.ASSAULT_RIFLE,
        BaseClasses.SNIPER_RIFLE,
        BaseClasses.MARKSMAN_RIFLE,
        BaseClasses.GRENADE_LAUNCHER,
        BaseClasses.MACHINE_GUN,
        BaseClasses.ROCKET_LAUNCHER,
        BaseClasses.SHOTGUN,
        BaseClasses.SMG
    ];
    
    private readonly List<MongoId> _holsterBaseClasses =
    [
        BaseClasses.PISTOL,
        BaseClasses.REVOLVER
    ];
    
    public async Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        
        var mappingDir = Path.Combine(pathToMod, "GeneratedVanillaMappings-DO_NOT_TOUCH");
        if (!NeedsRegeneration(mappingDir)) return;

        apbsLogger.Warning("Caching Vanilla Items... Please wait...");
        
        // Run vanilla items first to populate maps
        await LoadVanillaItemsMapping();
        
        // Then run appearance to populate maps
        await LoadVanillaAppearanceMapping();
        
        // Then write all maps to disk & new manifest
        await WriteAllCategoryMapsAsync(mappingDir);
        await WriteManifestAsync(mappingDir);
    }
    
    private bool NeedsRegeneration(string mappingDir)
    {
        var manifestPath = Path.Combine(mappingDir, "manifest.json");

        if (!Directory.Exists(mappingDir)) return true;
        if (!File.Exists(manifestPath)) return true;

        var manifest = jsonUtil.DeserializeFromFile<VanillaMappingManifest>(manifestPath) ?? new VanillaMappingManifest() { ManifestVersion = 0 };

        return manifest.ManifestVersion != ModConfig.CurrentVanillaMappingManifestVersion;
    }

    private Task LoadVanillaItemsMapping()
    {
        var allItems = databaseService.GetItems();

        var mapRules = new Dictionary<Func<MongoId, TemplateItem, bool>, Dictionary<MongoId, string>>
        {
            // Fuck BSG for putting the MTS255 in the revolver base class, and the MP43 in the shotgun baseclass. That makes this look stupid. Along with all the other shit that's special.
            { (id, _) => (itemHelper.IsOfBaseclasses(id, _primaryBaseClasses) && id != ItemTpl.GRENADELAUNCHER_FN40GL_02 && id != ItemTpl.GRENADELAUNCHER_FN40GL_03 && id != ItemTpl.SHOTGUN_MP43_12GA_SAWEDOFF_DOUBLEBARREL && !itemHelper.GetItemName(id).Contains("RSP-30") && !itemHelper.GetItemName(id).Contains("ROP-30") && !itemHelper.GetItemName(id).Contains("ZiD SP") && !itemHelper.GetItemName(id).Contains("NSV Utyos") && !itemHelper.GetItemName(id).Contains("Master Hand")) || id == ItemTpl.REVOLVER_MTS25512_12GA_SHOTGUN, _primaryWeaponMap },
            { (id, _) => itemHelper.IsOfBaseclasses(id, _holsterBaseClasses) && id != ItemTpl.REVOLVER_MTS25512_12GA_SHOTGUN || id == ItemTpl.SHOTGUN_MP43_12GA_SAWEDOFF_DOUBLEBARREL, _holsterMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.VEST) && (item.Properties?.Slots == null || !item.Properties.Slots.Any()), _tacticalVestMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.VEST) && item.Properties?.Slots != null && item.Properties.Slots.Any(), _armouredRigMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.KNIFE) && id != ItemTpl.KNIFE_SUPERFORS_DB_2020_DEAD_BLOW_HAMMER && id != ItemTpl.KNIFE_INFECTIOUS_STRIKE && id != ItemTpl.KNIFE_CHAINED_LABRYS, _scabbardMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.POCKETS), _pocketMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.MOB_CONTAINER), _secureContainerMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.ARM_BAND), _armbandMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.ARMOR), _armorvestMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.BACKPACK), _backpackMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.HEADPHONES), _earpieceMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.VISORS), _eyewearMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.FACE_COVER), _facecoverMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.HEADWEAR), _headwearMap },
            
            // Ammos
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber1143x23ACP", _caliber1143X23AcpMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber127x55", _caliber127X55Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber12g", _caliber12GMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber20g", _caliber20GMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber23x75" && id != ItemTpl.AMMO_23X75_CHER7M, _caliber23X75Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber366TKM", _caliber366TkmMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber40mmRU", _caliber40MmRuMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber40x46" && id != ItemTpl.AMMO_40X46_M716, _caliber40X46Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber46x30", _caliber46X30Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber545x39", _caliber545X39Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber556x45NATO" && id != ItemTpl.AMMO_556X45_6MM_BB, _caliber556X45NatoMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber57x28", _caliber57X28Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber68x51", _caliber68X51Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber762x25TT", _caliber762X25TtMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber762x35", _caliber762X35Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber762x39", _caliber762X39Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber762x51", _caliber762X51Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber762x54R", _caliber762X54RMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber86x70", _caliber86X70Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber9x18PM" && !itemHelper.GetItemName(id).Contains("shrapnel"), _caliber9X18PmMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber9x19PARA", _caliber9X19ParaMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber9x21", _caliber9X21Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber9x33R", _caliber9X33RMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber9x39", _caliber9X39Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber127x33", _caliber127X33Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber127x99", _caliber127X99Map },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.AMMO) && item.Properties?.Caliber == "Caliber725", _caliber725Map },
            
            // Generation stuff
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.THROW_WEAP) && id != ItemTpl.GRENADE_F1_HAND_GRENADE_REDUCED_DELAY, _grenadesMap },
            { (id, item) => item.Properties?.MedUseTime is not null && item.Parent != BaseClasses.STIMULATOR && item.Parent != BaseClasses.DRUGS && id != BaseClasses.MEDS && id != BaseClasses.STIMULATOR && id != ItemTpl.MEDKIT_SANITARS_FIRST_AID_KIT && id != ItemTpl.MEDICAL_SANITAR_KIT, _healingMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.DRUGS) && item.Properties?.MedUseTime is not null, _drugsMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.FOOD), _foodMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.DRINK) && id != ItemTpl.DRINK_BOTTLE_OF_TARKOVSKAYA_VODKA_BAD, _drinkMap },
            { (id, item) => itemHelper.IsOfBaseclass(id, BaseClasses.STIMULATOR) && item.Properties?.MedUseTime is not null, _stimMap },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.MONEY), _currencyMap },
            
            // Attachments
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.ARMOR_PLATE), _armourPlates },
            { (id, _) => itemHelper.IsOfBaseclass(id, BaseClasses.MOD), _mods },
        };

        foreach (var (id, item) in allItems)
        {
            foreach (var rule in mapRules.Where(rule => rule.Key(id, item)))
            {
                rule.Value[id] = itemHelper.GetItemName(id);
            }
        }

        return Task.CompletedTask;
    }
    
    private Task LoadVanillaAppearanceMapping()
    {
        var allItems = databaseService.GetCustomization();

        var mapRules = new Dictionary<Func<MongoId, CustomizationItem, bool>, Dictionary<MongoId, string>>
        {
            { (_, item) => item.Properties.Side.Contains("Usec") && item.Properties.BodyPart == "Body", _usecBodyMap },
            { (_, item) => item.Properties.Side.Contains("Usec") && item.Properties.BodyPart == "Feet", _usecFeetMap },
            { (_, item) => item.Properties.Side.Contains("Usec") && item.Properties.BodyPart == "Hands", _usecHandsMap },
            { (_, item) => item.Properties.Side.Contains("Usec") && item.Properties.BodyPart == "Head", _usecHeadMap },
            { (_, item) => item.Properties.Side.Contains("Bear") && item.Properties.BodyPart == "Body", _bearBodyMap },
            { (_, item) => item.Properties.Side.Contains("Bear") && item.Properties.BodyPart == "Feet", _bearFeetMap },
            { (_, item) => item.Properties.Side.Contains("Bear") && item.Properties.BodyPart == "Hands", _bearHandsMap },
            { (_, item) => item.Properties.Side.Contains("Bear") && item.Properties.BodyPart == "Head", _bearHeadMap },
        };

        foreach (var (id, item) in allItems)
        {
            if (item.Properties.BodyPart is null) continue;
            if (item.Properties.BearTemplateId is not null) continue;
            if (item.Properties.UsecTemplateId is not null) continue;
            var allowedParents = new[]
            {
                "5cc0868e14c02e000c6bea68",
                "5cc0869814c02e000a4cad94",
                "5cc086a314c02e000c6bea69",
                "5cc085e214c02e000c6bea67"
            };

            if (!allowedParents.Contains(item.Parent)) continue;
            
            foreach (var rule in mapRules.Where(rule => rule.Key(id, item)))
            {
                rule.Value[id] = string.IsNullOrEmpty(itemHelper.GetItemName(id)) ? item.Name : itemHelper.GetItemName(id);
            }
        }

        return Task.CompletedTask;
    }
    
    private async Task WriteAllCategoryMapsAsync(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        foreach (var (fileName, map) in CategoryMaps)
        {
            var path = Path.Combine(outputDirectory, $"{fileName}.json");

            var json = jsonUtil.Serialize(map);
            await File.WriteAllTextAsync(path, json);
        }
    }
    
    private async Task WriteManifestAsync(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        
        var manifest = new VanillaMappingManifest
        {
            ManifestVersion = ModConfig.CurrentVanillaMappingManifestVersion,
        };

        var path = Path.Combine(outputDirectory, "manifest.json");

        await File.WriteAllTextAsync(path, jsonUtil.Serialize(manifest));
    }
}