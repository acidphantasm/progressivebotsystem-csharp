using System.Collections.Concurrent;
using System.Diagnostics;
using ProgressiveBotSystem.Globals;
using ProgressiveBotSystem.Helpers;
using ProgressiveBotSystem.Models;
using ProgressiveBotSystem.Models.Enums;
using ProgressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace ProgressiveBotSystem.Services;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostSptModLoader + 69)]
public class ItemImportService(
    ApbsLogger apbsLogger,
    ItemImportHelper itemImportHelper,
    ItemImportTierHelper itemImportTierHelper,
    DatabaseService databaseService,
    ItemHelper itemHelper): IOnLoad
{
    private readonly bool _testMode = false;
    
    private readonly ConcurrentDictionary<ApbsEquipmentSlots, int> _slotImportCounts = new();
    private Dictionary<(ApbsEquipmentSlots Slot, int Tier, string BotType), (double WeightSum, int ItemCount)> _baselineSlotData = new();
        
    private readonly ConcurrentDictionary<(MongoId ParentId, string Slot, MongoId ChildId, int Tier), byte> _processedModCombos = new();
    private readonly ConcurrentDictionary<(MongoId ParentId, string Slot, MongoId ChildId, int Tier), byte> _processedVanillaWeaponModCombos = new();

    private readonly ConcurrentDictionary<MongoId, byte> _uniqueWeapons = new();
    private int _weaponCounter;
    private readonly ConcurrentDictionary<MongoId, byte> _uniqueWeaponAttachments = new();
    private int _weaponAttachmentCounter;
    private readonly ConcurrentDictionary<string, byte> _uniqueCalibers = new();
    private int _caliberCounter;
    
    private readonly ConcurrentDictionary<MongoId, byte> _uniqueVanillaWeaponModAttachment = new();
    private int _vanillaWeaponModAttachmentCounter;
    
    private readonly ConcurrentDictionary<MongoId, byte> _uniqueEquipment = new();
    private int _equipmentCounter;
    private readonly ConcurrentDictionary<MongoId, byte> _uniqueEquipmentAttachments = new();
    private int _equipmentAttachmentCounter;

    private int _bearClothingCounter;
    private int _usecClothingCounter;
    
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
        _equipmentCounter = LogAndClear("equipment items", _equipmentCounter, _uniqueEquipment);
        _equipmentAttachmentCounter = LogAndClear("unique equipment attachments", _equipmentAttachmentCounter, _uniqueEquipmentAttachments);
        _bearClothingCounter = LogAndClear("Bear bodies and legs", _bearClothingCounter);
        _usecClothingCounter = LogAndClear("Usec bodies and legs", _usecClothingCounter);
        
        var totalCombos = _processedModCombos.Count;
        var combosPerTier = _processedModCombos.Keys
            .GroupBy(k => k.Tier)
            .OrderBy(g => g.Key)
            .Select(g => $"Tier{g.Key}: {g.Count()}");
        apbsLogger.Success($"[IMPORT] Total mod attachment combinations: {totalCombos} ({string.Join(", ", combosPerTier)})");
        
        _processedModCombos.Clear();
        _processedVanillaWeaponModCombos.Clear();
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
        
        foreach (var item in itemsToImport)
        {
            if (itemImportHelper.WeaponOrEquipmentIsVanilla(item.Id))
                continue;

            var slot = itemImportHelper.ClassifyEquipmentSlot(item, _mountedHeadphones);
            if (slot is null)
            {
                continue;
            }

            _slotImportCounts.AddOrUpdate(slot.Value, 1, (_, count) => count + 1);
        }
        
        if (ModConfig.Config.CompatibilityConfig.UseDynamicWeaponWeights || ModConfig.Config.CompatibilityConfig.UseDynamicEquipmentWeights)
        {
            BuildBaselineWeights();
        }
        
        Parallel.ForEach(itemsToImport, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, SortAndStartEquipmentImport);
        
        var customizationItems = databaseService.GetCustomization();
        var customizationToImport = customizationItems.Values
            .Where(itemImportHelper.CustomizationNeedsImporting)
            .ToList();
        
        foreach (var item in customizationToImport)
        {
            SortAndStartCustomizationImport(item);
        }
        
        LogPoolComposition();
    }
    
    private void LogPoolComposition()
    {
        if (!ModConfig.Config.Debug.ModImportTuning.EnableModImportTuningSuccessLogs)
        {
            return;
        }
        var dynamicWeapons = ModConfig.Config.CompatibilityConfig.UseDynamicWeaponWeights;
        var dynamicEquipment = ModConfig.Config.CompatibilityConfig.UseDynamicEquipmentWeights;

        if (!dynamicWeapons && !dynamicEquipment)
            return;

        var weaponMultiplier = ModConfig.Config.Debug.ModImportTuning.BotTypeToCheck switch
        {
            var value when value.Equals("pmc", StringComparison.OrdinalIgnoreCase)
                => ModConfig.Config.CompatibilityConfig.DynamicWeaponWeightMultipliers.Pmc,

            var value when value.Equals("scav", StringComparison.OrdinalIgnoreCase)
                => ModConfig.Config.CompatibilityConfig.DynamicWeaponWeightMultipliers.Scav,

            var value when value.Equals("default", StringComparison.OrdinalIgnoreCase)
                => ModConfig.Config.CompatibilityConfig.DynamicWeaponWeightMultipliers.Follower,

            _ => ModConfig.Config.CompatibilityConfig.DynamicWeaponWeightMultipliers.Pmc
        };

        var botToCheck = ModConfig.Config.Debug.ModImportTuning.BotTypeToCheck;
        var tierToCheck = ModConfig.Config.Debug.ModImportTuning.TierToCheck;

        apbsLogger.Success($"[IMPORT][POOL] Dynamic weighting summary | Bot: {botToCheck} | Tier: {tierToCheck}");
        apbsLogger.Success($"[IMPORT][POOL] At 1.0 each modded item matches the average vanilla item weight for that slot.");

        var weaponSlots = new HashSet<ApbsEquipmentSlots>
        {
            ApbsEquipmentSlots.FirstPrimaryWeapon_ShortRange,
            ApbsEquipmentSlots.FirstPrimaryWeapon_LongRange,
            ApbsEquipmentSlots.SecondPrimaryWeapon_ShortRange,
            ApbsEquipmentSlots.SecondPrimaryWeapon_LongRange,
            ApbsEquipmentSlots.Holster,
            ApbsEquipmentSlots.Scabbard
        };

        foreach (var slot in _slotImportCounts.Keys.OrderBy(s => s.ToString()))
        {
            var moddedCount = _slotImportCounts[slot];
            if (moddedCount == 0)
            {
                continue;
            }

            var isWeaponSlot = weaponSlots.Contains(slot);
            var dynamicEnabled = isWeaponSlot ? dynamicWeapons : dynamicEquipment;
            if (!dynamicEnabled)
            {
                continue;
            }

            var (vanillaWeightSum, vanillaCount) = _baselineSlotData.GetValueOrDefault((slot, tierToCheck, botToCheck));
            if (vanillaCount == 0)
            {
                continue;
            }

            var multiplier = isWeaponSlot ? weaponMultiplier : itemImportHelper.GetEquipmentSlotMultiplierPublic(slot);

            var averageVanillaWeight = Math.Round(vanillaWeightSum / vanillaCount, 2);
            var moddedWeightEach = Math.Round(averageVanillaWeight * multiplier, 2);
            var totalWeight = vanillaWeightSum + (moddedCount * moddedWeightEach);
            var vanillaShare = Math.Round(vanillaWeightSum / totalWeight * 100, 1);
            var moddedShare = Math.Round(100 - vanillaShare, 1);

            apbsLogger.Success($"[IMPORT][POOL] {slot} | multiplier: {multiplier}");
            apbsLogger.Success($"[IMPORT][POOL]   Vanilla: {vanillaCount} items | Avg weight: {averageVanillaWeight}");
            apbsLogger.Success($"[IMPORT][POOL]   Modded:  {moddedCount} items | Weight each: {moddedWeightEach}");
            apbsLogger.Success($"[IMPORT][POOL]   Split: {vanillaShare}% vanilla / {moddedShare}% modded");
        }

        _slotImportCounts.Clear();
    }
    
    private void BuildBaselineWeights()
    {
        var baseline = new Dictionary<(ApbsEquipmentSlots, int, string), (double, int)>();

        foreach (var slot in Enum.GetValues<ApbsEquipmentSlots>())
        {
            for (var tier = 1; tier <= 7; tier++)
            {
                var equipmentData = itemImportTierHelper.GetEquipmentTierData(tier);
                baseline[(slot, tier, "pmc")]     = GetSlotData(equipmentData.PmcUsec.Equipment, slot);
                baseline[(slot, tier, "scav")]    = GetSlotData(equipmentData.Scav.Equipment, slot);
                baseline[(slot, tier, "default")] = GetSlotData(equipmentData.Default.Equipment, slot);
            }
        }

        lock (_equipmentLock)
        {
            _baselineSlotData = baseline;
        }
    }
    
    private static (double WeightSum, int ItemCount) GetSlotData(Dictionary<ApbsEquipmentSlots, Dictionary<MongoId, double>> equipment, ApbsEquipmentSlots slot)
    {
        if (!equipment.TryGetValue(slot, out var slotDict) || slotDict.Count == 0)
        {
            return (0, 0);
        }
        return (slotDict.Values.Sum(), slotDict.Count);
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
                if (itemImportHelper.IsWttBossWeapon(templateItem.Id))
                {
                    AssignBossWeapon(slot, templateItem.Id, equipmentData, tier);
                }
                else
                {
                    AssignDefaultWeapon(slot, templateItem.Id, equipmentData, tier);
                }
            }
            
            if (_uniqueWeapons.TryAdd(templateItem.Id, 0))
                Interlocked.Increment(ref _weaponCounter);

            if (!string.IsNullOrEmpty(ammoCaliber) && itemImportHelper.AmmoCaliberNeedsAdded(ammoCaliber))
            {
                ProcessAmmoForWeapon(templateItem, ammoCaliber, tier);
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
            apbsLogger.Debug($"[IMPORT][{slot}] Completed mod import: {templateItem.Id}");
        }
    }
    
    private void AssignBossWeapon(ApbsEquipmentSlots slot, MongoId itemId, EquipmentTierData equipmentData, int tier)
    {
        var assignedBosses = itemImportHelper.BossAssignmentPerWtt(itemId);
        foreach (var boss in assignedBosses)
        {
            var data = boss switch
            {
                "bossbully" => equipmentData.BossBully,
                "bossgluhar" => equipmentData.BossGluhar,
                "bosskilla" => equipmentData.BossKilla,
                "bossknight" => equipmentData.BossKnight,
                "followerbigpipe" => equipmentData.FollowerBigPipe,
                "followerbirdeye" => equipmentData.FollowerBirdeye,
                "bosskojaniy" => equipmentData.BossKojaniy,
                "bosspartisan" => equipmentData.BossPartisan,
                "bosszryachiy" => equipmentData.BossZryachiy,
                _ => throw new InvalidOperationException($"Unknown boss {boss}")
            };

            if (!data.Equipment.TryGetValue(slot, out var slotDictionary))
            {
                slotDictionary = new Dictionary<MongoId, double>();
                data.Equipment[slot] = slotDictionary;
            }

            var existingCount = slotDictionary.Count;
            var totalWeight = slotDictionary.Values.Sum();
            var averageWeight = existingCount > 0 ? Math.Round(totalWeight / existingCount) : 1.0;

            slotDictionary[itemId] = averageWeight;
        }
        
        if (ModConfig.Config.CompatibilityConfig.WttArmouryAddBossVariantsToOthers)
        {
            AssignDefaultWeapon(slot, itemId, equipmentData, tier);
        }
    }

    private void AssignDefaultWeapon(ApbsEquipmentSlots slot, MongoId itemId, EquipmentTierData equipmentData, int tier)
    {
        var testId = "67f425638b8cbfdc0cd1b5f2";
        var isTestItem = _testMode && itemId == testId;

        if (isTestItem)
        {
            equipmentData.PmcUsec.Equipment[slot][itemId] = 50000;
            equipmentData.PmcBear.Equipment[slot][itemId] = 50000;
            equipmentData.Scav.Equipment[slot][itemId] = 50000;
            equipmentData.Default.Equipment[slot][itemId] = 50000;
            return;
        }

        var (pmcWeightSum, pmcCount) = _baselineSlotData.GetValueOrDefault((slot, tier, "pmc"));
        var (scavWeightSum, scavCount) = _baselineSlotData.GetValueOrDefault((slot, tier, "scav"));
        var (defaultWeightSum, defaultCount) = _baselineSlotData.GetValueOrDefault((slot, tier, "default"));
        
        equipmentData.PmcUsec.Equipment[slot][itemId] = itemImportHelper.GetWeaponSlotWeight(slot, "pmc", pmcWeightSum, pmcCount);
        equipmentData.PmcBear.Equipment[slot][itemId] = itemImportHelper.GetWeaponSlotWeight(slot, "pmc", pmcWeightSum, pmcCount);
        equipmentData.Scav.Equipment[slot][itemId] = itemImportHelper.GetWeaponSlotWeight(slot, "scav", scavWeightSum, scavCount);
        equipmentData.Default.Equipment[slot][itemId] = itemImportHelper.GetWeaponSlotWeight(slot, "default", defaultWeightSum, defaultCount);
    }
    
    private void ProcessAmmoForWeapon(TemplateItem templateItem, string ammoCaliber, int tier)
    {
        var chambers = templateItem.Properties?.Chambers ?? [];
        var filter = chambers
            .SelectMany(c => c.Properties?.Filters ?? [])
            .Select(f => f.Filter)
            .FirstOrDefault(f => f != null);
        
        var ammoIds = filter ?? itemImportHelper.GetCompatibleCartridgesFromMagazineTemplate(templateItem);

        foreach (var ammoId in ammoIds)
        {
            if (!itemImportHelper.AmmoNeedsImporting(ammoId, ammoCaliber)) 
                continue;

            AddAmmoToBotData(ammoId, ammoCaliber, tier);
            if (_uniqueCalibers.TryAdd(ammoCaliber, 0))
                Interlocked.Increment(ref _caliberCounter);

            apbsLogger.Debug($"[IMPORT][T{tier}] Adding AmmoCaliber: {ammoCaliber} and {ammoIds.Count} ammunition types.");
        }
    }

    private void StartVanillaWeaponModAttachmentImport(TemplateItem parentItem, int tier, ImportContext context)
    {
        var weaponSlots = parentItem.Properties?.Slots?.ToList();
        if (weaponSlots is null || weaponSlots.Count == 0)
            return;

        context.CurrentDepth++;
        context.MaxDepth = Math.Max(context.MaxDepth, context.CurrentDepth);
        
        try
        {
            foreach (var slot in weaponSlots)
            {
                var slotName = slot.Name;
                if (slotName is null) 
                    continue;

                var originalFilters = slot.Properties?.Filters?
                    .FirstOrDefault(x => x.Filter is { Count: > 0 })?
                    .Filter;
                if (originalFilters is null) 
                    continue;
                
                var workingFilters = new HashSet<MongoId>(originalFilters);
                
                if (workingFilters.Contains(ItemTpl.MOUNT_NCSTAR_MPR45_BACKUP) && ModConfig.Config.CompatibilityConfig.EnableMprSafeGuard)
                    workingFilters.RemoveWhere(id => id != ItemTpl.MOUNT_NCSTAR_MPR45_BACKUP);
                
                foreach (var childItemId in workingFilters)
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

                        if (AddModsToBotData(parentItem, childItem, slotName, weaponImport: true, tier, true))
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
                apbsLogger.Debug($"[IMPORT][{slot}][T{tier}] Skipping item in tier: {templateItem.Id} due to armour class 4 or higher");
                continue;
            }

            var equipmentData = itemImportTierHelper.GetEquipmentTierData(tier);
            lock (_equipmentLock)
            {
                var testId = "694706dd0144198d74a266ff";
                var isTestItem = _testMode && templateItem.Id == testId;

                if (isTestItem)
                {
                    equipmentData.PmcUsec.Equipment[slot][templateItem.Id] = 50000;
                    equipmentData.PmcBear.Equipment[slot][templateItem.Id] = 50000;
                    equipmentData.Scav.Equipment[slot][templateItem.Id]    = 50000;
                    equipmentData.Default.Equipment[slot][templateItem.Id] = 50000;
                }
                else
                {
                    var (pmcWeightSum, pmcCount) = _baselineSlotData.GetValueOrDefault((slot, tier, "pmc"));
                    var (scavWeightSum, scavCount) = _baselineSlotData.GetValueOrDefault((slot, tier, "scav"));
                    var (defaultWeightSum, defaultCount) = _baselineSlotData.GetValueOrDefault((slot, tier, "default"));

                    equipmentData.PmcUsec.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem, false,   pmcWeightSum,     pmcCount);
                    equipmentData.PmcBear.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem, false,   pmcWeightSum,     pmcCount);
                    equipmentData.Scav.Equipment[slot][templateItem.Id]    = itemImportHelper.GetGearSlotWeight(slot, templateItem, true,    scavWeightSum,    scavCount);
                    equipmentData.Default.Equipment[slot][templateItem.Id] = itemImportHelper.GetGearSlotWeight(slot, templateItem, false,   defaultWeightSum, defaultCount);
                }
            }

            if (_uniqueEquipment.TryAdd(templateItem.Id, 0))
                Interlocked.Increment(ref _equipmentCounter);
        }

        if (equipmentSlotsLength > 0)
        {
            var context = new ImportContext { RootItemId = templateItem.Id };
            context.Ancestors.Add(context.RootItemId);
            for (var tier = 1; tier <= 7; tier++)
                StartEquipmentFilterItemImport(templateItem, context, false, tier);
        }

        if (_uniqueEquipment.ContainsKey(templateItem.Id))
            apbsLogger.Debug($"[IMPORT][{slot}] Completed mod import: {templateItem.Id}");
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

        context.CurrentDepth++;
        context.MaxDepth = Math.Max(context.MaxDepth, context.CurrentDepth);

        try
        {
            foreach (var slot in parentItemSlots)
            {
                var slotName = slot.Name;
                if (slotName is null) 
                    continue;

                var originalFilters = slot.Properties?.Filters?
                    .FirstOrDefault(x => x.Filter is { Count: > 0 })?
                    .Filter;
                if (originalFilters is null) 
                    continue;

                var workingFilters = new HashSet<MongoId>(originalFilters);
                if (workingFilters.Contains(ItemTpl.MOUNT_NCSTAR_MPR45_BACKUP) && ModConfig.Config.CompatibilityConfig.EnableMprSafeGuard)
                    workingFilters.RemoveWhere(id => id != ItemTpl.MOUNT_NCSTAR_MPR45_BACKUP);

                foreach (var childItemId in workingFilters)
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

                        if (AddModsToBotData(parentItem, childItem, slotName, weaponImport, tier))
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
    private bool AddModsToBotData(TemplateItem parentItem, TemplateItem itemToAdd, string slot, bool weaponImport, int tier, bool isFromVanilla = false)
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
    }
}