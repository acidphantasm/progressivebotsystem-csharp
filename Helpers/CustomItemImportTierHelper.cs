using _progressiveBotSystem.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class CustomItemImportTierHelper(
    DataLoader dataLoader)
{
    /// <summary>
    ///     Used to get and return the correct Equipment Data that was deserialized from either the preset or the default data
    ///     This data gets malformed by the callers, so we'll be getting this every time there is a relevant config or preset change
    ///     Probably not the best way to do this, I'd rather not malform the data but the import process is sub 100 ms right now so who cares
    /// </summary>
    public EquipmentTierData GetEquipmentTierData(int tier) => tier switch
    {
        1 => dataLoader.Tier1EquipmentData,
        2 => dataLoader.Tier2EquipmentData,
        3 => dataLoader.Tier3EquipmentData,
        4 => dataLoader.Tier4EquipmentData,
        5 => dataLoader.Tier5EquipmentData,
        6 => dataLoader.Tier6EquipmentData,
        7 => dataLoader.Tier7EquipmentData,
        _ => throw new ArgumentOutOfRangeException(
            nameof(tier),
            tier,
            "ModConfig - Initial Tier Appearance must be between 1 and 7."
        )
    };
    
    /// <summary>
    ///     Used to get and return the correct Mods Data that was deserialized from either the preset or the default data
    ///     This data gets malformed by the callers, so we'll be getting this every time there is a relevant config or preset change
    ///     Probably not the best way to do this, I'd rather not malform the data but the import process is sub 100 ms right now so who cares
    /// </summary>
    public Dictionary<MongoId,Dictionary<string,HashSet<MongoId>>> GetModsTierData(int tier) => tier switch
    {
        1 => dataLoader.Tier1ModsData,
        2 => dataLoader.Tier2ModsData,
        3 => dataLoader.Tier3ModsData,
        4 => dataLoader.Tier4ModsData,
        5 => dataLoader.Tier5ModsData,
        6 => dataLoader.Tier6ModsData,
        7 => dataLoader.Tier7ModsData,
        _ => throw new ArgumentOutOfRangeException(
            nameof(tier),
            tier,
            "ModConfig - Initial Tier Appearance must be between 1 and 7."
        )
    };
    
    /// <summary>
    ///     Used to get and return the correct Ammo Data that was deserialized from either the preset or the default data
    ///     This data gets malformed by the callers, so we'll be getting this every time there is a relevant config or preset change
    ///     Probably not the best way to do this, I'd rather not malform the data but the import process is sub 100 ms right now so who cares
    /// </summary>
    public AmmoTierData GetAmmoTierData(int tier) => tier switch
    {
        1 => dataLoader.Tier1AmmoData,
        2 => dataLoader.Tier2AmmoData,
        3 => dataLoader.Tier3AmmoData,
        4 => dataLoader.Tier4AmmoData,
        5 => dataLoader.Tier5AmmoData,
        6 => dataLoader.Tier6AmmoData,
        7 => dataLoader.Tier7AmmoData,
        _ => throw new ArgumentOutOfRangeException(
            nameof(tier),
            tier,
            "ModConfig - Initial Tier Appearance must be between 1 and 7."
        )
    };
}