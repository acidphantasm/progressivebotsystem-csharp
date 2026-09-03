namespace ProgressiveBotSystem.Services;

using Models;
using Models.Enums;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using Web.Core;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.Preload + 10)]
public class PresetStateService
{
    public PresetStateService()
    {
        Instance = this;
    }

    public static PresetStateService Instance { get; private set; } = null!;

    public Dictionary<string, double> EquipmentWeightChanges { get; } = new();
    public HashSet<string> EquipmentRemovedItems { get; } = new();
    public HashSet<string> EquipmentAddedItems { get; } = new();

    public Dictionary<string, double> AmmoWeightChanges { get; } = new();
    public HashSet<string> AmmoRemovedItems { get; } = new();
    public HashSet<string> AmmoAddedItems { get; } = new();

    public Dictionary<string, double> ChancesWeightChanges { get; } = new();
    public Dictionary<string, double> GenerationWeightChanges { get; } = new();
    public Dictionary<string, double> GenerationWhitelistWeightChanges { get; } = new();
    public HashSet<string> GenerationWhitelistRemovedItems { get; } = new();
    public HashSet<string> GenerationWhitelistAddedItems { get; } = new();

    public Dictionary<string, double> AppearanceWeightChanges { get; } = new();
    public HashSet<string> AppearanceRemovedItems { get; } = new();
    public HashSet<string> AppearanceAddedItems { get; } = new();

    public HashSet<string> PendingChanges { get; } = new();
    public event Action? OnPresetChanged;
    public event Action? OnPresetSaved;
    public event Action? OnPendingChangesUpdated;

    public void RaisePresetChanged() => OnPresetChanged?.Invoke();

    public void RaisePresetSaved() => OnPresetSaved?.Invoke();

    public void RaisePendingChangesUpdated() => OnPendingChangesUpdated?.Invoke();

    public bool HasPendingPresetNameOrFolderChange() =>
        PendingChanges.Contains("_presetName") || PendingChanges.Contains("_presetEnable");

    public bool HasPendingPresetChanges() =>
        EquipmentWeightChanges.Count > 0
        || AmmoWeightChanges.Count > 0
        || ChancesWeightChanges.Count > 0
        || GenerationWeightChanges.Count > 0
        || GenerationWhitelistWeightChanges.Count > 0
        || EquipmentAddedItems.Count > 0
        || EquipmentRemovedItems.Count > 0
        || AmmoAddedItems.Count > 0
        || AmmoRemovedItems.Count > 0
        || GenerationWhitelistAddedItems.Count > 0
        || GenerationWhitelistRemovedItems.Count > 0
        || AppearanceWeightChanges.Count > 0
        || AppearanceAddedItems.Count > 0
        || AppearanceRemovedItems.Count > 0;

    public bool HasAnyPendingChanges() =>
        HasPendingPresetChanges() || HasPendingPresetNameOrFolderChange();

    public void ClearPendingChanges()
    {
        PendingChanges.Clear();
        EquipmentWeightChanges.Clear();
        EquipmentRemovedItems.Clear();
        EquipmentAddedItems.Clear();
        AmmoWeightChanges.Clear();
        AmmoRemovedItems.Clear();
        AmmoAddedItems.Clear();
        ChancesWeightChanges.Clear();
        GenerationWeightChanges.Clear();
        GenerationWhitelistWeightChanges.Clear();
        GenerationWhitelistRemovedItems.Clear();
        GenerationWhitelistAddedItems.Clear();
        AppearanceAddedItems.Clear();
        AppearanceRemovedItems.Clear();
        AppearanceWeightChanges.Clear();
    }

    public void SyncItemChange(
        string key,
        bool added,
        bool removed,
        bool weightChanged,
        double weight,
        HashSet<string> addedSet,
        HashSet<string> removedSet,
        Dictionary<string, double> weightDict,
        string weightSuffix = "_weight"
    )
    {
        var weightKey = key + weightSuffix;

        if (added)
        {
            addedSet.Add(key);
        }
        else
        {
            addedSet.Remove(key);
        }
        if (removed)
        {
            removedSet.Add(key);
        }
        else
        {
            removedSet.Remove(key);
        }
        if (weightChanged)
        {
            weightDict[weightKey] = weight;
        }
        else
        {
            weightDict.Remove(weightKey);
        }

        var wasPendingItem = PendingChanges.Contains(key);
        var wasPendingWeight = PendingChanges.Contains(weightKey);
        var pendingItemNow = added || removed;

        if (wasPendingItem != pendingItemNow)
        {
            Utils.UpdateView(key);
        }
        if (wasPendingWeight != weightChanged)
        {
            Utils.UpdateView(weightKey);
        }
    }

    public void SyncWeightOnlyChange(
        string weightKey,
        bool weightChanged,
        double weight,
        Dictionary<string, double> weightDict
    )
    {
        if (weightChanged)
        {
            weightDict[weightKey] = weight;
        }
        else
        {
            weightDict.Remove(weightKey);
        }

        var wasPendingWeight = PendingChanges.Contains(weightKey);
        if (wasPendingWeight != weightChanged)
        {
            Utils.UpdateView(weightKey);
        }
    }

    public void SyncSlotWeightChange(string slotKey, bool changed, double value)
    {
        if (changed)
        {
            GenerationWeightChanges[slotKey] = value;
        }
        else
        {
            GenerationWeightChanges.Remove(slotKey);
        }

        var pendingKey = slotKey + "_generationSlotWeight";
        var wasPending = PendingChanges.Contains(pendingKey);
        if (wasPending != changed)
        {
            Utils.UpdateView(pendingKey);
        }
    }

    public List<string> BuildChangeSummary(string? presetName, bool? presetEnabled)
    {
        var changeList = new List<string>();

        changeList.AddRange(EquipmentAddedItems.Select(x => $"Equipment Added: {x}"));
        changeList.AddRange(EquipmentRemovedItems.Select(x => $"Equipment Removed: {x}"));
        changeList.AddRange(
            EquipmentWeightChanges.Select(x => $"Equipment Weight Changed: {x.Key}")
        );

        changeList.AddRange(AmmoAddedItems.Select(x => $"Ammo Added: {x}"));
        changeList.AddRange(AmmoRemovedItems.Select(x => $"Ammo Removed: {x}"));
        changeList.AddRange(AmmoWeightChanges.Select(x => $"Ammo Weight Changed: {x.Key}"));

        changeList.AddRange(ChancesWeightChanges.Select(x => $"Chance Weight Changed: {x.Key}"));

        changeList.AddRange(
            GenerationWeightChanges.Select(x => $"Generation Weight Changed: {x.Key}")
        );
        changeList.AddRange(
            GenerationWhitelistWeightChanges.Select(x =>
                $"Generation Whitelist Weight Changed: {x.Key}"
            )
        );
        changeList.AddRange(
            GenerationWhitelistAddedItems.Select(x => $"Generation Whitelist Added: {x}")
        );
        changeList.AddRange(
            GenerationWhitelistRemovedItems.Select(x => $"Generation Whitelist Removed: {x}")
        );

        changeList.AddRange(AppearanceAddedItems.Select(x => $"Appearance Added: {x}"));
        changeList.AddRange(AppearanceRemovedItems.Select(x => $"Appearance Removed: {x}"));
        changeList.AddRange(
            AppearanceWeightChanges.Select(x => $"Appearance Weight Changed: {x.Key}")
        );

        if (PendingChanges.Contains("_presetName"))
        {
            changeList.Add($"Preset Name Changed: {presetName}");
        }
        if (PendingChanges.Contains("_presetEnable"))
        {
            changeList.Add($"Preset Enablement Changed: {presetEnabled}");
        }

        if (changeList.Count == 0)
        {
            changeList.Add("No pending changes detected.");
        }

        return changeList;
    }

    public IEnumerable<PendingSlotWeightChange> GetPendingSlotWeightChanges(
        string tier,
        string botType
    )
    {
        var tierPrefix = $"Tier{tier}_{botType}_";

        foreach (var kvp in GenerationWeightChanges)
        {
            if (!kvp.Key.StartsWith(tierPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var remainder = kvp.Key[tierPrefix.Length..];
            var index = remainder.LastIndexOf('_');
            if (index < 0)
            {
                continue;
            }

            var category = remainder[..index];
            var slotPart = remainder[(index + 1)..];

            if (!int.TryParse(slotPart, out var slotIndex))
            {
                continue;
            }

            yield return new PendingSlotWeightChange(category, slotIndex, kvp.Value);
        }
    }

    public IEnumerable<PendingItemChange> GetPendingItemChanges(
        string tier,
        string botType,
        HashSet<string> addedSet,
        HashSet<string> removedSet,
        Dictionary<string, double> weightDict,
        string weightSuffix
    )
    {
        var tierPrefix = $"Tier{tier}_{botType}_";

        foreach (var key in addedSet)
        {
            if (!TryParse(key, tierPrefix, out var category, out var id))
            {
                continue;
            }
            var weight = weightDict.GetValueOrDefault(key + weightSuffix, 1);
            yield return new PendingItemChange(category, id, PendingItemAction.Add, weight);
        }

        foreach (var key in removedSet)
        {
            if (!TryParse(key, tierPrefix, out var category, out var id))
            {
                continue;
            }
            yield return new PendingItemChange(category, id, PendingItemAction.Remove, 0);
        }

        foreach (var kvp in weightDict)
        {
            if (!kvp.Key.EndsWith(weightSuffix))
            {
                continue;
            }

            var baseKey = kvp.Key[..^weightSuffix.Length];
            if (addedSet.Contains(baseKey))
            {
                continue;
            }

            if (!TryParse(baseKey, tierPrefix, out var category, out var id))
            {
                continue;
            }
            yield return new PendingItemChange(
                category,
                id,
                PendingItemAction.WeightOnly,
                kvp.Value
            );
        }
    }

    private bool TryParse(string key, string tierPrefix, out string category, out string id)
    {
        category = string.Empty;
        id = string.Empty;

        if (!key.StartsWith(tierPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = key[tierPrefix.Length..];
        var index = remainder.LastIndexOf('_');
        if (index < 0)
        {
            return false;
        }

        category = remainder[..index];
        id = remainder[(index + 1)..];
        return true;
    }
}
