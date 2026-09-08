using System.Text.Json;
using System.Text.RegularExpressions;
using ProgressiveBotSystem.Models;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace ProgressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class BotLogHelper(ItemHelper itemHelper, TierHelper tierHelper)
{
    private static readonly Regex _htmlTagRegex = new("<[^>]+>");
    private readonly List<MongoId> _grenadeList =
    [
        "5448be9a4bdc2dfd2f8b456a",
        "5710c24ad2720bc3458b45a3",
        "58d3db5386f77426186285a0",
        "5e32f56fcb6d5863cc5e5ee4",
        "5e340dcdcb6d5863cc5e5efb",
        "617fd91e5539a84ec44ce155",
        "618a431df1eb8e24b8741deb",
        "66dae7cbeb28f0f96809f325",
    ];

    public BotLogData GetBotDetails(BotBase? botBase, bool isApbsBot)
    {
        if (botBase?.Info is null || botBase.Inventory?.Items is null)
        {
            return new BotLogData();
        }

        var botInfo = botBase.Info;
        var botInventory = botBase.Inventory.Items;

        var botLogData = new BotLogData
        {
            Timestamp = DateTime.Now,
            Role = botInfo.Settings?.Role ?? "Unknown",
            Name = botInfo.Nickname ?? "Unknown",
            Level = botInfo.Level ?? 0,
            Difficulty = botInfo.Settings?.BotDifficulty ?? "Unknown",
            GameVersion = string.IsNullOrWhiteSpace(botInfo.GameVersion) ? null : botInfo.GameVersion,
            PrestigeLevel = botInfo.PrestigeLevel ?? 0,
            DogTagId = GetId(GetSlot(botInventory, "Dogtag")),
            GrenadeCount = GetGrenadeCount(botInventory),
        };

        SetTierInformation(botLogData, botInfo, isApbsBot);
        SetWeapons(botLogData, botInventory);
        SetEquipment(botLogData, botInventory);
        SetArmor(botLogData, botInventory);

        return botLogData;
    }

    private void SetTierInformation(BotLogData botLogData, Info? botInfo, bool isApbsBot)
    {
        if (!isApbsBot || botInfo is null)
        {
            return;
        }

        if (!botInfo.TryGetExtensionData(out var extensionData))
        {
            return;
        }

        if (
            extensionData == null
            || !extensionData.TryGetValue("Tier", out var tierElement)
            || tierElement is not JsonElement { ValueKind: JsonValueKind.Number } jsonElement
        )
        {
            return;
        }

        botLogData.Tier = jsonElement.GetInt32();
        botLogData.PovertyBot = tierHelper.GetTierByLevel(botLogData.Level) != botLogData.Tier;
    }

    private void SetWeapons(BotLogData botLogData, IEnumerable<Item> botInventory)
    {
        botLogData.Primary = GetWeapon(GetSlot(botInventory, "FirstPrimaryWeapon"), botInventory);
        botLogData.Secondary = GetWeapon(GetSlot(botInventory, "SecondPrimaryWeapon"), botInventory);
        botLogData.Holster = GetWeapon(GetSlot(botInventory, "Holster"), botInventory);
        botLogData.Melee = GetWeapon(GetSlot(botInventory, "Scabbard"), botInventory);
    }

    private void SetEquipment(BotLogData botLogData, IEnumerable<Item> botInventory)
    {
        botLogData.Helmet = GetEquipment(GetSlot(botInventory, "Headwear"));

        var nightVision = GetSlot(botInventory, "mod_nvg");

        if (nightVision?.Upd != null)
        {
            botLogData.NightVision = GetEquipment(nightVision);
        }

        botLogData.EarPiece = GetEquipment(GetSlot(botInventory, "Earpiece"));
    }

    private void SetArmor(BotLogData botLogData, IEnumerable<Item> botInventory)
    {
        var vestItem = GetSlot(botInventory, "ArmorVest") ?? GetSlot(botInventory, "TacticalVest");

        if (vestItem == null)
        {
            return;
        }

        var vestInformation = itemHelper.GetItem(vestItem.Template).Value;

        var armor = new BotArmorLog { Id = GetId(vestItem) ?? string.Empty, Name = GetName(vestItem) ?? "Unknown" };

        if (vestInformation?.Properties?.Slots?.Any() == true)
        {
            armor.FrontPlateClass = GetPlateClass(GetPlate(botInventory, "Front_plate", vestItem.Id));
            armor.BackPlateClass = GetPlateClass(GetPlate(botInventory, "Back_plate", vestItem.Id));
            armor.LeftPlateClass = GetPlateClass(GetPlate(botInventory, "Left_side_plate", vestItem.Id));
            armor.RightPlateClass = GetPlateClass(GetPlate(botInventory, "Right_side_plate", vestItem.Id));
        }

        botLogData.Armor = armor;
    }

    private BotWeaponLog? GetWeapon(Item? weapon, IEnumerable<Item> botInventory)
    {
        if (weapon == null)
        {
            return null;
        }

        var caliber = GetCaliber(weapon, botInventory);

        return new BotWeaponLog
        {
            Id = GetId(weapon) ?? string.Empty,
            Name = GetName(weapon) ?? "Unknown",
            CaliberId = GetId(caliber),
            Caliber = GetName(caliber),
        };
    }

    private BotEquipmentLog? GetEquipment(Item? item)
    {
        return item == null ? null : new BotEquipmentLog { Id = GetId(item) ?? string.Empty, Name = GetName(item) ?? "Unknown" };
    }

    private Item? GetSlot(IEnumerable<Item> botInventory, string slotId)
    {
        return botInventory.FirstOrDefault(i => i.SlotId == slotId);
    }

    private Item? GetCaliber(Item weapon, IEnumerable<Item> botInventory)
    {
        return botInventory.FirstOrDefault(i => i.SlotId == "patron_in_weapon" && i.ParentId != null && i.ParentId == weapon.Id);
    }

    private Item? GetPlate(IEnumerable<Item> botInventory, string slotId, string parentId)
    {
        return botInventory.FirstOrDefault(i => i.SlotId == slotId && i.ParentId == parentId);
    }

    private string? GetId(Item? item)
    {
        return item?.Template.ToString();
    }

    private string? GetName(Item? item)
    {
        return item == null ? null : CleanName(itemHelper.GetItemName(item.Template));
    }

    private static string? CleanName(string? name)
    {
        // Mainly because some mods, including my own add color tags to items
        return string.IsNullOrWhiteSpace(name) ? name : _htmlTagRegex.Replace(name, string.Empty);
    }

    private int? GetPlateClass(Item? plate)
    {
        if (plate == null)
        {
            return null;
        }

        var item = itemHelper.GetItem(plate.Template).Value;
        return item?.Properties?.ArmorClass;
    }

    private int GetGrenadeCount(IEnumerable<Item> botInventory)
    {
        return botInventory.Count(i => _grenadeList.Contains(i.Template));
    }
}
