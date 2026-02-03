using System.Collections.Concurrent;
using System.Diagnostics;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Models.Enums;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace _progressiveBotSystem.Services;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostSptModLoader + 69)]
public class ItemImportService(
    ApbsLogger apbsLogger,
    ItemImportHelper itemImportHelper,
    ItemImportTierHelper itemImportTierHelper,
    DatabaseService databaseService,
    ItemHelper itemHelper): IOnLoad
{
    
    private readonly ConcurrentDictionary<(MongoId ParentId, string Slot, MongoId ChildId, int Tier), byte> _processedModCombos = new();
    private readonly ConcurrentDictionary<(MongoId ParentId, string Slot, MongoId ChildId, int Tier), byte> _processedVanillaWeaponModCombos = new();

    private readonly ConcurrentDictionary<MongoId, byte> _uniqueWeapons = new();
    private int _weaponCounter = 0;
    private readonly ConcurrentDictionary<MongoId, byte> _uniqueWeaponAttachments = new();
    private int _weaponAttachmentCounter = 0;
    private readonly ConcurrentDictionary<string, byte> _uniqueCalibers = new();
    private int _caliberCounter = 0;
    
    private readonly ConcurrentDictionary<MongoId, byte> _uniqueVanillaWeaponModAttachment = new();
    private int _vanillaWeaponModAttachmentCounter = 0;
    
    private readonly ConcurrentDictionary<MongoId, byte> _uniqueEquipment = new();
    private int _equipmentCounter = 0;
    private readonly ConcurrentDictionary<MongoId, byte> _uniqueEquipmentAttachments = new();
    private int _equipmentAttachmentCounter = 0;

    private int _bearClothingCounter = 0;
    private int _usecClothingCounter = 0;
    
    private readonly ConcurrentDictionary<MongoId, byte> _mountedHeadphones = new();
    
    private readonly Lock _equipmentLock = new();
    private readonly Lock _modsLock = new();
    private readonly Lock _ammoLock = new();
    
    public async Task OnLoad()
    {
        if (itemImportHelper.ShouldSkipImport())
            return;
        
        var stopwatch = Stopwatch.StartNew();
        
        itemImportHelper.ValidateConfig();
        await itemImportHelper.BuildVanillaDictionaries();
        
        ImportEquipmentBySlot();
        
        stopwatch.Stop();
        apbsLogger.Success($"[IMPORT] Completed in {stopwatch.ElapsedMilliseconds} ms");
        _caliberCounter = LogAndClear("calibers", _caliberCounter, _uniqueCalibers);
        _weaponCounter = LogAndClear("weapons", _weaponCounter, _uniqueWeapons);
        _weaponAttachmentCounter = LogAndClear("unique weapon attachments", _weaponAttachmentCounter, _uniqueWeaponAttachments);
        _vanillaWeaponModAttachmentCounter = LogAndClear("unique weapon attachments added to vanilla items", _vanillaWeaponModAttachmentCounter, _uniqueVanillaWeaponModAttachment);
        _equipmentCounter = LogAndClear("equipment items", _equipmentCounter, _uniqueEquipmentAttachments);
        _equipmentAttachmentCounter = LogAndClear("unique equipment attachments", _equipmentAttachmentCounter, _uniqueEquipmentAttachments);
        _bearClothingCounter = LogAndClear("Bear bodies and legs", _bearClothingCounter);
        _usecClothingCounter = LogAndClear("Usec bodies and legs", _usecClothingCounter);
        _processedModCombos.Clear();
        _processedVanillaWeaponModCombos.Clear();

        /*
        // Debug Validation logging
        var parentId = new MongoId("6895bb82c4519957df062f82");
        var maxTier = 7;

        for (var tier = 1; tier <= maxTier; tier++)
        {
            var modsData = itemImportTierHelper.GetModsTierData(tier);
            var equipmentData = itemImportTierHelper.GetEquipmentTierData(tier);

            // Log modsData
            if (modsData.TryGetValue(parentId, out var knownItemData))
            {
                apbsLogger.Warning($"[TEST][Tier{tier}] Parent {parentId}: {knownItemData.Count} slots");

                foreach (var slot in knownItemData)
                {
                    var slotName = slot.Key;
                    var attachments = slot.Value;
                    apbsLogger.Warning($"[TEST][Tier{tier}] Slot '{slotName}': {attachments.Count} attachments");
                }
            }
            else
            {
                apbsLogger.Warning($"[TEST][Tier{tier}] Parent {parentId} not found in modsData");
            }

            // Log equipmentData
            var scavEquip = equipmentData.Scav.Equipment;
            bool foundInAnySlot = false;

            foreach (var slotKey in scavEquip.Keys)
            {
                if (scavEquip[slotKey].ContainsKey(parentId))
                {
                    apbsLogger.Warning($"[TEST][Tier{tier}] Scav has {parentId} in slot '{slotKey}'");
                    foundInAnySlot = true;
                }
            }

            if (!foundInAnySlot)
            {
                apbsLogger.Warning($"[TEST][Tier{tier}] Parent {parentId} not found in any Scav equipment slot");
            }
        }
        */
    }

    /// <summary>
    ///     Fancy helper methods to log the import counts and then reset the variables to 0
    /// </summary>
    private int LogAndClear(string name, int counter)
    {
        if (counter != 0) apbsLogger.Success($"[IMPORT] Imported {counter} {name}.");
        return 0;
    }
    private int LogAndClear<TKey, TValue>(string name, int counter, ConcurrentDictionary<TKey, TValue> dictToClear) where TKey : notnull
    {
        if (counter != 0) apbsLogger.Success($"[IMPORT] Imported {counter} {name}.");
        dictToClear.Clear();
        return 0;
    }
    
    /// <summary>
    ///     Start of the import process, all you're doing is validating if the item should be imported
    ///     If it should be imported, it's passed over to the sorting import process
    /// </summary>
    private void ImportEquipmentBySlot()
    {
        var allItems = databaseService.GetItems();
        var itemsToImport = allItems.Values
            .Where(item => itemImportHelper.EquipmentNeedsImporting(item.Id))
            .ToList();
        
        Parallel.ForEach(itemsToImport, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, SortAndStartEquipmentImport);
        
        var customizationItems = databaseService.GetCustomization();
        var customizationToImport = customizationItems.Values
            .Where(itemImportHelper.CustomizationNeedsImporting)
            .ToList();
        
        foreach (var item in customizationToImport)
        {
            SortAndStartCustomizationImport(item);
        }
    }
    
    /// <summary>
    ///     The actual start of the equipment import process
    ///     This method is big, ugly, but very explicit in the order of operations
    ///     If you come here and try to change this without understanding WHY this is ordered the way it is, and is massive - you might just break the entire thing
    /// </summary>
    private void SortAndStartEquipmentImport(TemplateItem templateItem)
    {
        var itemId = templateItem.Id;
        if (itemImportHelper.IsHolster(itemId))
        {
            AddWeaponToBotData(ApbsEquipmentSlots.Holster, templateItem);
            return;
        }
        
        if (itemImportHelper.IsPrimaryWeapon(itemId))
        {
            if (itemImportHelper.IsLongRangePrimaryWeapon(itemId))
            {
                AddWeaponToBotData(ApbsEquipmentSlots.FirstPrimaryWeapon_LongRange, templateItem);
                AddWeaponToBotData(ApbsEquipmentSlots.SecondPrimaryWeapon_LongRange, templateItem);
                return;
            }
            AddWeaponToBotData(ApbsEquipmentSlots.FirstPrimaryWeapon_ShortRange, templateItem);
            AddWeaponToBotData(ApbsEquipmentSlots.SecondPrimaryWeapon_ShortRange, templateItem);
            return;
        }
        
        if (itemImportHelper.IsScabbard(itemId))
        {
            AddWeaponToBotData(ApbsEquipmentSlots.Scabbard, templateItem);
            return;
        }
        
        if (itemImportHelper.IsHeadwear(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.Headwear, templateItem);
            return;
        }
        
        if (itemImportHelper.IsRigSlot(itemId))
        {
            if (itemImportHelper.IsArmouredRig(templateItem))
            {
                AddEquipmentToBotData(ApbsEquipmentSlots.ArmouredRig, templateItem);
                return;
            }

            AddEquipmentToBotData(ApbsEquipmentSlots.TacticalVest, templateItem);
            return;
        }
        
        if (itemImportHelper.IsArmourVest(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.ArmorVest, templateItem);
            return;
        }
        
        if (itemImportHelper.IsBackpack(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.Backpack, templateItem);
            return;
        }
        
        if (itemImportHelper.IsFacecover(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.FaceCover, templateItem);
            return;
        }
        
        if (itemImportHelper.IsPackNStrapBelt(itemId))
        {
            if (ModConfig.Config.CompatibilityConfig.PackNStrapUnlootablePmcArmbandBelts)
            {
                itemImportHelper.MarkPackNStrapUnlootable(templateItem);
            }
            AddEquipmentToBotData(ApbsEquipmentSlots.ArmBand, templateItem);
            return;
        }
        
        if (itemImportHelper.IsArmband(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.ArmBand, templateItem);
            return;
        }
        
        if (itemImportHelper.IsHeadphones(itemId))
        {
            if (_mountedHeadphones.ContainsKey(itemId)) return;
            if (itemImportHelper.AreHeadphonesMountable(templateItem)) return;
            
            AddEquipmentToBotData(ApbsEquipmentSlots.Earpiece, templateItem);
            return;
        }
        
        if (itemImportHelper.IsEyeglasses(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.Eyewear, templateItem);
            return;
        }
        
        // Log if something managed to make it this far and not get sorted for import.
        apbsLogger.Error($"[IMPORT][EQUIP][FAIL] No Classification Handling. Report This. ItemId: {itemId} | Name: {itemHelper.GetItemName(itemId)}");
    }

    private void SortAndStartCustomizationImport(CustomizationItem templateItem)
    {
        if (templateItem.Properties.Side.Contains("Bear"))
            _bearClothingCounter++;
        if (templateItem.Properties.Side.Contains("Usec"))
            _usecClothingCounter++;
        
        var startTier = Math.Clamp(ModConfig.Config.CompatibilityConfig.InitalTierAppearance, 1, 7);
        for (var tier = startTier; tier <= 7; tier++)
        {
            var clothingData = itemImportTierHelper.GetAppearanceTierData(tier);
            
            if (templateItem.Properties.Side.Contains("Bear"))
            {
                switch (templateItem.Properties.BodyPart)
                {
                    case "Feet":
                        clothingData.PmcBear["appearance"].Feet[templateItem.Id] = 1;
                        break;
                    case "Body":
                        clothingData.PmcBear["appearance"].Body[templateItem.Id] = 1;
                        break;
                }
            }
            if (templateItem.Properties.Side.Contains("Usec"))
            {
                switch (templateItem.Properties.BodyPart)
                {
                    case "Feet":
                        clothingData.PmcUsec["appearance"].Feet[templateItem.Id] = 1;
                        break;
                    case "Body":
                        clothingData.PmcUsec["appearance"].Body[templateItem.Id] = 1;
                        break;
                }
            }
        }
    }

    /// <summary>
    ///     Add Weapon to the actual bot data, will use helper methods to get the proper weight for the slot
    ///     After importing to bot data, will kick off checking for children item filters and importing those to the proper bot data locations as well
    /// </summary>
    private void AddWeaponToBotData(ApbsEquipmentSlots slot, TemplateItem templateItem)
    {
        var startTier = Math.Clamp(ModConfig.Config.CompatibilityConfig.InitalTierAppearance, 1, 7);
        var weaponSlotsLength = templateItem.Properties?.Slots?.Count() ?? 0;
        var ammoCaliber = templateItem.Properties?.AmmoCaliber ?? string.Empty;
        var weaponIsVanilla = itemImportHelper.WeaponOrEquipmentIsVanilla(templateItem.Id);
        
        for (var tier = startTier; tier <= 7; tier++)
        {
            if (itemImportHelper.IsBlacklistedViaModConfig(templateItem.Id, tier))
            {
                apbsLogger.Debug($"[IMPORT] {templateItem.Id}: Blacklisted Via Mod Config in Tier{tier}");
                continue;
            }
            
            if (weaponIsVanilla)
            {
                var context = new ImportContext { RootItemId = templateItem.Id };
                StartVanillaWeaponModAttachmentImport(templateItem, tier, context);
                continue;
            }
            
            var equipmentData = itemImportTierHelper.GetEquipmentTierData(tier);
            lock (_equipmentLock)
            {
                equipmentData.PmcUsec.Equipment[slot][templateItem.Id] = itemImportHelper.GetWeaponSlotWeight(slot, "pmc");
                equipmentData.PmcBear.Equipment[slot][templateItem.Id] = itemImportHelper.GetWeaponSlotWeight(slot, "pmc");
                equipmentData.Scav.Equipment[slot][templateItem.Id] = itemImportHelper.GetWeaponSlotWeight(slot, "scav");
                equipmentData.Default.Equipment[slot][templateItem.Id] = itemImportHelper.GetWeaponSlotWeight(slot, "default");
            }
            
            if (_uniqueWeapons.TryAdd(templateItem.Id, 0))
                Interlocked.Increment(ref _weaponCounter);

            if (string.IsNullOrEmpty(ammoCaliber) || !itemImportHelper.AmmoCaliberNeedsAdded(ammoCaliber)) continue;
            
            var chambers = templateItem.Properties?.Chambers ?? [];
            var filter = chambers
                .SelectMany(c => c?.Properties?.Filters ?? [])
                .Select(f => f.Filter)
                .FirstOrDefault(f => f != null);

            var ammoIds = filter ?? itemImportHelper.GetCompatibleCartridgesFromMagazineTemplate(templateItem);

            foreach (var ammoId in ammoIds)
            {
                if (!itemImportHelper.AmmoNeedsImporting(ammoId, ammoCaliber)) 
                    continue;

                AddAmmoToBotData(ammoId, ammoCaliber, tier);
                if (!_uniqueCalibers.TryAdd(ammoCaliber, 0)) continue;
                
                Interlocked.Increment(ref _caliberCounter);
                apbsLogger.Debug($"[T{tier}] Adding AmmoCaliber: {ammoCaliber} and {ammoIds.Count} ammunition types.");
            }
        }
        
        if (!weaponIsVanilla && weaponSlotsLength > 0)
        {
            var context = new ImportContext { RootItemId = templateItem.Id };
            context.Ancestors.Add(context.RootItemId);
            for (var tier = 1; tier <= 7; tier++)
            {
                StartEquipmentFilterItemImport(templateItem, context, true, tier);
            }
        }

        if (_uniqueWeapons.ContainsKey(templateItem.Id))
        {
            apbsLogger.Debug($"[{slot}] Completed mod import: {templateItem.Id}");
        }
    }

    private void StartVanillaWeaponModAttachmentImport(TemplateItem parentItem, int tier, ImportContext context)
    {
        var weaponSlots = parentItem.Properties?.Slots?.ToList();
        if (weaponSlots is null || weaponSlots.Count == 0)
            return;

        context.RecursiveCalls++;
        context.CurrentDepth++;
        context.MaxDepth = Math.Max(context.MaxDepth, context.CurrentDepth);
        
        try
        {
            foreach (var slot in weaponSlots)
            {
                var slotName = slot.Name;
                if (slotName is null) 
                    continue;

                var filters = slot.Properties?.Filters?
                    .FirstOrDefault(x => x.Filter is { Count: > 0 })?
                    .Filter;
                if (filters is null) 
                    continue;
                
                if (filters.Contains(ItemTpl.MOUNT_NCSTAR_MPR45_BACKUP) && ModConfig.Config.CompatibilityConfig.EnableMprSafeGuard)
                    filters.ExceptWith(filters.Where(itemId => itemId != ItemTpl.MOUNT_NCSTAR_MPR45_BACKUP).ToList());
                
                foreach (var childItemId in filters)
                {
                    if (!context.Ancestors.Add(childItemId))
                    {
                        var stackStr = string.Join(" -> ", context.ParentStack.Select(x => $"{x.ItemId}({x.SlotName})"));
                        apbsLogger.Error($"[IMPORT] Detected recursive loop! Root: {context.RootItemId} | Full path: {stackStr} -> {childItemId} (slot '{slotName}')");
                        continue;
                    }

                    context.ParentStack.Push((childItemId, slotName));

                    try
                    {
                        var childItem = itemHelper.GetItem(childItemId).Value;
                        if (childItem is null)
                            continue;

                        if (itemImportHelper.IsVanillaAttachment(childItem.Id))
                            continue;

                        var comboKey = (ParentId: parentItem.Id, Slot: slotName, ChildId: childItem.Id, Tier: tier);
                        if (!_processedVanillaWeaponModCombos.TryAdd(comboKey, 0))
                            continue;

                        if (AddModsToBotData(parentItem, childItem, slotName, weaponImport: true, tier, context, true))
                        {
                            if (_uniqueVanillaWeaponModAttachment.TryAdd(childItem.Id, 0))
                                Interlocked.Increment(ref _vanillaWeaponModAttachmentCounter);

                            if (!ModConfig.Config.CompatibilityConfig.EnableModdedWeapons)
                            {
                                StartVanillaWeaponModAttachmentImport(childItem, tier, context);
                            }
                        }
                    }
                    finally
                    {
                        context.ParentStack.Pop();
                        context.Ancestors.Remove(childItemId);
                    }
                }
            }
        }
        finally
        {
            context.CurrentDepth--;
        }
    }

    private void AddAmmoToBotData(MongoId itemId, string caliber, int tier)
    {
        var ammoData = itemImportTierHelper.GetAmmoTierData(tier);
        lock (_ammoLock)
        {
            AddAmmo(ammoData.ScavAmmo, caliber, itemId);
            AddAmmo(ammoData.PmcAmmo, caliber, itemId);
            AddAmmo(ammoData.BossAmmo, caliber, itemId);
        }
    }

    private static void AddAmmo(Dictionary<string, Dictionary<MongoId, double>> dictionary, string caliber, MongoId itemId)
    {
        if (!dictionary.TryGetValue(caliber, out var ammoDict))
        {
            ammoDict = new Dictionary<MongoId, double>();
            dictionary[caliber] = ammoDict;
        }

        ammoDict[itemId] = 1;
    }
    
    /// <summary>
    ///     Add Equipment to the actual bot data, will use helper methods to get the proper weight for the slot
    ///     After importing to bot data, will kick off checking for children item filters and importing those to the proper bot data locations as well
    /// </summary>
    private void AddEquipmentToBotData(ApbsEquipmentSlots slot, TemplateItem templateItem)
    {
        var startTier = Math.Clamp(ModConfig.Config.CompatibilityConfig.InitalTierAppearance, 1, 7);
        var equipmentSlotsLength = templateItem.Properties?.Slots?.Count() ?? 0;
        
        for (var tier = startTier; tier <= 7; tier++)
        {
            if (itemImportHelper.IsBlacklistedViaModConfig(templateItem.Id, tier))
            {
                apbsLogger.Debug($"[IMPORT] {templateItem.Id}: Blacklisted Via Mod Config in Tier{tier}");
                continue;
            }
            if (itemImportHelper.IfArmouredHelmetAndShouldSkip(templateItem, tier))
            {
                apbsLogger.Debug($"[{slot.ToString()}][T${tier}] Skipping item in tier: {templateItem.Id} due to armour class 4 or higher");
                continue;
            }
            
            var equipmentData = itemImportTierHelper.GetEquipmentTierData(tier);
            lock (_equipmentLock)
            {
                equipmentData.PmcUsec.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem);
                equipmentData.PmcBear.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem);
                equipmentData.Scav.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem, true);
                equipmentData.Default.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem);
            }
            
            if (_uniqueEquipment.TryAdd(templateItem.Id, 0))
                Interlocked.Increment(ref _equipmentCounter);
        }
        
        if (equipmentSlotsLength > 0)
        {
            var context = new ImportContext { RootItemId = templateItem.Id };
            context.Ancestors.Add(context.RootItemId);
            for (var tier = 1; tier <= 7; tier++)
            {
                StartEquipmentFilterItemImport(templateItem, context, false, tier);
            }
        }

        if (_uniqueEquipment.ContainsKey(templateItem.Id))
        {
            apbsLogger.Debug($"[{slot}] Completed mod import: {templateItem.Id}");
        }
    }

    /// <summary>
    ///     Start the child item import process, this will kick off adding any relevant information to the APBS Data for Mods
    ///     This is a recursive lookup, it starts here and then actually calls adding the item to the data
    ///     If the item has children, it will then process those and also call adding the item to the data and itself recursively
    /// </summary>
    private void StartEquipmentFilterItemImport(TemplateItem parentItem, ImportContext context, bool weaponImport, int tier)
    {
        var parentItemSlots = parentItem.Properties?.Slots?.ToList();
        if (parentItemSlots is null || parentItemSlots.Count == 0) return;

        context.RecursiveCalls++;
        context.CurrentDepth++;
        context.MaxDepth = Math.Max(context.MaxDepth, context.CurrentDepth);

        try
        {
            foreach (var slot in parentItemSlots)
            {
                var slotName = slot.Name;
                if (slotName is null) 
                    continue;

                var filters = slot.Properties?.Filters?
                    .FirstOrDefault(x => x.Filter is { Count: > 0 })?
                    .Filter;
                if (filters is null) 
                    continue;

                if (filters.Contains(ItemTpl.MOUNT_NCSTAR_MPR45_BACKUP) && ModConfig.Config.CompatibilityConfig.EnableMprSafeGuard)
                    filters.ExceptWith(filters.Where(itemId => itemId != ItemTpl.MOUNT_NCSTAR_MPR45_BACKUP).ToList());

                foreach (var childItemId in filters)
                {
                    if (!context.Ancestors.Add(childItemId))
                    {
                        var stackStr = string.Join(" -> ", context.ParentStack.Select(x => $"{x.ItemId}({x.SlotName})"));
                        apbsLogger.Error($"[IMPORT] Detected recursive loop! Root: {context.RootItemId} | Full path: {stackStr} -> {childItemId} (slot '{slotName}')");
                        continue;
                    }

                    context.ParentStack.Push((childItemId, slotName));

                    try
                    {
                        var childItem = itemHelper.GetItem(childItemId).Value;
                        if (childItem == null) 
                            continue;

                        if (AddModsToBotData(parentItem, childItem, slotName, weaponImport, tier, context))
                        {
                            StartEquipmentFilterItemImport(childItem, context, weaponImport, tier);
                        }
                    }
                    finally
                    {
                        context.ParentStack.Pop();
                        context.Ancestors.Remove(childItemId);
                    }
                }
            }
        }
        finally
        {
            context.CurrentDepth--;
        }
    }

    /// <summary>
    ///     Adds a child item to the bot data for a specific parent item and slot across all tiers
    ///     Checks if the item should be imported first
    ///     Should safely add the item to the bot data, because if it fails at any point it adds the relevant data
    /// </summary>
    private bool AddModsToBotData(TemplateItem parentItem, TemplateItem itemToAdd, string slot, bool weaponImport, int tier, ImportContext context, bool isFromVanilla = false)
    {
        if (!itemImportHelper.AttachmentNeedsImporting(parentItem, itemToAdd, slot))
            return false;

        var comboKey = (ParentId: parentItem.Id, Slot: slot, ChildId: itemToAdd.Id, Tier: tier);
        if (!_processedModCombos.TryAdd(comboKey, 0))
            return false;
        
        switch (weaponImport)
        {
            case false when itemHelper.IsOfBaseclass(itemToAdd.Id, BaseClasses.HEADPHONES):
            {
                if (itemImportHelper.AreHeadphonesMountable(itemToAdd))
                {
                    _mountedHeadphones.TryAdd(itemToAdd.Id, 0);
                }
                else
                {
                    apbsLogger.Debug($"[IMPORT] Item: {itemToAdd.Id} is not mountable headphones but some mod says it is");
                    return false;
                }
                break;
            }
            case true when !itemImportHelper.AttachmentShouldBeInTier(parentItem, itemToAdd, slot, tier):
                return false;
        }

        var modsData = itemImportTierHelper.GetModsTierData(tier);
        lock (_modsLock)
        {
            if (!modsData.TryGetValue(parentItem.Id, out var knownItemData))
                modsData[parentItem.Id] = knownItemData = new Dictionary<string, HashSet<MongoId>>();

            if (!knownItemData.TryGetValue(slot, out var knownAttachmentIds))
                knownItemData[slot] = knownAttachmentIds = new HashSet<MongoId>();

            if (knownAttachmentIds.Add(itemToAdd.Id))
            {
                apbsLogger.Debug($"[IMPORT][T{tier}] Added mod {itemToAdd.Id} to {parentItem.Id} in {slot}");
            }
        }

        if (isFromVanilla) return true;
        
        switch (weaponImport)
        {
            case false when _uniqueEquipmentAttachments.TryAdd(itemToAdd.Id, 0):
                Interlocked.Increment(ref _equipmentAttachmentCounter);
                break;
            case true when _uniqueWeaponAttachments.TryAdd(itemToAdd.Id, 0):
                Interlocked.Increment(ref _weaponAttachmentCounter);
                break;
        }

        return true;
    }
    
    /// <summary>
    ///     Holds context information for a single recursive import of mods for a root item
    /// </summary>
    private sealed class ImportContext
    {
        public readonly Stack<(MongoId ItemId, string SlotName)> ParentStack = new();
        public readonly HashSet<MongoId> Ancestors = new();
        public MongoId RootItemId { get; init; }
        public int CurrentDepth;
        public int MaxDepth;
        public int RecursiveCalls;
    }
}