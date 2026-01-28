using System.Collections.Frozen;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Models.Enums;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class ItemImportHelper(
    ApbsLogger apbsLogger,
    JsonUtil jsonUtil,
    ItemHelper itemHelper)
{
    private bool _alreadyRan = false;
    
    private HashSet<MongoId> _vanillaEquipmentLookup = new();
    private HashSet<MongoId> _vanillaAttachmentLookup = new();
    private HashSet<MongoId> _vanillaAmmoLookup = new();
    private HashSet<MongoId> _vanillaClothingLookup = new();
    
    private Dictionary<ApbsEquipmentSlots, HashSet<MongoId>> _vanillaEquipmentSlotDictionary = new();
    private Dictionary<string, HashSet<MongoId>> _vanillaAmmoDictionary = new();
    private Dictionary<string, Dictionary<string, HashSet<MongoId>>> _vanillaClothingBotSlotDictionary = new();
    
    // Because we're looping the database, and holster is super special, we cache that slot once from default inventory instead of repeated lookups
    private HashSet<MongoId>? _holsterAllowedItems;
    private bool _holsterCacheInitialized;
    
    // Custom Base classes
    private static readonly MongoId BaseClassPackNStrapBelt = "6815465859b8c6ff13f94026";
    
    private readonly ApbsEquipmentSlots[] _shortRangeSlots =
    [
        ApbsEquipmentSlots.FirstPrimaryWeapon_ShortRange,
        ApbsEquipmentSlots.SecondPrimaryWeapon_ShortRange,
    ];

    private readonly ApbsEquipmentSlots[] _longRangeSlots =
    [
        ApbsEquipmentSlots.FirstPrimaryWeapon_LongRange,
        ApbsEquipmentSlots.SecondPrimaryWeapon_LongRange,
    ];
    
    private readonly FrozenSet<MongoId> _shortRangeBaseClasses =
    [
        BaseClasses.ASSAULT_RIFLE,
        BaseClasses.GRENADE_LAUNCHER,
        BaseClasses.MACHINE_GUN,
        BaseClasses.ROCKET_LAUNCHER,
        BaseClasses.SHOTGUN,
        BaseClasses.SMG,
    ];
    
    private readonly FrozenSet<MongoId> _longRangeBaseClasses =
    [
        BaseClasses.ASSAULT_CARBINE,
        BaseClasses.SNIPER_RIFLE,
        BaseClasses.MARKSMAN_RIFLE,
    ];
    
    private readonly FrozenSet<MongoId> _holsterBaseClasses =
    [
        BaseClasses.PISTOL,
    ];

    private readonly FrozenSet<MongoId> _allImportableWeaponBaseClasses =
    [
        BaseClasses.ASSAULT_CARBINE,
        BaseClasses.ASSAULT_RIFLE,
        BaseClasses.SNIPER_RIFLE,
        BaseClasses.MARKSMAN_RIFLE,
        BaseClasses.GRENADE_LAUNCHER,
        BaseClasses.MACHINE_GUN,
        BaseClasses.ROCKET_LAUNCHER,
        BaseClasses.SHOTGUN,
        BaseClasses.SMG,
        BaseClasses.KNIFE,
    ];

    private readonly FrozenSet<MongoId> _allImportableEquipmentBaseClasses =
    [
        BaseClasses.ARM_BAND,
        BaseClasses.ARMOR,
        BaseClasses.BACKPACK,
        BaseClasses.HEADPHONES,
        BaseClasses.VISORS,
        BaseClasses.FACE_COVER,
        BaseClasses.HEADWEAR,
        BaseClasses.VEST,
        BaseClassPackNStrapBelt
    ];

    private readonly FrozenSet<MongoId> _bannedItems =
    [
        ItemTpl.ASSAULTRIFLE_MASTER_HAND,
        ItemTpl.MACHINEGUN_NSV_UTYOS_127X108_HEAVY_MACHINE_GUN,
        ItemTpl.SIGNALPISTOL_ZID_SP81_26X75_SIGNAL_PISTOL,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_BLUE,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_FIREWORK,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_GREEN,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_RED,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_SPECIAL_YELLOW,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_YELLOW,
        ItemTpl.FLARE_ROP30_REACTIVE_FLARE_CARTRIDGE_WHITE,
        ItemTpl.GRENADELAUNCHER_FN40GL_02,
        ItemTpl.GRENADELAUNCHER_FN40GL_03,
        ItemTpl.KNIFE_SUPERFORS_DB_2020_DEAD_BLOW_HAMMER,
        ItemTpl.KNIFE_INFECTIOUS_STRIKE,
        ItemTpl.KNIFE_CHAINED_LABRYS,
        ItemTpl.AMMO_23X75_CHER7M,
        ItemTpl.AMMO_40X46_M716,
        ItemTpl.AMMO_556X45_6MM_BB,
    ];
    
    /// <summary>
    ///     Validates any configuration options, corrects them if needed and logs if they're invalid
    /// </summary>
    public void ValidateConfig()
    {
        if (ModConfig.Config.CompatibilityConfig.InitalTierAppearance < 1 || ModConfig.Config.CompatibilityConfig.InitalTierAppearance > 7)
        {
            ModConfig.Config.CompatibilityConfig.InitalTierAppearance = 3;
            apbsLogger.Warning($"Compatibility Config -> InitialTierAppearance is invalid. Defaulting to 3. Fix your config in the WebApp.");
        }
    }
    
    /// <summary>
    ///     Build the vanilla dictionaries that get generated the first time you run the server after installing APBS
    ///     This is the dictionary that contains all vanilla items that live in the same types of data APBS can import
    /// </summary>
    public async Task BuildVanillaDictionaries()
    {
        if (_alreadyRan) return;
        
        _vanillaEquipmentSlotDictionary = await ValidateVanillaEquipmentDatabase();
        _vanillaAmmoDictionary = await ValidateVanillaAmmoDatabase();
        _vanillaAttachmentLookup = await ValidateVanillaAttachmentDatabase();
        
        _vanillaEquipmentLookup = _vanillaEquipmentSlotDictionary
            .SelectMany(x => x.Value)
            .ToHashSet();

        _vanillaAmmoLookup = _vanillaAmmoDictionary
            .SelectMany(x => x.Value)
            .ToHashSet();
        //_vanillaClothingBotSlotDictionary = await ValidateVanillaClothingDatabase();

        _alreadyRan = true;
    }
    
    /// <summary>
    ///     Deserializes all the weapon / equipment JSON's from the VanillaMappings and adds those items to the dictionary
    /// </summary>
    private async Task<Dictionary<ApbsEquipmentSlots, HashSet<MongoId>>> ValidateVanillaEquipmentDatabase()
    {
        var returnDictionary = Enum
            .GetValues<ApbsEquipmentSlots>()
            .ToDictionary(slot => slot, _ => new HashSet<MongoId>());
        
        var primary = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, string>>(Path.Combine(ModConfig._modPath, "GeneratedVanillaMappings-DO_NOT_TOUCH", "PrimaryWeapon.json")) ?? throw new ArgumentNullException();
        foreach (var kvp in primary)
        {
            var itemId = kvp.Key;
            var item = itemHelper.GetItem(itemId);
            if (item.Value is null) continue;

            foreach (var slot in GetSlotsForPrimaryWeapon(itemId))
            {
                returnDictionary[slot].Add(itemId);
            }
        }
        
        await AddEquipmentToSlotAsync(returnDictionary, "Holster.json", ApbsEquipmentSlots.Holster);
        await AddEquipmentToSlotAsync(returnDictionary, "Headwear.json", ApbsEquipmentSlots.Headwear);
        await AddEquipmentToSlotAsync(returnDictionary, "ArmorVest.json", ApbsEquipmentSlots.ArmorVest);
        await AddEquipmentToSlotAsync(returnDictionary, "ArmouredRig.json", ApbsEquipmentSlots.ArmouredRig);
        await AddEquipmentToSlotAsync(returnDictionary, "Backpack.json", ApbsEquipmentSlots.Backpack);
        await AddEquipmentToSlotAsync(returnDictionary, "Earpiece.json", ApbsEquipmentSlots.Earpiece);
        await AddEquipmentToSlotAsync(returnDictionary, "Eyewear.json", ApbsEquipmentSlots.Eyewear);
        await AddEquipmentToSlotAsync(returnDictionary, "FaceCover.json", ApbsEquipmentSlots.FaceCover);
        await AddEquipmentToSlotAsync(returnDictionary, "Scabbard.json", ApbsEquipmentSlots.Scabbard);
        await AddEquipmentToSlotAsync(returnDictionary, "TacticalVest.json", ApbsEquipmentSlots.TacticalVest);
        await AddEquipmentToSlotAsync(returnDictionary, "ArmBand.json", ApbsEquipmentSlots.ArmBand);

        // Debug Slot Logging
        foreach (var (slot, items) in returnDictionary)
        {
            apbsLogger.Debug($"[VANILLA] Equipment slot: {slot.ToString()} contains {items.Count} items");
        }
        return returnDictionary;
    }
    
    /// <summary>
    ///     Adds item to the vanilla dictionary, this is a helper method
    ///     Called from ValidateVanillaEquipmentDatabase
    /// </summary>
    private async Task AddEquipmentToSlotAsync(Dictionary<ApbsEquipmentSlots, HashSet<MongoId>> dictionary, string fileName, ApbsEquipmentSlots slot)
    {
        var items = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, string>>(Path.Combine(ModConfig._modPath, "GeneratedVanillaMappings-DO_NOT_TOUCH", fileName)) ?? throw new ArgumentNullException(fileName);
        foreach (var itemId in items.Keys)
        {
            var item = itemHelper.GetItem(itemId);
            if (item.Value is null) continue;

            dictionary[slot].Add(itemId);
        }
    }

    /// <summary>
    ///     Deserializes all the ammo JSON's from the VanillaMappings and adds those items to the dictionary
    ///     We probably don't need these, but it's very quick to build this dictionary so we might as well
    /// </summary>
    private async Task<Dictionary<string, HashSet<MongoId>>> ValidateVanillaAmmoDatabase()
    {
        var returnDictionary = new Dictionary<string, HashSet<MongoId>>();

        await AddAmmoToCaliberAsync(returnDictionary, "Caliber9x18PM.json", "Caliber9x18PM");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber9x19PARA.json", "Caliber9x19PARA");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber9x21.json", "Caliber9x21");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber9x33R.json", "Caliber9x33R");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber9x39.json", "Caliber9x39");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber12g.json", "Caliber12g");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber20g.json", "Caliber20g");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber23x75.json", "Caliber23x75");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber40mmRU.json", "Caliber40mmRU");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber40x46.json", "Caliber40x46");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber46x30.json", "Caliber46x30");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber57x28.json", "Caliber57x28");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber68x51.json", "Caliber68x51");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber86x70.json", "Caliber86x70");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber127x33.json", "Caliber127x33");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber127x55.json", "Caliber127x55");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber127x99.json", "Caliber127x99");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber366TKM.json", "Caliber366TKM");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber545x39.json", "Caliber545x39");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber556x45NATO.json", "Caliber556x45NATO");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber725.json", "Caliber725");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber762x25TT.json", "Caliber762x25TT");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber762x35.json", "Caliber762x35");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber762x39.json", "Caliber762x39");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber762x51.json", "Caliber762x51");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber762x54R.json", "Caliber762x54R");
        await AddAmmoToCaliberAsync(returnDictionary, "Caliber1143x23ACP.json", "Caliber1143x23ACP");
        
        // Debug Slot Logging
        foreach (var (caliber, items) in returnDictionary)
        {
            apbsLogger.Debug($"[VANILLA] Ammo Caliber: {caliber} contains {items.Count} items");
        }
        return returnDictionary;
    }
    
    /// <summary>
    ///     Adds item to the vanilla dictionary, this is a helper method
    ///     Called from ValidateVanillaAmmoDatabase
    /// </summary>
    private async Task AddAmmoToCaliberAsync(Dictionary<string, HashSet<MongoId>> dictionary, string fileName, string caliber)
    {
        var items = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, string>>(Path.Combine(ModConfig._modPath, "GeneratedVanillaMappings-DO_NOT_TOUCH", fileName)) ?? throw new ArgumentNullException(fileName);
        foreach (var itemId in items.Keys)
        {
            var item = itemHelper.GetItem(itemId);
            if (item.Value is null) continue;
            
            if (!dictionary.TryGetValue(caliber, out var list))
            {
                list = [];
                dictionary[caliber] = [];
            }
            list.Add(itemId);
        }
    }

    /// <summary>
    ///     Deserializes the mods JSON from the VanillaMappings and adds those items to the list
    /// </summary>
    private async Task<HashSet<MongoId>> ValidateVanillaAttachmentDatabase()
    {
        var hashSet = new HashSet<MongoId>();
        
        await AddAttachmentsToModAsync(hashSet, "Mods.json");
        await AddAttachmentsToModAsync(hashSet, "ArmourPlates.json");
        
        // Debug Logging
        apbsLogger.Debug($"[VANILLA] Mods contains {hashSet.Count} items");
        return hashSet;
    }
    
    /// <summary>
    ///     Adds item to the vanilla list, this is a helper method
    ///     Called from ValidateVanillaAttachmentDatabase
    /// </summary>
    private async Task AddAttachmentsToModAsync(HashSet<MongoId> hashSet, string fileName)
    {
        var items = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, string>>(Path.Combine(ModConfig._modPath, "GeneratedVanillaMappings-DO_NOT_TOUCH", fileName)) ?? throw new ArgumentNullException(fileName);
        foreach (var itemId in items.Keys)
        {
            var item = itemHelper.GetItem(itemId);
            if (item.Value is null) continue;
            
            hashSet.Add(itemId);
        }
    }

    /// <summary>
    ///     Deserializes all the clothing JSON's from the VanillaMappings and adds those items to the dictionary
    /// </summary>
    private Task<Dictionary<string, Dictionary<string, List<MongoId>>>> ValidateVanillaClothingDatabase()
    {
        return null;
    }

    /// <summary>
    ///     Return the proper slot types for specific weapon base classes
    /// </summary>
    private IReadOnlyList<ApbsEquipmentSlots> GetSlotsForPrimaryWeapon(MongoId itemId)
    {
        return itemHelper.IsOfBaseclasses(itemId, _shortRangeBaseClasses) ? _shortRangeSlots
            : itemHelper.IsOfBaseclasses(itemId, _longRangeBaseClasses) ? _longRangeSlots
            : [];
    }

    /// <summary>
    ///     Check if the item is banned from import
    ///     Check if the import is even enabled, and if it's importable
    ///     If neither the weapon nor equipment check pass, go ahead and return false so we skip this item for import
    ///     If either of them pass, go ahead and check if the vanilla dictionary contains that item, if it does then skip it
    ///     If all checks are completed, go ahead and mark the item for import
    /// </summary>
    public bool EquipmentNeedsImporting(MongoId itemId)
    {
        if (_bannedItems.Contains(itemId)) return false;
        if (_vanillaEquipmentLookup.Contains(itemId)) return false;

        var isWeapon = ModConfig.Config.CompatibilityConfig.EnableModdedWeapons &&
                        itemHelper.IsOfBaseclasses(itemId, _allImportableWeaponBaseClasses);

        var isEquipment = ModConfig.Config.CompatibilityConfig.EnableModdedEquipment &&
                           itemHelper.IsOfBaseclasses(itemId, _allImportableEquipmentBaseClasses);

        if (!isWeapon && !isEquipment) return false;

        return true;
    }

    /// <summary>
    ///     Check if the item is banned from import
    ///     Check if the import is even enabled, and if it's importable
    ///     If neither the weapon nor equipment check pass, go ahead and return false so we skip this item for import
    ///     If either of them pass, go ahead and check if the vanilla dictionary contains that item, if it does then skip it
    ///     If all checks are completed, go ahead and mark the item for import
    /// </summary>
    public bool AttachmentNeedsImporting(MongoId itemId)
    {
        if (_bannedItems.Contains(itemId)) return false;
        if (_vanillaAttachmentLookup.Contains(itemId)) return false;
        if (!itemHelper.IsOfBaseclass(itemId, BaseClasses.MOD)) return false;

        return true;
    }

    /// <summary>
    ///     Check if the item is banned from import
    ///     Check if the item is ammo
    ///     The Caliber9x18PM caliber is also used for grenade shrapnel, so lets check those and skip if they are shrapnel
    ///     Go ahead and check specific calibers to skip, these are usually used for mounted weapons on the map. We don't spawn these
    ///     If all of these pass, go ahead and check if the vanilla dictionary contains the item, if it does then skip it
    ///     If all checks are completed, go ahead and mark the item for import
    /// </summary>
    public bool AmmoNeedsImporting(MongoId itemId, string caliber)
    {
        if (_bannedItems.Contains(itemId)) return false;
        if (!itemHelper.IsOfBaseclass(itemId, BaseClasses.AMMO)) return false;
        if (caliber == string.Empty) return false;
        
        // Specifically check grenade shrapnel which also happens to always have 9x18PM as the caliber
        if (caliber == "Caliber9x18PM" && itemHelper.GetItemName(itemId).Contains("shrapnel")) return false;
        // Skip these calibers as they are for things we don't spawn on bots (or we already have like the disks)
        if (caliber == "Caliber127x108" || caliber == "Caliber30x29" || caliber == "Caliber26x75" || caliber == "Caliber20x1mm") return false;
        
        if (_vanillaAmmoLookup.Contains(itemId)) return false;

        return true;
    }

    /// <summary>
    ///     Check if the item is banned from import
    ///     Check if the item is ammo
    ///     The Caliber9x18PM caliber is also used for grenade shrapnel, so lets check those and skip if they are shrapnel
    ///     Go ahead and check specific calibers to skip, these are usually used for mounted weapons on the map. We don't spawn these
    ///     If all of these pass, go ahead and check if the vanilla dictionary contains the item, if it does then skip it
    ///     If all checks are completed, go ahead and mark the item for import
    /// </summary>
    public bool AmmoCaliberNeedsAdded(string caliber)
    {
        return !_vanillaAmmoDictionary.ContainsKey(caliber);
    }
    
    /// <summary>
    ///     Get the cartridge ids from a weapon's magazine template that work with the weapon
    /// </summary>
    /// <param name="weaponTemplate">Weapon db template to get magazine cartridges for</param>
    /// <returns>Hashset of cartridge tpls</returns>
    /// <exception cref="ArgumentNullException">Thrown when weaponTemplate is null.</exception>
    public HashSet<MongoId> GetCompatibleCartridgesFromMagazineTemplate(TemplateItem weaponTemplate)
    {
        var magazineSlot = weaponTemplate.Properties?.Slots?.FirstOrDefault(slot => slot.Name == "mod_magazine");
        if (magazineSlot is null)
        {
            return [];
        }

        var magazineTemplate = itemHelper.GetItem(magazineSlot.Properties?.Filters?.FirstOrDefault()?.Filter?.FirstOrDefault() ?? new MongoId(null));
        if (!magazineTemplate.Key)
        {
            return [];
        }

        var cartridges =
            magazineTemplate.Value.Properties.Slots.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter
            ?? magazineTemplate.Value.Properties.Cartridges.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter;

        return cartridges ?? [];
    }

    /// <summary>
    ///     Hydrate the holster cache
    ///     This cache contains all the weapons that can go in the holster, including the Pistol baseclass
    ///     We check it this way for Holster, specifically because there are some revolvers and some shotguns that go here and not in primary classes
    /// </summary>
    private void EnsureHolsterCacheExistsFirst()
    {
        if (_holsterCacheInitialized) return;

        var defaultInventory =
            itemHelper.GetItem(ItemTpl.INVENTORY_DEFAULT).Value;

        _holsterAllowedItems = defaultInventory?
            .Properties?.Slots?.FirstOrDefault(x => x.Name == "Holster")?
            .Properties?.Filters?.FirstOrDefault(f => f.Filter?.Count > 0)?
            .Filter is { } filter
            ? [..filter]
            : null;

        _holsterCacheInitialized = true;
    }
    
    /// <summary>
    ///     Is the item supposed to go in the Holster slot?
    /// </summary>
    public bool IsHolster(MongoId itemId)
    {
        EnsureHolsterCacheExistsFirst();
        
        return _holsterAllowedItems?.Contains(itemId) == true || itemHelper.IsOfBaseclasses(itemId, _holsterBaseClasses);
    }

    /// <summary>
    ///     Is the item supposed to go in the Primary slot?
    /// </summary>
    public bool IsPrimaryWeapon(MongoId itemId)
    {
        return itemHelper.IsOfBaseclasses(itemId, _shortRangeBaseClasses) || itemHelper.IsOfBaseclasses(itemId, _longRangeBaseClasses);
    }

    /// <summary>
    ///     Is the item supposed to go in the PrimaryLongRange slot?
    /// </summary>
    public bool IsLongRangePrimaryWeapon(MongoId itemId)
    {
        return itemHelper.IsOfBaseclasses(itemId, _longRangeBaseClasses);
    }

    /// <summary>
    ///     Is the item supposed to go in the Backpack slot?
    /// </summary>
    public bool IsBackpack(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.BACKPACK);
    }

    /// <summary>
    ///     Is the item supposed to go in the Face cover slot?
    /// </summary>
    public bool IsFacecover(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.FACE_COVER);
    }
    
    /// <summary>
    ///     Is the item supposed to go in the Tactical Rig slot?
    /// </summary>
    public bool IsRigSlot(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.VEST);
    }
    
    /// <summary>
    ///     Is the item supposed to go in the ArmouredRig Data slot?
    /// </summary>
    public bool IsArmouredRig(TemplateItem? itemDetails)
    {
        // Armoured Rigs have Slots, Tactical Rigs do not
        return itemDetails?.Properties?.Slots != null && itemDetails.Properties.Slots.Any();
    }

    /// <summary>
    ///     Is the item supposed to go in the ArmourVest slot?
    /// </summary>
    public bool IsArmourVest(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.ARMOR);
    }

    /// <summary>
    ///     Is the item supposed to go in the Armband slot?
    /// </summary>
    public bool IsArmband(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.ARM_BAND);
    }

    /// <summary>
    ///     Is the item supposed to go in the Headphones slot?
    /// </summary>
    public bool IsHeadphones(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.HEADPHONES);
    }

    /// <summary>
    ///     Is the item supposed to go in the Headwear slot?
    /// </summary>
    public bool IsHeadwear(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.HEADWEAR);
    }

    /// <summary>
    ///     Is the item supposed to go in the Headwear slot and is armoured?
    /// </summary>
    public bool IsArmouredHelmet(TemplateItem itemDetails)
    {
        // Armoured helmets have slots, shit helmets do not
        return itemDetails.Properties?.Slots != null && itemDetails.Properties.Slots.Any();
    }

    /// <summary>
    ///     Is the item supposed to go in the Knife slot?
    /// </summary>
    public bool IsScabbard(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.KNIFE);
    }

    /// <summary>
    ///     Is the item supposed to go in the Eyeglasses slot?
    /// </summary>
    public bool IsEyeglasses(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.VISORS);
    }

    /// <summary>
    ///     Is the item a pack n strap belt?
    /// </summary>
    public bool IsPackNStrapBelt(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClassPackNStrapBelt);
    }

    /// <summary>
    ///     Marks the pack n strap belts as unlootable
    ///     Controlled by the mod config
    /// </summary>
    public void MarkPackNStrapUnlootable(TemplateItem itemDetails)
    {
        if (itemDetails.Properties is null) return;
        
        var unlootableFromSide = new List<PlayerSideMask> { PlayerSideMask.Bear, PlayerSideMask.Usec, PlayerSideMask.Savage };
        itemDetails.Properties.Unlootable = true;
        itemDetails.Properties.UnlootableFromSide = unlootableFromSide;
        itemDetails.Properties.UnlootableFromSlot = "Armband";
    }
    
    /// <summary>
    ///     Check if the item is a primary, if it is, send it to a different helper method
    ///     If it's not a primary weapon, go ahead and return the proper weights for each bot type
    /// </summary>
    public int GetWeaponSlotWeight(ApbsEquipmentSlots slot, string botType)
    {
        if (IsPrimary(slot))
        {
            return GetPrimaryWeaponWeight(botType);
        }
        
        return botType switch
        {
            "pmc" => slot switch
            {
                ApbsEquipmentSlots.Holster  => 5,
                ApbsEquipmentSlots.Scabbard => 6,
                _ => 1
            },

            "scav" => slot switch
            {
                ApbsEquipmentSlots.Holster  => 1,
                ApbsEquipmentSlots.Scabbard => 3,
                _ => 1
            },

            _ => slot switch
            {
                ApbsEquipmentSlots.Holster  => 3,
                ApbsEquipmentSlots.Scabbard => 3,
                _ => 1
            }
        };
    }

    /// <summary>
    ///     Is the slot being checked a primary weapon slot?
    /// </summary>
    private bool IsPrimary(ApbsEquipmentSlots slot)
    {
        return slot is ApbsEquipmentSlots.FirstPrimaryWeapon_LongRange
            or ApbsEquipmentSlots.FirstPrimaryWeapon_ShortRange
            or ApbsEquipmentSlots.SecondPrimaryWeapon_LongRange
            or ApbsEquipmentSlots.SecondPrimaryWeapon_ShortRange;
    }
    
    /// <summary>
    ///     Returns the Mod Config values for the imported weapon weights
    ///     This is a shortcut helper method for GetWeaponSlotWeight so we don't need additional logic there
    /// </summary>
    private int GetPrimaryWeaponWeight(string botType)
    {
        return botType switch
        {
            "pmc" => ModConfig.Config.CompatibilityConfig.PmcWeaponWeights,
            "scav" => ModConfig.Config.CompatibilityConfig.ScavWeaponWeights,
            "default" => ModConfig.Config.CompatibilityConfig.FollowerWeaponWeights,
            _ => 1
        };
    }
    
    private readonly FrozenSet<MongoId> _armourSlots = [BaseClasses.HEADWEAR, BaseClasses.VEST, BaseClasses.ARMOR];
    /// <summary>
    ///     Check if the item has an armour class or armour plate slots
    ///     If it isn't armoured, bail out and don't skip it
    ///     If it is armoured, check if it should be skipped by armour class
    /// </summary>
    public bool IfArmouredHelmetAndShouldSkip(TemplateItem templateItem, int currentTier)
    {
        if (currentTier >= 4) return false;
        if (!itemHelper.IsOfBaseclass(templateItem.Id, BaseClasses.HEADWEAR)) return false;
        
        foreach (var slot in templateItem.Properties?.Slots ?? [])
        {
            foreach (var filter in slot.Properties?.Filters ?? [])
            {
                foreach (var itemId in filter.Filter ?? [])
                {
                    var childArmorClass = itemHelper.GetItem(itemId).Value?.Properties?.ArmorClass ?? 0;
                    if (childArmorClass >= 4) return true;
                }
            }
        }

        return false;
    }
    
    /// <summary>
    ///     Checks various item properties, such as grid length, slot length, etc.
    ///     If the gear is for a scav, go ahead and return 1 to make it less likely
    ///     If the gear isn't for a scav, go ahead and get and return the correct weight for the slot based on the aforementioned properties if needed
    /// </summary>
    public int GetGearSlotWeight(ApbsEquipmentSlots slot, TemplateItem templateItem, bool isScav = false)
    {
        if (isScav) return 1;
        
        var gridLength = templateItem.Properties?.Grids?.Count() ?? 0;
        var equipmentSlotsLength = templateItem.Properties?.Slots?.Count() ?? 0;
        var armorClass = templateItem.Properties?.ArmorClass ?? 0;
        return slot switch
        {
            ApbsEquipmentSlots.ArmBand => 3,
            ApbsEquipmentSlots.ArmorVest => 10,
            ApbsEquipmentSlots.ArmouredRig => 7,
            ApbsEquipmentSlots.Backpack => 5,
            ApbsEquipmentSlots.Eyewear => 1,
            ApbsEquipmentSlots.Earpiece => 5,
            ApbsEquipmentSlots.FaceCover when armorClass > 2 => 1,
            ApbsEquipmentSlots.FaceCover when armorClass > 0 => 2,
            ApbsEquipmentSlots.FaceCover => 4,
            ApbsEquipmentSlots.Headwear when equipmentSlotsLength > 0 => 6,
            ApbsEquipmentSlots.Headwear => 1,
            ApbsEquipmentSlots.TacticalVest when gridLength > 10 => 10,
            ApbsEquipmentSlots.TacticalVest => 1,
            _ => 15
        };
    }

    public bool AreHeadphonesMountable(TemplateItem headphoneTemplateItem)
    {
        return headphoneTemplateItem.Properties?.BlocksEarpiece != null && (bool)headphoneTemplateItem.Properties?.BlocksEarpiece.Value;
    }

    public bool AttachmentNeedsImporting(TemplateItem parentItem, TemplateItem itemToAdd)
    {
        
        if (ModConfig.Config.CompatibilityConfig.EnableSafeGuard)
        {
            if (_vanillaAttachmentLookup.Contains(parentItem.Id) &&
                _vanillaAttachmentLookup.Contains(itemToAdd.Id))
            {
                return false;
            }
        }

        if (!ModConfig.Config.CompatibilityConfig.EnableModdedAttachments)
        {
            if (!_vanillaAttachmentLookup.Contains(itemToAdd.Id) &&
                _vanillaEquipmentLookup.Contains(parentItem.Id))
            {
                return false;
            }
        }
        
        return true;
    }

    public bool AttachmentShouldBeInTier(TemplateItem parentItem, TemplateItem itemToAdd)
    {
        if (ModConfig.Config.CompatibilityConfig.EnableSafeGuard)
        {
            if (_vanillaAttachmentLookup.Contains(parentItem.Id) &&
                _vanillaAttachmentLookup.Contains(itemToAdd.Id))
            {
                apbsLogger.Debug($"[IMPORT][MODS] Skipping. Both attachments are vanilla. Parent: {parentItem.Id} | Child: {itemToAdd.Id}");
                return false;
            }
        }

        if (!ModConfig.Config.CompatibilityConfig.EnableModdedAttachments)
        {
            if (!_vanillaAttachmentLookup.Contains(itemToAdd.Id) &&
                _vanillaEquipmentLookup.Contains(parentItem.Id))
            {
                apbsLogger.Debug($"[IMPORT][MODS] Skipping. Attachment is modded and parent item is vanilla. Parent: {parentItem.Id} | Child: {itemToAdd.Id}");
                return false;
            }
        }
        
        return true;
    }
}