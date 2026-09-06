using ProgressiveBotSystem.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace ProgressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class ItemImportTierHelper(DataLoader dataLoader)
{
    /// <summary>
    ///     Used to get and return the correct Equipment Tier Data that was deserialized from either the preset or the default
    ///     data
    ///     This data gets malformed by the callers, so we'll be getting this every time there is a relevant config or preset
    ///     change
    ///     Probably not the best way to do this, I'd rather not malform the data but the import process is sub 100 ms right
    ///     now so who cares
    /// </summary>
    public EquipmentTierData GetEquipmentTierData(int tier)
    {
        return !dataLoader.AllTierDataDirty.Tiers.TryGetValue(tier, out var tierData)
            ? throw new ArgumentOutOfRangeException(nameof(tier), tier, "ModConfig - Initial Tier Appearance must be between 1 and 7.")
            : tierData.EquipmentData;
    }

    /// <summary>
    ///     Used to get and return the correct Mods Data that was deserialized from either the preset or the default data
    ///     This data gets malformed by the callers, so we'll be getting this every time there is a relevant config or preset
    ///     change
    ///     Probably not the best way to do this, I'd rather not malform the data but the import process is sub 100 ms right
    ///     now so who cares
    /// </summary>
    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> GetModsTierData(int tier)
    {
        return !dataLoader.AllTierDataDirty.Tiers.TryGetValue(tier, out var tierData)
            ? throw new ArgumentOutOfRangeException(nameof(tier), tier, "ModConfig - Initial Tier Appearance must be between 1 and 7.")
            : tierData.ModsData;
    }

    /// <summary>
    ///     Used to get and return the correct Ammo Tier Data that was deserialized from either the preset or the default data
    ///     This data gets malformed by the callers, so we'll be getting this every time there is a relevant config or preset
    ///     change
    ///     Probably not the best way to do this, I'd rather not malform the data but the import process is sub 100 ms right
    ///     now so who cares
    /// </summary>
    public AmmoTierData GetAmmoTierData(int tier)
    {
        return !dataLoader.AllTierDataDirty.Tiers.TryGetValue(tier, out var tierData)
            ? throw new ArgumentOutOfRangeException(nameof(tier), tier, "ModConfig - Initial Tier Appearance must be between 1 and 7.")
            : tierData.AmmoData;
    }

    /// <summary>
    ///     Used to get and return the correct Appearance Tier Data that was deserialized from either the preset or the default
    ///     data
    ///     This data gets malformed by the callers, so we'll be getting this every time there is a relevant config or preset
    ///     change
    ///     Probably not the best way to do this, I'd rather not malform the data but the import process is sub 100 ms right
    ///     now so who cares
    /// </summary>
    public AppearanceTierData GetAppearanceTierData(int tier)
    {
        return !dataLoader.AllTierDataDirty.Tiers.TryGetValue(tier, out var tierData)
            ? throw new ArgumentOutOfRangeException(nameof(tier), tier, "ModConfig - Initial Tier Appearance must be between 1 and 7.")
            : tierData.AppearanceData;
    }
}
