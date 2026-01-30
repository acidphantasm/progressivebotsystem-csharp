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
    private int _weaponCounter = 0;
    private HashSet<MongoId> _uniqueWeaponAttachments = new HashSet<MongoId>();
    private int _weaponAttachmentCounter = 0;
    private int _caliberCounter = 0;
    
    private int _equipmentCounter = 0;
    private HashSet<MongoId> _uniqueEquipmentAttachments = new HashSet<MongoId>();
    private int _equipmentAttachmentCounter = 0;
    
    private int _appearanceCounter = 0;
    
    private readonly HashSet<MongoId> _mountedHeadphones = new();
    
    private readonly Lock _equipmentLock = new();
    private readonly Lock _modsLock = new();
    private readonly Lock _ammoLock = new();
    
    public async Task OnLoad()
    {
        if (!ModConfig.Config.CompatibilityConfig.EnableModdedEquipment
            && !ModConfig.Config.CompatibilityConfig.EnableModdedAttachments
            && !ModConfig.Config.CompatibilityConfig.EnableModdedClothing
            && !ModConfig.Config.CompatibilityConfig.EnableModdedWeapons)
            return;
        
        var stopwatch = Stopwatch.StartNew();
        
        itemImportHelper.ValidateConfig();
        await itemImportHelper.BuildVanillaDictionaries();
        
        ImportEquipmentBySlot();
        
        stopwatch.Stop();
        apbsLogger.Success($"[IMPORT] Completed in {stopwatch.ElapsedMilliseconds} ms");
        _weaponCounter = LogAndClear("weapons", _weaponCounter, _uniqueWeaponAttachments);
        _weaponAttachmentCounter = LogAndClear("unique weapon attachments", _weaponAttachmentCounter, _uniqueWeaponAttachments);
        _caliberCounter = LogAndClear("calibers", _caliberCounter);
        _equipmentCounter = LogAndClear("equipment items", _equipmentCounter, _uniqueEquipmentAttachments);
        _equipmentAttachmentCounter = LogAndClear("unique equipment attachments", _equipmentAttachmentCounter, _uniqueEquipmentAttachments);
        _appearanceCounter = LogAndClear("appearance items", _appearanceCounter);
    }

    /// <summary>
    ///     Fancy helper method to log the import counts and then reset the variables to 0
    /// </summary>
    private int LogAndClear(string name, int counter, HashSet<MongoId>? setToClear = null)
    {
        if (counter != 0) apbsLogger.Success($"[IMPORT] Imported {counter} {name}.");
        setToClear?.Clear();
        return 0;
    }
    
    /// <summary>
    ///     Start of the import process, all you're doing is validating if the item should be imported
    ///     If it should be imported, it's passed over to the sorting import process
    /// </summary>
    private void ImportEquipmentBySlot()
    {
        var allItems = databaseService.GetItems();
        Parallel.ForEach(
            allItems.Values,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 },
            templateItem =>
            {
                if (itemImportHelper.EquipmentNeedsImporting(templateItem.Id))
                {
                    SortAndStartEquipmentImport(templateItem);
                }
            });
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
            if (_mountedHeadphones.Contains(itemId)) return;
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

    /// <summary>
    ///     Add Weapon to the actual bot data, will use helper methods to get the proper weight for the slot
    ///     After importing to bot data, will kick off checking for children item filters and importing those to the proper bot data locations as well
    /// </summary>
    private void AddWeaponToBotData(ApbsEquipmentSlots slot, TemplateItem templateItem)
    {
        var startTier = Math.Clamp(ModConfig.Config.CompatibilityConfig.InitalTierAppearance, 1, 7);
        var weaponSlotsLength = templateItem.Properties?.Slots?.Count() ?? 0;
        var ammoCaliber = templateItem.Properties?.AmmoCaliber ?? string.Empty;
        
        for (var tier = startTier; tier <= 7; tier++)
        {
            var equipmentData = itemImportTierHelper.GetEquipmentTierData(tier);
            
            lock (_equipmentLock)
            {
                equipmentData.PmcUsec.Equipment[slot][templateItem.Id] = itemImportHelper.GetWeaponSlotWeight(slot, "pmc");
                equipmentData.PmcBear.Equipment[slot][templateItem.Id] = itemImportHelper.GetWeaponSlotWeight(slot, "pmc");
                equipmentData.Scav.Equipment[slot][templateItem.Id] = itemImportHelper.GetWeaponSlotWeight(slot, "scav");
                equipmentData.Default.Equipment[slot][templateItem.Id] = itemImportHelper.GetWeaponSlotWeight(slot, "default");
            }
        }

        if (!string.IsNullOrEmpty(ammoCaliber) && itemImportHelper.AmmoCaliberNeedsAdded(ammoCaliber))
        {
            var chambers = templateItem.Properties?.Chambers ?? [];
            var filter = chambers
                .SelectMany(c => c?.Properties?.Filters ?? Enumerable.Empty<SlotFilter>())
                .Select(f => f.Filter)
                .FirstOrDefault(filter => filter != null);

            foreach (var ammoId in filter ?? [])
            {
                if (!itemImportHelper.AmmoNeedsImporting(ammoId, ammoCaliber)) continue;
                
                apbsLogger.Debug($"Adding AmmoCaliber: {ammoCaliber} and ammoId: {ammoId}");
                AddAmmoToBotData(ammoId, ammoCaliber);
            }

            if (filter == null || filter.Count == 0)
            {
                var cartridgesFromMagazine = itemImportHelper.GetCompatibleCartridgesFromMagazineTemplate(templateItem);
                foreach (var ammoId in cartridgesFromMagazine)
                {
                    if (!itemImportHelper.AmmoNeedsImporting(ammoId, ammoCaliber)) continue;
                    
                    apbsLogger.Debug($"Adding Fallback AmmoCaliber: {ammoCaliber} and ammoId: {ammoId}");
                    AddAmmoToBotData(ammoId, ammoCaliber);
                }
            }

            _caliberCounter++;
        }

        _weaponCounter++;
        
        if (weaponSlotsLength == 0)
        {
            apbsLogger.Debug($"[{slot.ToString()}] Completed mod import: {templateItem.Id}");
            return;
        }
        
        var context = new ImportContext();
        StartEquipmentFilterItemImport(templateItem, context, true);
        
        apbsLogger.Debug($"[{slot.ToString()}] Completed mod import: {templateItem.Id} | Recursive calls: {context.RecursiveCalls} | Max depth: {context.MaxDepth}");
    }

    private void AddAmmoToBotData(MongoId itemId, string caliber)
    {
        var startTier = Math.Clamp(ModConfig.Config.CompatibilityConfig.InitalTierAppearance, 1, 7);
        
        for (var tier = startTier; tier <= 7; tier++)
        {
            var ammoData = itemImportTierHelper.GetAmmoTierData(tier);

            lock (_ammoLock)
            {
                AddAmmo(ammoData.ScavAmmo, caliber, itemId);
                AddAmmo(ammoData.PmcAmmo, caliber, itemId);
                AddAmmo(ammoData.BossAmmo, caliber, itemId);
            }
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
            var equipmentData = itemImportTierHelper.GetEquipmentTierData(tier);

            if (itemImportHelper.IfArmouredHelmetAndShouldSkip(templateItem, tier))
            {
                apbsLogger.Debug($"[{slot.ToString()}][T${tier}] Skipping item in tier: {templateItem.Id} due to armour class 4 or higher");
                continue;
            }
            
            lock (_equipmentLock)
            {
                equipmentData.PmcUsec.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem);
                equipmentData.PmcBear.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem);
                equipmentData.Scav.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem, true);
                equipmentData.Default.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem);
            }
        }

        _equipmentCounter++;
        
        if (equipmentSlotsLength == 0)
        {
            apbsLogger.Debug($"[{slot.ToString()}] Completed mod import: {templateItem.Id}");
            return;
        }
        
        var context = new ImportContext { RootItemId = templateItem.Id };
        StartEquipmentFilterItemImport(templateItem, context);
        apbsLogger.Debug($"[{slot.ToString()}] Completed mod import: {templateItem.Id} | Recursive calls: {context.RecursiveCalls} | Max depth: {context.MaxDepth}");
    }

    /// <summary>
    ///     Start the child item import process, this will kick off adding any relevant information to the APBS Data for Mods
    ///     This is a recursive lookup, it starts here and then actually calls adding the item to the data
    ///     If the item has children, it will then process those and also call adding the item to the data and itself recursively
    /// </summary>
    private void StartEquipmentFilterItemImport(TemplateItem parentItem, ImportContext context, bool weaponImport = false)
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
                if (slotName is null) continue;

                context.ParentStack.Push((parentItem.Id, slotName));

                if (!context.Ancestors.Add(parentItem.Id))
                {
                    context.ParentStack.Pop();
                    continue;
                }

                var filters = slot.Properties?.Filters?
                    .FirstOrDefault(x => x.Filter is { Count: > 0 })?
                    .Filter;

                if (filters is null)
                {
                    context.Ancestors.Remove(parentItem.Id);
                    context.ParentStack.Pop();
                    continue;
                }

                foreach (var childItemId in filters)
                {
                    if (context.Ancestors.Contains(childItemId))
                    {
                        var stackStr = string.Join(" -> ", context.ParentStack.Select(x => $"{x.ItemId}({x.SlotName})"));
                        apbsLogger.Error($"[IMPORT] Detected recursive loop! Root: {context.RootItemId} | Parent stack: {stackStr} -> {childItemId} (slot '{slotName}')");
                        continue;
                    }

                    var childItem = itemHelper.GetItem(childItemId);
                    if (childItem.Value is null) continue;

                    AddModsToBotData(parentItem, childItem.Value, slotName, weaponImport);
                    StartEquipmentFilterItemImport(childItem.Value, context, weaponImport);
                }

                context.Ancestors.Remove(parentItem.Id);
                context.ParentStack.Pop();
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
    private void AddModsToBotData(TemplateItem parentItem, TemplateItem itemToAdd, string slot, bool weaponImport = false)
    {
        if (!itemImportHelper.AttachmentNeedsImporting(parentItem, itemToAdd, slot)) return;
        
        if (!weaponImport && itemHelper.IsOfBaseclass(itemToAdd.Id, BaseClasses.HEADPHONES))
        {
            if (itemImportHelper.AreHeadphonesMountable(itemToAdd))
            {
                _mountedHeadphones.Add(itemToAdd.Id);
            }
            else
            {
                apbsLogger.Debug($"Item: {itemToAdd.Id} is not mountable headphones but some mod says it is");
                return;
            }
        }
        
        for (var tier = 1; tier <= 7; tier++)
        {
            var modsData = itemImportTierHelper.GetModsTierData(tier);

            if (weaponImport && !itemImportHelper.AttachmentShouldBeInTier(parentItem, itemToAdd, slot, tier))
                continue;
            
            lock (_modsLock)
            {
                if (!modsData.TryGetValue(parentItem.Id, out var knownItemData))
                {
                    knownItemData = new Dictionary<string, HashSet<MongoId>>();
                    modsData[parentItem.Id] = knownItemData;

                    apbsLogger.Debug($"[IMPORT][T{tier}] New parent entry: {parentItem.Id}");
                }

                if (!knownItemData.TryGetValue(slot, out var knownAttachmentIds))
                {
                    knownAttachmentIds = new HashSet<MongoId>();
                    knownItemData[slot] = knownAttachmentIds;

                    apbsLogger.Debug($"[IMPORT][T{tier}] New slot '{slot}' for parent {parentItem.Id}");
                }

                if (knownAttachmentIds.Add(itemToAdd.Id))
                {
                    apbsLogger.Debug($"[IMPORT][T{tier}] Added mod {itemToAdd.Id} to {parentItem.Id} in {slot}");
                }
            }
        }

        if (_uniqueEquipmentAttachments.Add(itemToAdd.Id))
        {
            _equipmentAttachmentCounter++;
        }
    }
    
    /// <summary>
    ///     Holds context information for a single recursive import of mods for a root item
    /// </summary>
    private sealed class ImportContext
    {
        public Stack<(MongoId ItemId, string SlotName)> ParentStack = new();
        public HashSet<MongoId> Ancestors = new();
        public MongoId RootItemId { get; set; }
        public int CurrentDepth;
        public int MaxDepth;
        public int RecursiveCalls;
    }
}