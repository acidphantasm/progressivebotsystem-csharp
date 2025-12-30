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
public class CustomItemImportService(
    ApbsLogger apbsLogger,
    CustomItemImportHelper customItemImportHelper,
    CustomItemImportTierHelper customItemImportTierHelper,
    DatabaseService databaseService,
    ItemHelper itemHelper): IOnLoad
{
    private Dictionary<ApbsEquipmentSlots, List<MongoId>> _moddedEquipmentSlotDictionary = new();
    private Dictionary<string, Dictionary<string, List<MongoId>>> _moddedClothingBotSlotDictionary = new();
    
    public async Task OnLoad()
    {
        if (!ModConfig.Config.CompatibilityConfig.EnableModdedEquipment
            && !ModConfig.Config.CompatibilityConfig.EnableModdedAttachments
            && !ModConfig.Config.CompatibilityConfig.EnableModdedClothing
            && !ModConfig.Config.CompatibilityConfig.EnableModdedWeapons)
            return;
        
        var stopwatch = Stopwatch.StartNew();
        
        customItemImportHelper.ValidateConfig();
        await customItemImportHelper.BuildVanillaDictionaries();
        
        ImportEquipmentBySlot();
        
        stopwatch.Stop();
        apbsLogger.Warning($"[IMPORT] Completed in {stopwatch.ElapsedMilliseconds} ms");
    }

    /// <summary>
    ///     Start of the import process, all you're doing is validating if the item should be imported
    ///     If it should be imported, it's passed over to the sorting import process
    /// </summary>
    private void ImportEquipmentBySlot()
    {
        var allItems = databaseService.GetItems();
        foreach (var kvp in allItems)
        {
            var itemDetails = kvp.Value;

            if (customItemImportHelper.EquipmentNeedsImporting(itemDetails.Id))
            {
                SortAndStartEquipmentImport(itemDetails);
            }
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
        if (customItemImportHelper.IsHolster(itemId))
        {
            apbsLogger.Debug($"[IMPORT][HOLSTER] Item: {itemId} needs importing: {itemHelper.GetItemName(itemId)}");
            return;
        }
        
        if (customItemImportHelper.IsPrimaryWeapon(itemId))
        {
            if (customItemImportHelper.IsLongRangePrimaryWeapon(itemId))
            {
                // Launch Long Range Import
                apbsLogger.Debug($"[IMPORT][PRIMARYLR] Item: {itemId} needs importing: {itemHelper.GetItemName(itemId)}");
                return;
            }
            // Launch Short Range Import
            apbsLogger.Debug($"[IMPORT][PRIMARYSR] Item: {itemId} needs importing: {itemHelper.GetItemName(itemId)}");
            return;
        }
        
        if (customItemImportHelper.IsScabbard(itemId))
        {
            AddWeaponToBotData(ApbsEquipmentSlots.Scabbard, templateItem);
            return;
        }
        
        if (customItemImportHelper.IsHeadwear(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.Headwear, templateItem);
            return;
        }
        
        if (customItemImportHelper.IsRigSlot(itemId))
        {
            if (customItemImportHelper.IsArmouredRig(templateItem))
            {
                AddEquipmentToBotData(ApbsEquipmentSlots.ArmouredRig, templateItem);
                return;
            }

            AddEquipmentToBotData(ApbsEquipmentSlots.TacticalVest, templateItem);
            return;
        }
        
        if (customItemImportHelper.IsArmourVest(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.ArmorVest, templateItem);
            return;
        }
        
        if (customItemImportHelper.IsBackpack(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.Backpack, templateItem);
            return;
        }
        
        if (customItemImportHelper.IsFacecover(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.FaceCover, templateItem);
            return;
        }
        
        if (customItemImportHelper.IsPackNStrapBelt(itemId))
        {
            if (ModConfig.Config.CompatibilityConfig.PackNStrapUnlootablePmcArmbandBelts)
            {
                customItemImportHelper.MarkPackNStrapUnlootable(templateItem);
            }
            AddEquipmentToBotData(ApbsEquipmentSlots.ArmBand, templateItem);
            return;
        }
        
        if (customItemImportHelper.IsArmband(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.ArmBand, templateItem);
            return;
        }
        
        if (customItemImportHelper.IsHeadphones(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.Earpiece, templateItem);
            return;
        }
        
        if (customItemImportHelper.IsEyeglasses(itemId))
        {
            AddEquipmentToBotData(ApbsEquipmentSlots.Eyewear, templateItem);
            return;
        }
        
        // Launch Equipment Importing
        apbsLogger.Error($"[IMPORT][EQUIP][FAIL] No Classification Handling. Report This. ItemId: {itemId} | Name: {itemHelper.GetItemName(itemId)}");
    }

    /// <summary>
    ///     Add Weapon to the actual bot data, will use helper methods to get the proper weight for the slot
    ///     After importing to bot data, will kick off checking for children item filters and importing those to the proper bot data locations as well
    /// </summary>
    private void AddWeaponToBotData(ApbsEquipmentSlots slot, TemplateItem templateItem)
    {
        var startTier = Math.Clamp(ModConfig.Config.CompatibilityConfig.InitalTierAppearance, 1, 7);
        
        for (var tier = startTier; tier <= 7; tier++)
        {
            var equipmentData = customItemImportTierHelper.GetEquipmentTierData(tier);
            
            equipmentData.PmcUsec.Equipment[slot][templateItem.Id] = customItemImportHelper.GetWeaponSlotWeight(slot, "pmc");
            equipmentData.PmcBear.Equipment[slot][templateItem.Id] = customItemImportHelper.GetWeaponSlotWeight(slot, "pmc");
            equipmentData.Scav.Equipment[slot][templateItem.Id] = customItemImportHelper.GetWeaponSlotWeight(slot, "scav");
            equipmentData.Default.Equipment[slot][templateItem.Id] = customItemImportHelper.GetWeaponSlotWeight(slot, "default");
        }
        
        apbsLogger.Debug($"[{slot.ToString()}] Completed mod import: {templateItem.Id}");
    }

    /// <summary>
    ///     These declarations are here, because they're only used below this line
    /// </summary>
    private readonly HashSet<MongoId> _processedItems = new();
    private int _currentRecursionDepth;
    private int _maxRecursionDepth;
    private int _totalRecursiveCalls;
    
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
            var equipmentData = customItemImportTierHelper.GetEquipmentTierData(tier);

            if (customItemImportHelper.IfArmouredHelmetAndShouldSkip(templateItem, tier))
            {
                apbsLogger.Debug($"[{slot.ToString()}][T${tier}] Skipping item in tier: {templateItem.Id} due to armour class 4 or higher");
                continue;
            }
            
            equipmentData.PmcUsec.Equipment[slot][templateItem.Id] = customItemImportHelper.GetGearSlotWeight(slot, templateItem);
            equipmentData.PmcBear.Equipment[slot][templateItem.Id] = customItemImportHelper.GetGearSlotWeight(slot, templateItem);
            equipmentData.Scav.Equipment[slot][templateItem.Id] = customItemImportHelper.GetGearSlotWeight(slot, templateItem, true);
            equipmentData.Default.Equipment[slot][templateItem.Id] = customItemImportHelper.GetGearSlotWeight(slot, templateItem);
        }
        
        if (equipmentSlotsLength == 0)
        {
            apbsLogger.Debug($"[{slot.ToString()}] Completed mod import: {templateItem.Id}");
            return;
        }
        
        _processedItems.Clear();
        _currentRecursionDepth = 0;
        _maxRecursionDepth = 0;
        _totalRecursiveCalls = 0;
            
        StartEquipmentFilterItemImport(templateItem);
        apbsLogger.Debug(
            $"[{slot.ToString()}] Completed mod import: {templateItem.Id} | Items processed: {_processedItems.Count} | Recursive calls: {_totalRecursiveCalls} | Max depth: {_maxRecursionDepth}");
    }

    /// <summary>
    ///     Start the child item import process, this will kick off adding any relevant information to the APBS Data for Mods
    ///     This is a recursive lookup, it starts here and then actually calls adding the item to the data
    ///     If the item has children, it will then process those and also call adding the item to the data and itself recursively
    /// </summary>
    private void StartEquipmentFilterItemImport(TemplateItem parentItem)
    {
        var parentItemSlots = parentItem.Properties?.Slots?.ToList();
        if (parentItemSlots is null || parentItemSlots.Count == 0) return;

        _totalRecursiveCalls++;
        _currentRecursionDepth++;
        _maxRecursionDepth = Math.Max(_maxRecursionDepth, _currentRecursionDepth);

        try
        {
            _maxRecursionDepth = Math.Max(_maxRecursionDepth, _currentRecursionDepth);

            if (!_processedItems.Add(parentItem.Id))
            {
                apbsLogger.Debug($"[IMPORT] Skipping already processed item: {parentItem.Id} (depth {_currentRecursionDepth})");
                return;
            }
            
            foreach (var slot in parentItemSlots)
            {
                var slotName = slot.Name;
                if (slotName is null)
                {
                    apbsLogger.Error($"[IMPORT] Slot name is null: {parentItem.Id}");
                    continue;
                }

                var filters = slot.Properties?.Filters?
                    .FirstOrDefault(x => x.Filter is { Count: > 0 })?
                    .Filter;

                if (filters is null)
                    continue;

                foreach (var childItemId in filters)
                {
                    var childItem = itemHelper.GetItem(childItemId).Value;
                    if (childItem is null)
                        continue;

                    if (_processedItems.Add(childItem.Id))
                    {
                        apbsLogger.Debug($"[IMPORT] Processing child item: {childItem.Id} (depth {_currentRecursionDepth})");
                    }
                    AddModsToBotData(parentItem, childItem, slotName);
                    StartEquipmentFilterItemImport(childItem);
                }
            }
        }
        finally
        {
            _currentRecursionDepth--;
        }
    }

    private void AddModsToBotData(TemplateItem parentItem, TemplateItem itemToAdd, string slot)
    {
        if (!customItemImportHelper.AttachmentNeedsImporting(parentItem, itemToAdd)) return;
        
        for (var tier = 1; tier <= 7; tier++)
        {
            var modsData = customItemImportTierHelper.GetModsTierData(tier);
            
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
}