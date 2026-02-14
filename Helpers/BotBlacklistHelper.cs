using _progressiveBotSystem.Globals;
using SPTarkov.DI.Annotations;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class BotBlacklistHelper
{
    /// <summary>
    ///     Used to get and return the correct weapon blacklist data that was deserialized from either the preset or the default data
    /// </summary>
    public List<string> GetWeaponBlacklistTierData(int tier) => tier switch
    {
        1 => ModConfig.Blacklist.WeaponBlacklist.Tier1Blacklist,
        2 => ModConfig.Blacklist.WeaponBlacklist.Tier2Blacklist,
        3 => ModConfig.Blacklist.WeaponBlacklist.Tier3Blacklist,
        4 => ModConfig.Blacklist.WeaponBlacklist.Tier4Blacklist,
        5 => ModConfig.Blacklist.WeaponBlacklist.Tier5Blacklist,
        6 => ModConfig.Blacklist.WeaponBlacklist.Tier6Blacklist,
        7 => ModConfig.Blacklist.WeaponBlacklist.Tier7Blacklist,
        _ => throw new ArgumentOutOfRangeException(
            nameof(tier),
            tier,
            "ModConfig - Weapon Blacklist doesn't exist. Report this."
        )
    };
    
    /// <summary>
    ///     Used to get and return the correct equipment blacklist data that was deserialized from either the preset or the default data
    /// </summary>
    public List<string> GetEquipmentBlacklistTierData(int tier) => tier switch
    {
        1 => ModConfig.Blacklist.EquipmentBlacklist.Tier1Blacklist,
        2 => ModConfig.Blacklist.EquipmentBlacklist.Tier2Blacklist,
        3 => ModConfig.Blacklist.EquipmentBlacklist.Tier3Blacklist,
        4 => ModConfig.Blacklist.EquipmentBlacklist.Tier4Blacklist,
        5 => ModConfig.Blacklist.EquipmentBlacklist.Tier5Blacklist,
        6 => ModConfig.Blacklist.EquipmentBlacklist.Tier6Blacklist,
        7 => ModConfig.Blacklist.EquipmentBlacklist.Tier7Blacklist,
        _ => throw new ArgumentOutOfRangeException(
            nameof(tier),
            tier,
            "ModConfig - Equipment Blacklist doesn't exist. Report this."
        )
    };
    
    /// <summary>
    ///     Used to get and return the correct ammo blacklist data that was deserialized from either the preset or the default data
    /// </summary>
    public List<string> GetAmmoBlacklistTierData(int tier) => tier switch
    {
        1 => ModConfig.Blacklist.AmmoBlacklist.Tier1Blacklist,
        2 => ModConfig.Blacklist.AmmoBlacklist.Tier2Blacklist,
        3 => ModConfig.Blacklist.AmmoBlacklist.Tier3Blacklist,
        4 => ModConfig.Blacklist.AmmoBlacklist.Tier4Blacklist,
        5 => ModConfig.Blacklist.AmmoBlacklist.Tier5Blacklist,
        6 => ModConfig.Blacklist.AmmoBlacklist.Tier6Blacklist,
        7 => ModConfig.Blacklist.AmmoBlacklist.Tier7Blacklist,
        _ => throw new ArgumentOutOfRangeException(
            nameof(tier),
            tier,
            "ModConfig - Ammo Blacklist doesn't exist. Report this."
        )
    };
    
    /// <summary>
    ///     Used to get and return the correct attachment blacklist data that was deserialized from either the preset or the default data
    /// </summary>
    public List<string> GetAttachmentBlacklistTierData(int tier) => tier switch
    {
        1 => ModConfig.Blacklist.AttachmentBlacklist.Tier1Blacklist,
        2 => ModConfig.Blacklist.AttachmentBlacklist.Tier2Blacklist,
        3 => ModConfig.Blacklist.AttachmentBlacklist.Tier3Blacklist,
        4 => ModConfig.Blacklist.AttachmentBlacklist.Tier4Blacklist,
        5 => ModConfig.Blacklist.AttachmentBlacklist.Tier5Blacklist,
        6 => ModConfig.Blacklist.AttachmentBlacklist.Tier6Blacklist,
        7 => ModConfig.Blacklist.AttachmentBlacklist.Tier7Blacklist,
        _ => throw new ArgumentOutOfRangeException(
            nameof(tier),
            tier,
            "ModConfig - Attachment Blacklist doesn't exist. Report this."
        )
    };
    
    /// <summary>
    ///     Used to get and return the correct clothing blacklist data that was deserialized from either the preset or the default data
    /// </summary>
    public List<string> GetClothingBlacklistTierData(int tier) => tier switch
    {
        1 => ModConfig.Blacklist.ClothingBlacklist.Tier1Blacklist,
        2 => ModConfig.Blacklist.ClothingBlacklist.Tier2Blacklist,
        3 => ModConfig.Blacklist.ClothingBlacklist.Tier3Blacklist,
        4 => ModConfig.Blacklist.ClothingBlacklist.Tier4Blacklist,
        5 => ModConfig.Blacklist.ClothingBlacklist.Tier5Blacklist,
        6 => ModConfig.Blacklist.ClothingBlacklist.Tier6Blacklist,
        7 => ModConfig.Blacklist.ClothingBlacklist.Tier7Blacklist,
        _ => throw new ArgumentOutOfRangeException(
            nameof(tier),
            tier,
            "ModConfig - Clothing Blacklist doesn't exist. Report this."
        )
    };
}