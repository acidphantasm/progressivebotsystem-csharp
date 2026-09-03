namespace ProgressiveBotSystem.Services;

using System.Collections;
using Helpers;
using Models;
using Models.Enums;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.Preload + 10)]
public class PresetApplicationService(DataLoader dataLoader, PresetStateService presetState)
{
    private static readonly Dictionary<string, ApbsEquipmentSlots> StringToSlot = new()
    {
        { "Headwear", ApbsEquipmentSlots.Headwear },
        { "Earpiece", ApbsEquipmentSlots.Earpiece },
        { "FaceCover", ApbsEquipmentSlots.FaceCover },
        { "ArmorVest", ApbsEquipmentSlots.ArmorVest },
        { "Eyewear", ApbsEquipmentSlots.Eyewear },
        { "ArmBand", ApbsEquipmentSlots.ArmBand },
        { "TacticalVest", ApbsEquipmentSlots.TacticalVest },
        { "Pockets", ApbsEquipmentSlots.Pockets },
        { "Backpack", ApbsEquipmentSlots.Backpack },
        { "SecuredContainer", ApbsEquipmentSlots.SecuredContainer },
        { "FirstPrimaryWeapon_LongRange", ApbsEquipmentSlots.FirstPrimaryWeapon_LongRange },
        { "FirstPrimaryWeapon_ShortRange", ApbsEquipmentSlots.FirstPrimaryWeapon_ShortRange },
        { "SecondPrimaryWeapon_LongRange", ApbsEquipmentSlots.SecondPrimaryWeapon_LongRange },
        { "SecondPrimaryWeapon_ShortRange", ApbsEquipmentSlots.SecondPrimaryWeapon_ShortRange },
        { "Holster", ApbsEquipmentSlots.Holster },
        { "Scabbard", ApbsEquipmentSlots.Scabbard },
        { "ArmouredRig", ApbsEquipmentSlots.ArmouredRig },
    };

    public void ApplyPresetChanges()
    {
        for (var tier = 1; tier <= 7; tier++)
        {
            var tierIndex = tier.ToString();
            var tierData = dataLoader.AllTierDataClean.Tiers[tier];

            ApplyEquipmentChangesToTier(tierData.EquipmentData, tierIndex);
            ApplyAmmoChangesToTier(tierData.AmmoData, tierIndex);
            ApplyChancesChangesToTier(tierData.ChancesData, tierIndex);
            ApplyAppearanceChangesToTier(tierData.AppearanceData, tierIndex);
        }
    }

    private void ApplyEquipmentChangesToTier(EquipmentTierData tier, string tierIndex)
    {
        foreach (var prop in typeof(EquipmentTierData).GetProperties())
        {
            var botName = prop.Name;
            var botData = (ApbsEquipmentBot)prop.GetValue(tier)!;

            void ApplyToEquipment(string key, Action<Dictionary<MongoId, double>, MongoId> action)
            {
                var parts = key.Split('_');
                if (parts.Length < 4)
                {
                    return;
                }

                var keyTier = parts[0];
                var keyBot = parts[1];
                var idStr = parts[^1];
                var categoryStr = string.Join("_", parts[2..^1]);

                if (!keyTier.Equals($"Tier{tierIndex}", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (!keyBot.Equals(botName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (!StringToSlot.TryGetValue(categoryStr, out var slot))
                {
                    return;
                }

                var equipmentDict = botData.Equipment;
                if (!equipmentDict.TryGetValue(slot, out var items))
                {
                    items = new Dictionary<MongoId, double>();
                    equipmentDict[slot] = items;
                }

                action(items, new MongoId(idStr));
            }

            foreach (var key in presetState.EquipmentRemovedItems)
            {
                ApplyToEquipment(key, (items, id) => items.Remove(id));
            }

            foreach (var key in presetState.EquipmentAddedItems)
            {
                var weightKey = key + "_weight";
                ApplyToEquipment(
                    key,
                    (items, id) =>
                    {
                        if (
                            presetState.EquipmentWeightChanges.TryGetValue(
                                weightKey,
                                out var weight
                            )
                        )
                        {
                            items[id] = weight;
                            presetState.EquipmentWeightChanges.Remove(weightKey);
                        }
                        else
                        {
                            items[id] = 1;
                        }
                    }
                );
            }

            foreach (var kvp in presetState.EquipmentWeightChanges.ToList())
            {
                if (!kvp.Key.EndsWith("_weight"))
                {
                    continue;
                }
                var baseKey = kvp.Key[..^"_weight".Length];
                ApplyToEquipment(baseKey, (items, id) => items[id] = kvp.Value);
            }
        }
    }

    private void ApplyAmmoChangesToTier(AmmoTierData tier, string tierIndex)
    {
        foreach (var prop in typeof(AmmoTierData).GetProperties())
        {
            var botName = prop.Name;
            var botData = (Dictionary<string, Dictionary<MongoId, double>>)prop.GetValue(tier)!;

            void ApplyToAmmo(string key, Action<Dictionary<MongoId, double>, MongoId> action)
            {
                var parts = key.Split('_');
                if (parts.Length < 4)
                {
                    return;
                }

                var keyTier = parts[0];
                var keyBot = parts[1];
                var idStr = parts[^1];
                var category = string.Join("_", parts[2..^1]);

                if (!keyTier.Equals($"Tier{tierIndex}", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (!keyBot.Equals(botName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (!botData.TryGetValue(category, out var items))
                {
                    items = new Dictionary<MongoId, double>();
                    botData[category] = items;
                }

                action(items, new MongoId(idStr));
            }

            foreach (var key in presetState.AmmoRemovedItems)
            {
                ApplyToAmmo(key, (items, id) => items.Remove(id));
            }

            foreach (var key in presetState.AmmoAddedItems)
            {
                var weightKey = key + "_weight";
                ApplyToAmmo(
                    key,
                    (items, id) =>
                    {
                        if (presetState.AmmoWeightChanges.TryGetValue(weightKey, out var weight))
                        {
                            items[id] = weight;
                            presetState.AmmoWeightChanges.Remove(weightKey);
                        }
                        else
                        {
                            items[id] = 1;
                        }
                    }
                );
            }

            foreach (var kvp in presetState.AmmoWeightChanges.ToList())
            {
                if (!kvp.Key.EndsWith("_weight"))
                {
                    continue;
                }
                var baseKey = kvp.Key[..^"_weight".Length];
                ApplyToAmmo(baseKey, (items, id) => items[id] = kvp.Value);
            }
        }
    }

    private void ApplyChancesChangesToTier(ChancesTierData tierData, string tier)
    {
        foreach (var botProp in typeof(ChancesTierData).GetProperties())
        {
            var botType = botProp.Name;
            var botData = botProp.GetValue(tierData) as BotChancesData;
            if (botData?.Chances == null)
            {
                continue;
            }

            var chances = botData.Chances;

            void ApplyWeightChanges(
                IDictionary dict,
                string dictName,
                IEnumerable<KeyValuePair<string, double>> changes,
                string suffix = ""
            )
            {
                foreach (var kvp in changes)
                {
                    var key = kvp.Key;
                    if (!string.IsNullOrEmpty(suffix) && key.EndsWith(suffix))
                    {
                        key = key[..^suffix.Length];
                    }

                    var parts = key.Split('_', 4);
                    if (parts.Length != 4)
                    {
                        continue;
                    }
                    if (parts[0] != $"Tier{tier}" || parts[1] != botType)
                    {
                        continue;
                    }

                    var category = parts[2];
                    var id = parts[3];

                    if (!string.Equals(dictName, category, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (dict is Dictionary<MongoId, double>)
                    {
                        dict[new MongoId(id)] = kvp.Value;
                    }
                    else
                    {
                        dict[id] = kvp.Value;
                    }
                }
            }

            foreach (
                var dictProp in typeof(ApbsChances)
                    .GetProperties()
                    .Where(p => !p.Name.Equals("Generation", StringComparison.OrdinalIgnoreCase))
            )
            {
                if (dictProp.GetValue(chances) is not IDictionary dict)
                {
                    continue;
                }
                ApplyWeightChanges(
                    dict,
                    dictProp.Name,
                    presetState.ChancesWeightChanges,
                    "_chancesWeight"
                );
            }

            var genItems = chances.Generation?.Items;
            if (genItems == null)
            {
                continue;
            }

            foreach (var genProp in typeof(ApbsGenerationWeightingItems).GetProperties())
            {
                if (genProp.GetValue(genItems) is not ApbsGenerationData genData)
                {
                    continue;
                }

                var genCategory = genProp.Name;

                foreach (
                    var key in presetState.GenerationWhitelistAddedItems.Concat(
                        presetState.GenerationWhitelistRemovedItems
                    )
                )
                {
                    var parts = key.Split('_', 4);
                    if (
                        parts.Length != 4
                        || parts[0] != $"Tier{tier}"
                        || parts[1] != botType
                        || parts[2] != genCategory
                    )
                    {
                        continue;
                    }

                    var mongoId = new MongoId(parts[3]);

                    if (presetState.GenerationWhitelistAddedItems.Contains(key))
                    {
                        if (
                            presetState.GenerationWhitelistWeightChanges.TryGetValue(
                                key + "_generationWeight",
                                out var weight
                            )
                        )
                        {
                            genData.Whitelist[mongoId] = weight;
                            presetState.GenerationWhitelistWeightChanges.Remove(
                                key + "_generationWeight"
                            );
                        }
                        else
                        {
                            genData.Whitelist[mongoId] = 1;
                        }
                    }
                    else if (presetState.GenerationWhitelistRemovedItems.Contains(key))
                    {
                        genData.Whitelist.Remove(mongoId);
                    }
                }

                ApplyWeightChanges(
                    genData.Whitelist,
                    genCategory,
                    presetState.GenerationWhitelistWeightChanges,
                    "_generationWeight"
                );

                for (var slotIndex = 0; slotIndex < 8; slotIndex++)
                {
                    var slotKey = $"Tier{tier}_{botType}_{genCategory}_{slotIndex}";
                    var value =
                        presetState.GenerationWeightChanges.TryGetValue(
                            slotKey,
                            out var changedValue
                        )
                            ? changedValue
                        : genData.Weights.TryGetValue(slotIndex, out var existingValue)
                            ? existingValue
                        : 0;

                    genData.Weights[slotIndex] = value;
                }
            }
        }
    }

    private void ApplyAppearanceChangesToTier(AppearanceTierData tierData, string tierIndex)
    {
        void ApplyToAppearance(string key, Action<Dictionary<MongoId, double>, MongoId> action)
        {
            var parts = key.Split('_');
            if (parts.Length < 4)
            {
                return;
            }

            var keyTier = parts[0];
            var botType = parts[1];
            var category = parts[2];
            var idStr = parts[3];

            if (!keyTier.Equals($"Tier{tierIndex}", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (category.Equals("voice", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var mongoId = new MongoId(idStr);
            Dictionary<MongoId, double>? dict;

            Dictionary<string, Appearance>? GetBotAppearances() =>
                botType switch
                {
                    "PmcUsec" => tierData.PmcUsec,
                    "PmcBear" => tierData.PmcBear,
                    _ => null,
                };

            if (botType.Contains('-'))
            {
                var seasonParts = botType.Split('-', 2);
                var seasonName = seasonParts[0];
                var seasonBot = seasonParts[1];

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
                    return;
                }

                var botAppearances = seasonBot switch
                {
                    "PmcUsec" => season.PmcUsec,
                    "PmcBear" => season.PmcBear,
                    _ => null,
                };
                if (botAppearances?.Values.FirstOrDefault() is not { } app)
                {
                    return;
                }

                dict = category.ToLower() switch
                {
                    "body" => app.Body,
                    "feet" => app.Feet,
                    "hands" => app.Hands,
                    "head" => app.Head,
                    _ => null,
                };
            }
            else
            {
                var botAppearances = GetBotAppearances();
                if (botAppearances?.Values.FirstOrDefault() is not { } app)
                {
                    return;
                }

                dict = category.ToLower() switch
                {
                    "body" => app.Body,
                    "feet" => app.Feet,
                    "hands" => app.Hands,
                    "head" => app.Head,
                    _ => null,
                };
            }

            if (dict != null)
            {
                action(dict, mongoId);
            }
        }

        foreach (var key in presetState.AppearanceRemovedItems)
        {
            ApplyToAppearance(key, (dict, id) => dict.Remove(id));
        }

        foreach (var key in presetState.AppearanceAddedItems)
        {
            ApplyToAppearance(
                key,
                (dict, id) =>
                {
                    var weight = presetState.AppearanceWeightChanges.TryGetValue(
                        key + "_weight",
                        out var w
                    )
                        ? w
                        : 1;
                    dict[id] = weight;
                    presetState.AppearanceWeightChanges.Remove(key + "_weight");
                }
            );
        }

        foreach (var kvp in presetState.AppearanceWeightChanges)
        {
            if (!kvp.Key.EndsWith("_weight"))
            {
                continue;
            }
            var baseKey = kvp.Key[..^"_weight".Length];
            ApplyToAppearance(
                baseKey,
                (dict, id) =>
                {
                    if (dict.ContainsKey(id))
                    {
                        dict[id] = kvp.Value;
                    }
                }
            );
        }
    }
}
