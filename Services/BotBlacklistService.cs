using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Models.Enums;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace _progressiveBotSystem.Services;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostSptModLoader + 70)]
public class BotBlacklistService(
    ApbsLogger apbsLogger,
    BotBlacklistHelper botBlacklistHelper,
    ItemImportTierHelper itemImportTierHelper): IOnLoad
{
    public Task OnLoad()
    {
        apbsLogger.Debug("Running Blacklist");
        RunBlacklisting();
        return Task.CompletedTask;
    }

    public void RunBlacklisting()
    {
        RunWeaponBlacklist();
        RunEquipmentBlacklist();
        RunAmmoBlacklist();
        RunAttachmentBlacklist();
        RunClothingBlacklist();
    }

    private void RunWeaponBlacklist()
    {
        for (var tier = 1; tier <= 7; tier++)
        {
            var currentTierBlacklist = botBlacklistHelper.GetWeaponBlacklistTierData(tier);
            var currentEquipmentTierData = itemImportTierHelper.GetEquipmentTierData(tier);

            foreach (var botProp in typeof(EquipmentTierData).GetProperties())
            {
                var botType = botProp.Name.ToLowerInvariant();
                var data = botProp.GetValue(currentEquipmentTierData) as ApbsEquipmentBot;
                if (data?.Equipment == null)
                    continue;

                foreach (var item in currentTierBlacklist ?? [])
                {
                    var slotsToRemove = new[]
                    {
                        ApbsEquipmentSlots.FirstPrimaryWeapon_LongRange,
                        ApbsEquipmentSlots.FirstPrimaryWeapon_ShortRange,
                        ApbsEquipmentSlots.SecondPrimaryWeapon_LongRange,
                        ApbsEquipmentSlots.SecondPrimaryWeapon_ShortRange,
                        ApbsEquipmentSlots.Holster,
                        ApbsEquipmentSlots.Scabbard
                    };

                    var removedFrom = new List<string>();
                    var blockedFrom = new List<string>();

                    foreach (var slot in slotsToRemove)
                    {
                        if (!data.Equipment.TryGetValue(slot, out var weaponList) || weaponList.Count == 0)
                            continue;

                        
                        if (weaponList.ContainsKey(item) && weaponList.Count == 1)
                        {
                            blockedFrom.Add(slot.ToString());
                            continue;
                        }

                        if (weaponList.Remove(item))
                        {
                            removedFrom.Add(slot.ToString());
                        }
                    }

                    if (removedFrom.Count > 0)
                    {
                        apbsLogger.Warning($"[WEAPON BLACKLIST] Removed {item} from Tier {tier} slot(s): {string.Join(", ", removedFrom)} on bot: {botType}");
                    }

                    if (blockedFrom.Count > 0)
                    {
                        apbsLogger.Warning($"[WEAPON BLACKLIST] Could NOT remove {item} from Tier {tier} because it was the only weapon left in slot(s): {string.Join(", ", blockedFrom)} on bot: {botType}");
                    }
                }
            }
        }
    }

    private void RunEquipmentBlacklist()
    {
        for (var tier = 1; tier <= 7; tier++)
        {
            var currentTierBlacklist = botBlacklistHelper.GetEquipmentBlacklistTierData(tier);
            var currentEquipmentTierData = itemImportTierHelper.GetEquipmentTierData(tier);

            foreach (var botProp in typeof(EquipmentTierData).GetProperties())
            {
                var botType = botProp.Name.ToLowerInvariant();
                var data = botProp.GetValue(currentEquipmentTierData) as ApbsEquipmentBot;
                if (data?.Equipment == null)
                    continue;

                foreach (var item in currentTierBlacklist ?? [])
                {
                    var slotsToRemove = new[]
                    {
                        ApbsEquipmentSlots.ArmBand,
                        ApbsEquipmentSlots.ArmorVest,
                        ApbsEquipmentSlots.ArmouredRig,
                        ApbsEquipmentSlots.Backpack,
                        ApbsEquipmentSlots.Earpiece,
                        ApbsEquipmentSlots.Eyewear,
                        ApbsEquipmentSlots.FaceCover,
                        ApbsEquipmentSlots.Headwear,
                        ApbsEquipmentSlots.TacticalVest
                    };

                    var removedFrom = new List<string>();
                    var blockedFrom = new List<string>();

                    foreach (var slot in slotsToRemove)
                    {
                        if (!data.Equipment.TryGetValue(slot, out var equipmentList) || equipmentList.Count == 0)
                            continue;

                        if (equipmentList.ContainsKey(item) && equipmentList.Count == 1)
                        {
                            blockedFrom.Add(slot.ToString());
                            continue;
                        }

                        if (equipmentList.Remove(item))
                        {
                            removedFrom.Add(slot.ToString());
                        }
                    }

                    if (removedFrom.Count > 0)
                    {
                        apbsLogger.Warning(
                            $"[EQUIPMENT BLACKLIST] Removed {item} from Tier {tier} slot(s): {string.Join(", ", removedFrom)} on bot: {botType}");
                    }

                    if (blockedFrom.Count > 0)
                    {
                        apbsLogger.Warning($"[EQUIPMENT BLACKLIST] Could NOT remove {item} from Tier {tier} because it was the only item left in slot(s): {string.Join(", ", blockedFrom)} on bot: {botType}");
                    }
                }
            }
        }
    }

    private void RunAmmoBlacklist()
    {
        for (var tier = 1; tier <= 7; tier++)
        {
            var currentTierBlacklist = botBlacklistHelper.GetAmmoBlacklistTierData(tier);
            var currentAmmoTierData = itemImportTierHelper.GetAmmoTierData(tier);
            
            foreach (var item in currentTierBlacklist ?? [])
            {
                RemoveAmmo(currentAmmoTierData.ScavAmmo, "Scav", item, tier);
                RemoveAmmo(currentAmmoTierData.PmcAmmo, "Pmc", item, tier);
                RemoveAmmo(currentAmmoTierData.BossAmmo, "Boss", item, tier);
            }
        }
    }

    private void RemoveAmmo(Dictionary<string, Dictionary<MongoId, double>> dictionary, string botType, MongoId ammoToRemove, int tierToRemove)
    {
        var itemRemoved = false;
        
        // This is only a list because maybe someone's preset is stupid and puts an ammo in multiple calibers, which would break anyway but oh well
        var blockedRemovals = new List<string>();
        
        foreach (var (caliberName, ammoDict) in dictionary)
        {
            if (ammoDict.ContainsKey(ammoToRemove) && ammoDict.Count == 1)
            {
                blockedRemovals.Add(caliberName);
                continue;
            }
            if (ammoDict.Count > 1 && ammoDict.Remove(ammoToRemove))
            {
                itemRemoved = true;
            }
        }
        
        if (itemRemoved)
        {
            apbsLogger.Warning($"[AMMO BLACKLIST] Removed {ammoToRemove} from Tier: {tierToRemove}");
        }
        
        if (blockedRemovals.Count > 0)
        {
            apbsLogger.Warning($"[AMMO BLACKLIST] Could NOT remove {ammoToRemove} from Tier: {tierToRemove} because it was the only ammo left in caliber(s): {string.Join(", ", blockedRemovals)} for {botType}");
        }
    }
    
    private void RunAttachmentBlacklist()
    {
        for (var tier = 1; tier <= 7; tier++)
        {
            var currentTierBlacklist = botBlacklistHelper.GetAttachmentBlacklistTierData(tier);
            var currentAttachmentData = itemImportTierHelper.GetModsTierData(tier);
            
            foreach (var item in currentTierBlacklist ?? [])
            {
                RemoveAttachment(currentAttachmentData, item, tier);
            }
        }
    }
    
    private void RemoveAttachment(Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> dictionary, MongoId itemToRemove, int tier)
    {
        var removedFrom = new List<string>();
        var blockedRemovals = new List<string>();
        
        foreach (var (primaryItem, innerDictionary) in dictionary)
        {
            if (innerDictionary.Count == 0) continue;

            foreach (var (slot, modSet) in innerDictionary)
            {
                if (modSet.Count == 0) continue;
                if (modSet.Count == 1 && modSet.Contains(itemToRemove))
                {
                    blockedRemovals.Add($"{primaryItem}:{slot}");
                    continue;
                }
                
                if (modSet.Remove(itemToRemove))
                {
                    removedFrom.Add($"{primaryItem}:{slot}");
                }
            }
        }

        if (removedFrom.Count > 0)
        {
            apbsLogger.Warning($"[ATTACHMENT BLACKLIST] Removed {itemToRemove} in tier {tier} from {removedFrom.Count} items: {string.Join(", ", removedFrom)}");
        }
        
        if (blockedRemovals.Count > 0)
        {
            apbsLogger.Warning($"[ATTACHMENT BLACKLIST] Could NOT remove {itemToRemove} from Tier {tier} because it was the only mod left in slot(s): {string.Join(", ", blockedRemovals)}");
        }
    }
    
    private void RunClothingBlacklist()
    {
        for (var tier = 1; tier <= 7; tier++)
        {
            var currentTierBlacklist = botBlacklistHelper.GetClothingBlacklistTierData(tier);
            var currentAppearanceData = itemImportTierHelper.GetAppearanceTierData(tier);
            
            foreach (var item in currentTierBlacklist ?? [])
            {
                ProcessAppearanceDict(currentAppearanceData.PmcUsec, item, tier, "USEC");
                ProcessAppearanceDict(currentAppearanceData.PmcBear, item, tier, "BEAR");

                ProcessSeason(currentAppearanceData.SpringEarly, item, tier, "SpringEarly");
                ProcessSeason(currentAppearanceData.Spring, item, tier, "Spring");
                ProcessSeason(currentAppearanceData.Summer, item, tier, "Summer");
                ProcessSeason(currentAppearanceData.Autumn, item, tier, "Autumn");
                ProcessSeason(currentAppearanceData.Winter, item, tier, "Winter");
            }
        }
    }
    
    private void ProcessAppearanceDict(Dictionary<string, Appearance> dictionary, MongoId item, int tier, string label)
    {
        var removedFrom = new List<string>();
        var blockedRemovals = new List<string>();

        if (dictionary.TryGetValue("appearance", out var appearanceData))
        {
            RemoveFromAppearance(
                appearanceData,
                item,
                tier,
                $"{label}",
                removedFrom,
                blockedRemovals);
        }

        if (removedFrom.Count > 0)
        {
            apbsLogger.Warning(
                $"[CLOTHING BLACKLIST] Removed {item} from Tier {tier}: {string.Join(", ", removedFrom)}");
        }

        if (blockedRemovals.Count > 0)
        {
            apbsLogger.Warning(
                $"[CLOTHING BLACKLIST] Could NOT remove {item} from Tier {tier} because it was the only option in: {string.Join(", ", blockedRemovals)}");
        }
    }
    
    private void RemoveFromAppearance(Appearance appearance, MongoId itemToRemove, int tier, string context, List<string> removedFrom, List<string> blockedRemovals)
    {
        RemoveAppearanceItem(appearance.Body,  itemToRemove, $"{context}:Body",  removedFrom, blockedRemovals);
        RemoveAppearanceItem(appearance.Feet,  itemToRemove, $"{context}:Feet",  removedFrom, blockedRemovals);
        RemoveAppearanceItem(appearance.Hands, itemToRemove, $"{context}:Hands", removedFrom, blockedRemovals);
        RemoveAppearanceItem(appearance.Head,  itemToRemove, $"{context}:Head",  removedFrom, blockedRemovals);
    }
    
    private void RemoveAppearanceItem(Dictionary<MongoId, double> pool, MongoId item, string label, List<string> removedFrom, List<string> blockedRemovals)
    {
        if (pool.Count == 0)
            return;

        if (pool.ContainsKey(item) && pool.Count == 1)
        {
            blockedRemovals.Add(label);
            return;
        }

        if (pool.Remove(item))
        {
            removedFrom.Add(label);
        }
    }
    
    private void ProcessSeason(SeasonAppearance season, MongoId item, int tier, string seasonName)
    {
        ProcessAppearanceDict(season.PmcUsec, item, tier, $"{seasonName}:USEC");
        ProcessAppearanceDict(season.PmcBear, item, tier, $"{seasonName}:BEAR");
    }
}