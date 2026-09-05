using System.Collections;
using ProgressiveBotSystem.Helpers;
using ProgressiveBotSystem.Models;
using ProgressiveBotSystem.Models.Enums;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace ProgressiveBotSystem.Services;

using Appearance = Appearance;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.Preload + 10)]
public class PresetCopyService(DataLoader dataLoader, PresetStateService presetState)
{
    private static readonly HashSet<string> _excludedEquipmentCategories = [nameof(ApbsEquipmentSlots.SecuredContainer)];

    public int CopyAmmoBotType(int sourceTier, string sourceBotType, int targetTier, string targetBotType)
    {
        var targetProp = typeof(AmmoTierData).GetProperty(targetBotType);
        if (targetProp == null)
        {
            return 0;
        }

        if (
            targetProp.GetValue(dataLoader.AllTierDataClean.Tiers[targetTier].AmmoData)
            is not Dictionary<string, Dictionary<MongoId, double>> cleanTargetData
        )
        {
            return 0;
        }

        var effectiveSourceData = BuildEffectiveAmmoData(sourceTier, sourceBotType);

        ClearPendingChangesFor(
            targetTier,
            targetBotType,
            presetState.AmmoAddedItems,
            presetState.AmmoRemovedItems,
            presetState.AmmoWeightChanges
        );

        var changeCount = ForceWriteDiff(
            effectiveSourceData,
            cleanTargetData,
            targetTier,
            targetBotType,
            presetState.AmmoAddedItems,
            presetState.AmmoRemovedItems,
            presetState.AmmoWeightChanges
        );

        presetState.RaisePendingChangesUpdated();
        return changeCount;
    }

    public int CopyAmmoBotTypeToAllTiers(int sourceTier, string sourceBotType, string targetBotType)
    {
        var total = 0;
        for (var tier = 1; tier <= 7; tier++)
        {
            if (tier == sourceTier && targetBotType == sourceBotType)
            {
                continue;
            }
            total += CopyAmmoBotType(sourceTier, sourceBotType, tier, targetBotType);
        }
        return total;
    }

    private Dictionary<string, Dictionary<MongoId, double>> BuildEffectiveAmmoData(int tier, string botType)
    {
        var prop = typeof(AmmoTierData).GetProperty(botType);
        if (
            prop == null
            || prop.GetValue(dataLoader.AllTierDataClean.Tiers[tier].AmmoData)
                is not Dictionary<string, Dictionary<MongoId, double>> cleanData
        )
        {
            return new Dictionary<string, Dictionary<MongoId, double>>();
        }

        var effective = cleanData.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value));

        OverlayPending(effective, tier, botType, presetState.AmmoAddedItems, presetState.AmmoRemovedItems, presetState.AmmoWeightChanges);

        return effective;
    }

    public int CopyAppearanceBotType(int sourceTier, string sourceBotType, int targetTier, string targetBotType)
    {
        if (!IsSameFaction(sourceBotType, targetBotType))
        {
            return -1;
        }

        var cleanTargetAppearance = ResolveAppearance(dataLoader.AllTierDataClean.Tiers[targetTier].AppearanceData, targetBotType);
        if (cleanTargetAppearance == null)
        {
            return 0;
        }

        var cleanTargetData = ExtractSlotData(cleanTargetAppearance);
        var effectiveSourceData = BuildEffectiveAppearanceData(sourceTier, sourceBotType);

        ClearPendingChangesFor(
            targetTier,
            targetBotType,
            presetState.AppearanceAddedItems,
            presetState.AppearanceRemovedItems,
            presetState.AppearanceWeightChanges
        );

        var changeCount = ForceWriteDiff(
            effectiveSourceData,
            cleanTargetData,
            targetTier,
            targetBotType,
            presetState.AppearanceAddedItems,
            presetState.AppearanceRemovedItems,
            presetState.AppearanceWeightChanges
        );

        presetState.RaisePendingChangesUpdated();
        return changeCount;
    }

    public int CopyAppearanceBotTypeToAllTiers(int sourceTier, string sourceBotType, string targetBotType)
    {
        if (!IsSameFaction(sourceBotType, targetBotType))
        {
            return -1;
        }

        var total = 0;
        for (var tier = 1; tier <= 7; tier++)
        {
            if (tier == sourceTier && targetBotType == sourceBotType)
            {
                continue;
            }
            var result = CopyAppearanceBotType(sourceTier, sourceBotType, tier, targetBotType);
            if (result > 0)
            {
                total += result;
            }
        }
        return total;
    }

    public bool IsSameFaction(string sourceBot, string targetBot)
    {
        return GetFaction(sourceBot) is { } a && GetFaction(targetBot) is { } b && a == b;
    }

    private static string? GetFaction(string botType)
    {
        if (botType.EndsWith("PmcUsec", StringComparison.OrdinalIgnoreCase))
        {
            return "Usec";
        }
        if (botType.EndsWith("PmcBear", StringComparison.OrdinalIgnoreCase))
        {
            return "Bear";
        }
        return null;
    }

    private static Appearance? ResolveAppearance(AppearanceTierData tierData, string botTypeName)
    {
        Dictionary<string, Appearance>? botAppearances;

        if (botTypeName.Contains('-'))
        {
            var parts = botTypeName.Split('-', 2);
            var seasonName = parts[0];
            var seasonBot = parts[1];

            var season = seasonName.ToLower() switch
            {
                "springearly" => tierData.SpringEarly,
                "spring" => tierData.Spring,
                "summer" => tierData.Summer,
                "autumn" => tierData.Autumn,
                "winter" => tierData.Winter,
                _ => null,
            };
            if (season == null)
            {
                return null;
            }

            botAppearances = seasonBot switch
            {
                "PmcUsec" => season.PmcUsec,
                "PmcBear" => season.PmcBear,
                _ => null,
            };
        }
        else
        {
            botAppearances = botTypeName switch
            {
                "PmcUsec" => tierData.PmcUsec,
                "PmcBear" => tierData.PmcBear,
                _ => null,
            };
        }

        return botAppearances?.Values.FirstOrDefault();
    }

    private static Dictionary<string, Dictionary<MongoId, double>> ExtractSlotData(Appearance app)
    {
        var result = new Dictionary<string, Dictionary<MongoId, double>>();

        void AddSlot(string category, IDictionary<MongoId, double>? dict)
        {
            if (dict == null)
            {
                return;
            }
            result[category] = new Dictionary<MongoId, double>(dict);
        }

        AddSlot("body", app.Body);
        AddSlot("feet", app.Feet);
        AddSlot("hands", app.Hands);
        AddSlot("head", app.Head);

        return result;
    }

    private Dictionary<string, Dictionary<MongoId, double>> BuildEffectiveAppearanceData(int tier, string botType)
    {
        var appearance = ResolveAppearance(dataLoader.AllTierDataClean.Tiers[tier].AppearanceData, botType);
        if (appearance == null)
        {
            return new Dictionary<string, Dictionary<MongoId, double>>();
        }

        var effective = ExtractSlotData(appearance);

        OverlayPending(
            effective,
            tier,
            botType,
            presetState.AppearanceAddedItems,
            presetState.AppearanceRemovedItems,
            presetState.AppearanceWeightChanges
        );

        return effective;
    }

    public int CopyEquipmentBotType(int sourceTier, string sourceBotType, int targetTier, string targetBotType)
    {
        var targetProp = typeof(EquipmentTierData).GetProperty(targetBotType);
        if (targetProp == null)
        {
            return 0;
        }

        if (targetProp.GetValue(dataLoader.AllTierDataClean.Tiers[targetTier].EquipmentData) is not ApbsEquipmentBot cleanTargetBot)
        {
            return 0;
        }

        var cleanTargetData = ExtractEquipmentSlotData(cleanTargetBot);
        var effectiveSourceData = BuildEffectiveEquipmentData(sourceTier, sourceBotType);

        ClearPendingChangesFor(
            targetTier,
            targetBotType,
            presetState.EquipmentAddedItems,
            presetState.EquipmentRemovedItems,
            presetState.EquipmentWeightChanges
        );

        var changeCount = ForceWriteDiff(
            effectiveSourceData,
            cleanTargetData,
            targetTier,
            targetBotType,
            presetState.EquipmentAddedItems,
            presetState.EquipmentRemovedItems,
            presetState.EquipmentWeightChanges
        );

        presetState.RaisePendingChangesUpdated();
        return changeCount;
    }

    public int CopyEquipmentBotTypeToAllTiers(int sourceTier, string sourceBotType, string targetBotType)
    {
        var total = 0;
        for (var tier = 1; tier <= 7; tier++)
        {
            if (tier == sourceTier && targetBotType == sourceBotType)
            {
                continue;
            }
            total += CopyEquipmentBotType(sourceTier, sourceBotType, tier, targetBotType);
        }
        return total;
    }

    private static Dictionary<string, Dictionary<MongoId, double>> ExtractEquipmentSlotData(ApbsEquipmentBot botData)
    {
        var result = new Dictionary<string, Dictionary<MongoId, double>>();

        foreach (var (slot, items) in botData.Equipment)
        {
            var category = slot.ToString();
            if (_excludedEquipmentCategories.Contains(category))
            {
                continue;
            }

            result[category] = new Dictionary<MongoId, double>(items);
        }

        return result;
    }

    private Dictionary<string, Dictionary<MongoId, double>> BuildEffectiveEquipmentData(int tier, string botType)
    {
        var prop = typeof(EquipmentTierData).GetProperty(botType);
        if (prop == null || prop.GetValue(dataLoader.AllTierDataClean.Tiers[tier].EquipmentData) is not ApbsEquipmentBot cleanBot)
        {
            return new Dictionary<string, Dictionary<MongoId, double>>();
        }

        var effective = ExtractEquipmentSlotData(cleanBot);

        OverlayPending(
            effective,
            tier,
            botType,
            presetState.EquipmentAddedItems,
            presetState.EquipmentRemovedItems,
            presetState.EquipmentWeightChanges
        );

        return effective;
    }

    public int CopyChancesBotType(int sourceTier, string sourceBotType, int targetTier, string targetBotType)
    {
        if (targetBotType != sourceBotType)
        {
            return -1;
        }
        var targetBotData = GetBotChancesData(targetTier, targetBotType);
        if (targetBotData?.Chances == null)
        {
            return 0;
        }

        var (_, cleanTargetWhitelist, _) = ExtractChancesData(targetBotData);

        var sourceBotData = GetBotChancesData(sourceTier, sourceBotType);
        var (sourceChancesClean, sourceWhitelistClean, sourceSlotsClean) = ExtractChancesData(sourceBotData);

        var effectiveSourceChances = BuildEffectiveChancesWeights(sourceChancesClean, sourceTier, sourceBotType);
        var effectiveSourceWhitelist = BuildEffectiveGenerationWhitelist(sourceWhitelistClean, sourceTier, sourceBotType);
        var effectiveSourceSlots = BuildEffectiveSlotWeights(sourceSlotsClean, sourceTier, sourceBotType);

        ClearPendingChancesFor(targetTier, targetBotType);

        var changeCount = 0;

        foreach (var (category, items) in effectiveSourceChances)
        {
            foreach (var (id, weight) in items)
            {
                var key = $"Tier{targetTier}_{targetBotType}_{category}_{id}_chancesWeight";
                presetState.SyncWeightOnlyChange(key, true, weight, presetState.ChancesWeightChanges);
                changeCount++;
            }
        }

        changeCount += ForceWriteDiff(
            effectiveSourceWhitelist,
            cleanTargetWhitelist,
            targetTier,
            targetBotType,
            presetState.GenerationWhitelistAddedItems,
            presetState.GenerationWhitelistRemovedItems,
            presetState.GenerationWhitelistWeightChanges,
            "_generationWeight"
        );

        foreach (var (category, slots) in effectiveSourceSlots)
        {
            foreach (var (slotIndex, weight) in slots)
            {
                var slotKey = $"Tier{targetTier}_{targetBotType}_{category}_{slotIndex}";
                presetState.SyncSlotWeightChange(slotKey, true, weight);
                changeCount++;
            }
        }

        presetState.RaisePendingChangesUpdated();
        return changeCount;
    }

    public int CopyChancesBotTypeToAllTiers(int sourceTier, string sourceBotType, string targetBotType)
    {
        if (targetBotType != sourceBotType)
        {
            return -1;
        }
        var total = 0;
        for (var tier = 1; tier <= 7; tier++)
        {
            if (tier == sourceTier && targetBotType == sourceBotType)
            {
                continue;
            }
            total += CopyChancesBotType(sourceTier, sourceBotType, tier, targetBotType);
        }
        return total;
    }

    private BotChancesData? GetBotChancesData(int tier, string botType)
    {
        var prop = typeof(ChancesTierData).GetProperty(botType);
        if (prop == null)
        {
            return null;
        }

        return prop.GetValue(dataLoader.AllTierDataClean.Tiers[tier].ChancesData) as BotChancesData;
    }

    // lol gross
    private static (
        Dictionary<string, Dictionary<string, double>> chancesWeights,
        Dictionary<string, Dictionary<MongoId, double>> generationWhitelist,
        Dictionary<string, Dictionary<double, double>> slotWeights
    ) ExtractChancesData(BotChancesData? botData)
    {
        var chancesWeights = new Dictionary<string, Dictionary<string, double>>();
        var generationWhitelist = new Dictionary<string, Dictionary<MongoId, double>>();
        var slotWeights = new Dictionary<string, Dictionary<double, double>>();

        if (botData?.Chances == null)
        {
            return (chancesWeights, generationWhitelist, slotWeights);
        }

        var chances = botData.Chances;

        foreach (
            var dictProp in typeof(ApbsChances).GetProperties().Where(p => !p.Name.Equals("Generation", StringComparison.OrdinalIgnoreCase))
        )
        {
            if (dictProp.GetValue(chances) is not IDictionary dict)
            {
                continue;
            }

            var items = new Dictionary<string, double>();
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Value == null)
                {
                    continue;
                }
                items[entry.Key.ToString()!] = Convert.ToDouble(entry.Value);
            }

            chancesWeights[dictProp.Name] = items;
        }

        var genItems = chances.Generation.Items;

        foreach (var genProp in typeof(ApbsGenerationWeightingItems).GetProperties())
        {
            if (genProp.GetValue(genItems) is not ApbsGenerationData genData)
            {
                continue;
            }

            var genCategory = genProp.Name;

            if (genData.Whitelist != null)
            {
                generationWhitelist[genCategory] = new Dictionary<MongoId, double>(genData.Whitelist);
            }

            if (genData.Weights != null)
            {
                slotWeights[genCategory] = new Dictionary<double, double>(genData.Weights);
            }
        }

        return (chancesWeights, generationWhitelist, slotWeights);
    }

    private Dictionary<string, Dictionary<string, double>> BuildEffectiveChancesWeights(
        Dictionary<string, Dictionary<string, double>> cleanChances,
        int tier,
        string botType
    )
    {
        var effective = cleanChances.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<string, double>(kvp.Value));

        var prefix = $"Tier{tier}_{botType}_";
        const string suffix = "_chancesWeight";

        foreach (var kvp in presetState.ChancesWeightChanges)
        {
            if (!kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!kvp.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var remainder = kvp.Key[prefix.Length..^suffix.Length];
            var index = remainder.LastIndexOf('_');
            if (index < 0)
            {
                continue;
            }

            var category = remainder[..index];
            var id = remainder[(index + 1)..];

            if (!effective.TryGetValue(category, out var items))
            {
                items = new Dictionary<string, double>();
                effective[category] = items;
            }

            items[id] = kvp.Value;
        }

        return effective;
    }

    private Dictionary<string, Dictionary<MongoId, double>> BuildEffectiveGenerationWhitelist(
        Dictionary<string, Dictionary<MongoId, double>> cleanWhitelist,
        int tier,
        string botType
    )
    {
        var effective = cleanWhitelist.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<MongoId, double>(kvp.Value));

        OverlayPending(
            effective,
            tier,
            botType,
            presetState.GenerationWhitelistAddedItems,
            presetState.GenerationWhitelistRemovedItems,
            presetState.GenerationWhitelistWeightChanges,
            "_generationWeight"
        );

        return effective;
    }

    private Dictionary<string, Dictionary<double, double>> BuildEffectiveSlotWeights(
        Dictionary<string, Dictionary<double, double>> cleanSlots,
        int tier,
        string botType
    )
    {
        var effective = cleanSlots.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<double, double>(kvp.Value));

        foreach (var change in presetState.GetPendingSlotWeightChanges(tier.ToString(), botType))
        {
            if (!effective.TryGetValue(change.Category, out var slots))
            {
                slots = new Dictionary<double, double>();
                effective[change.Category] = slots;
            }

            slots[change.SlotIndex] = change.Weight;
        }

        return effective;
    }

    private void ClearPendingChancesFor(int tier, string botType)
    {
        var prefix = $"Tier{tier}_{botType}_";

        foreach (
            var key in presetState.ChancesWeightChanges.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList()
        )
        {
            presetState.ChancesWeightChanges.Remove(key);
        }

        presetState.GenerationWhitelistAddedItems.RemoveWhere(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        presetState.GenerationWhitelistRemovedItems.RemoveWhere(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        foreach (
            var key in presetState
                .GenerationWhitelistWeightChanges.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList()
        )
        {
            presetState.GenerationWhitelistWeightChanges.Remove(key);
        }

        foreach (
            var key in presetState
                .GenerationWeightChanges.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList()
        )
        {
            presetState.GenerationWeightChanges.Remove(key);
        }

        foreach (var key in presetState.PendingChanges.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            presetState.PendingChanges.Remove(key);
        }
    }

    private void OverlayPending(
        Dictionary<string, Dictionary<MongoId, double>> effective,
        int tier,
        string botType,
        HashSet<string> addedSet,
        HashSet<string> removedSet,
        Dictionary<string, double> weightDict,
        string weightSuffix = "_weight"
    )
    {
        var pendingChanges = presetState.GetPendingItemChanges(tier.ToString(), botType, addedSet, removedSet, weightDict, weightSuffix);

        foreach (var change in pendingChanges)
        {
            if (!effective.TryGetValue(change.Category, out var items))
            {
                items = new Dictionary<MongoId, double>();
                effective[change.Category] = items;
            }

            var id = new MongoId(change.Id);

            switch (change.Action)
            {
                case PendingItemAction.Add:
                case PendingItemAction.WeightOnly:
                    items[id] = change.Weight;
                    break;
                case PendingItemAction.Remove:
                    items.Remove(id);
                    break;
            }
        }
    }

    private int ForceWriteDiff(
        Dictionary<string, Dictionary<MongoId, double>> effectiveSourceData,
        Dictionary<string, Dictionary<MongoId, double>> cleanTargetData,
        int targetTier,
        string targetBotType,
        HashSet<string> addedSet,
        HashSet<string> removedSet,
        Dictionary<string, double> weightDict,
        string weightSuffix = "_weight"
    )
    {
        var changeCount = 0;
        var touchedIds = new HashSet<(string category, MongoId id)>();

        foreach (var (category, items) in effectiveSourceData)
        {
            foreach (var (id, weight) in items)
            {
                touchedIds.Add((category, id));

                var targetHasId = cleanTargetData.TryGetValue(category, out var targetItems) && targetItems.ContainsKey(id);

                var key = $"Tier{targetTier}_{targetBotType}_{category}_{id}";

                presetState.SyncItemChange(key, !targetHasId, false, true, weight, addedSet, removedSet, weightDict, weightSuffix);

                changeCount++;
            }
        }

        foreach (var (category, targetItems) in cleanTargetData)
        {
            foreach (var id in targetItems.Keys)
            {
                if (touchedIds.Contains((category, id)))
                {
                    continue;
                }

                var key = $"Tier{targetTier}_{targetBotType}_{category}_{id}";

                presetState.SyncItemChange(key, false, true, false, 0, addedSet, removedSet, weightDict, weightSuffix);

                changeCount++;
            }
        }

        return changeCount;
    }

    private void ClearPendingChangesFor(
        int tier,
        string botType,
        HashSet<string> addedSet,
        HashSet<string> removedSet,
        Dictionary<string, double> weightDict
    )
    {
        var prefix = $"Tier{tier}_{botType}_";

        addedSet.RemoveWhere(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        removedSet.RemoveWhere(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        foreach (var key in weightDict.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            weightDict.Remove(key);
        }

        foreach (var key in presetState.PendingChanges.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            presetState.PendingChanges.Remove(key);
        }
    }
}
