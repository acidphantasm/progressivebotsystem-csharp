using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class BotLogHelper(
    ApbsLogger apbsLogger,
    ItemHelper itemHelper)
{
    private List<MongoId> _grenadeList =
    [
        "5710c24ad2720bc3458b45a3",
        "58d3db5386f77426186285a0",
        "618a431df1eb8e24b8741deb",
        "5448be9a4bdc2dfd2f8b456a",
        "5e32f56fcb6d5863cc5e5ee4",
        "5e340dcdcb6d5863cc5e5efb",
        "617fd91e5539a84ec44ce155"
    ];
    
    public BotLogData GetBotDetails(BotBase? botBase)
    {
        if (botBase is null || botBase.Info is null || botBase.Inventory?.Items is null) return new BotLogData();
        
        var returnValue = new BotLogData();

        var botInfo = botBase.Info;
        var botInventory = botBase.Inventory.Items;

        // Bot Information
        var extensionData = botBase.Info.GetExtensionData();
        returnValue.Tier = extensionData.TryGetValue("Tier", out var tier) && tier is int t ? t : 0;
        returnValue.Role = botInfo.Settings?.Role ?? "Unknown";
        returnValue.Name = botInfo.Nickname ?? "Unknown";
        returnValue.Level = botInfo.Level ?? 0;
        returnValue.Difficulty = botInfo.Settings?.BotDifficulty ?? "Unknown";
        returnValue.GameVersion = string.IsNullOrWhiteSpace(botInfo.GameVersion) ? "Unknown" : botInfo.GameVersion;
        returnValue.PrestigeLevel = botInfo.PrestigeLevel ?? 0;
        
        // Weapon Information
        var primaryWeapon = botInventory.FirstOrDefault(i => i.SlotId == "FirstPrimaryWeapon");
        var secondaryWeapon = botInventory.FirstOrDefault(i => i.SlotId == "SecondPrimaryWeapon");
        var holsterWeapon = botInventory.FirstOrDefault(i => i.SlotId == "Holster");
        var scabbardWeapon  = botInventory.FirstOrDefault(i => i.SlotId == "Scabbard");

        returnValue.PrimaryWeaponId = Tpl(primaryWeapon);
        returnValue.SecondaryWeaponId = Tpl(secondaryWeapon);
        returnValue.HolsterWeaponId = Tpl(holsterWeapon);
        returnValue.ScabbardId = Tpl(scabbardWeapon);

        returnValue.PrimaryWeaponCaliber = Tpl(botInventory.FirstOrDefault(i => i is { SlotId: "patron_in_weapon", ParentId: not null } && i.ParentId == primaryWeapon?.Id));
        returnValue.SecondaryWeaponCaliber = Tpl(botInventory.FirstOrDefault(i => i is { SlotId: "patron_in_weapon", ParentId: not null } && i.ParentId == secondaryWeapon?.Id));
        returnValue.HolsterWeaponCaliber = Tpl(botInventory.FirstOrDefault(i => i is { SlotId: "patron_in_weapon", ParentId: not null } && i.ParentId == holsterWeapon?.Id));
        
        // Equipment Information
        returnValue.HelmetId = Tpl(botInventory.FirstOrDefault(i => i.SlotId == "Headwear"));
        returnValue.NightVisionId = Tpl(botInventory.FirstOrDefault(i => i.SlotId == "mod_nvg" && i.Upd != null));
        returnValue.EarPieceId = Tpl(botInventory.FirstOrDefault(i => i.SlotId == "Earpiece"));
        
        // Fun Armour logic stuff
        var vestItem = botInventory.FirstOrDefault(i => i.SlotId == "ArmorVest")
                       ?? botInventory.FirstOrDefault(i => i.SlotId == "TacticalVest");

        returnValue.ArmourVestId = Tpl(vestItem);
        
        var vestInformation = vestItem != null ? itemHelper.GetItem(vestItem.Template).Value : null;
        if (vestInformation?.Properties?.Slots?.Any() == true)
        {
            returnValue.CanHavePlates = true;
            returnValue.FrontPlateId = Tpl(botInventory.FirstOrDefault(i => i.SlotId == "Front_plate" && i.ParentId == vestItem.Id));
            returnValue.BackPlateId = Tpl(botInventory.FirstOrDefault(i => i.SlotId == "Back_plate" && i.ParentId == vestItem.Id));
            returnValue.LeftSidePlateId = Tpl(botInventory.FirstOrDefault(i => i.SlotId == "Left_side_plate" && i.ParentId == vestItem.Id));
            returnValue.RightSidePlateId = Tpl(botInventory.FirstOrDefault(i => i.SlotId == "Right_side_plate" && i.ParentId == vestItem.Id));
        }

        // Grenade Count
        returnValue.GrenadeCount = botInventory.Count(i => _grenadeList.Contains(i.Template));

        return returnValue;
    }
    
    private static string Tpl(Item? item) => item?.Template.ToString() ?? "Unknown";

    public string[] GetLogMessage(BotLogData botDetails)
    {
        bool RemoveNoneValues(string value) =>
            !string.IsNullOrWhiteSpace(value) &&
            !value.Contains("Unknown", StringComparison.OrdinalIgnoreCase);
        bool RemoveNonArmouredRigs(string value) => !new[] { "Armour/Rig:" }.Any(element => value.Contains(element));
        bool RemoveInvalidPlates(string value) => !value.Contains("69420");

        string? realMessage1 = null;
        string? realMessage2 = null;
        string? realMessage3 = null;
        string? realMessage4 = null;
        
        var temporaryMessage1 = new List<string>
        {
            $"Tier: {botDetails.Tier}",
            $"Role: {botDetails.Role}",
            $"Nickname: {botDetails.Name}",
            $"Level: {botDetails.Level}",
            $"Difficulty: {botDetails.Difficulty}",
            $"GameVersion: {botDetails.GameVersion}",
            $"Prestige: {botDetails.PrestigeLevel}",
            $"Grenades: {(botDetails.GrenadeCount >= 1 ? botDetails.GrenadeCount.ToString() : "Unknown")}"
        };
        var temporaryMessage2 = new List<string>
        {
            $"Primary: {ResolveName(botDetails.PrimaryWeaponId)}",
            $"Primary Caliber: {ResolveName(botDetails.PrimaryWeaponCaliber)}",
            $"Secondary: {ResolveName(botDetails.SecondaryWeaponId)}",
            $"Secondary Caliber: {ResolveName(botDetails.SecondaryWeaponCaliber)}",
            $"Holster: {ResolveName(botDetails.HolsterWeaponId)}",
            $"Holster Caliber: {ResolveName(botDetails.HolsterWeaponCaliber)}",
            $"Melee: {ResolveName(botDetails.ScabbardId)}"
        };
        var temporaryMessage3 = new List<string>
        {
            $"Helmet: {ResolveName(botDetails.HelmetId)}",
            $"NVG: {ResolveName(botDetails.NightVisionId)}",
            $"Ears: {ResolveName(botDetails.EarPieceId)}",
            $"Armour/Rig: {ResolveName(botDetails.ArmourVestId)}"
        };
        var temporaryMessage4 = new List<string>
        {
            "| Plates:",
            $"Front [{ResolvePlateClass(botDetails.FrontPlateId)}]",
            $"Back [{ResolvePlateClass(botDetails.BackPlateId)}]",
            $"Left [{ResolvePlateClass(botDetails.LeftSidePlateId)}]",
            $"Right [{ResolvePlateClass(botDetails.RightSidePlateId)}]"
        };
        
        // Filter out "Unknown" values
        temporaryMessage1 = temporaryMessage1.Where(RemoveNoneValues).ToList();
        realMessage1 = temporaryMessage1.Any() ? string.Join(" | ", temporaryMessage1.Where(s => !string.IsNullOrWhiteSpace(s))) : "No Bot Details";

        temporaryMessage2 = temporaryMessage2.Where(RemoveNoneValues).ToList();
        realMessage2 = temporaryMessage2.Any() ? string.Join(" | ", temporaryMessage2.Where(s => !string.IsNullOrWhiteSpace(s))) : "No Weapon Details";

        if (!botDetails.CanHavePlates)
        {
            temporaryMessage3 = temporaryMessage3.Where(RemoveNonArmouredRigs).ToList();
        }
        temporaryMessage3 = temporaryMessage3.Where(RemoveNoneValues).ToList();
        realMessage3 = temporaryMessage3.Any() ? string.Join(" | ", temporaryMessage3.Where(s => !string.IsNullOrWhiteSpace(s))) : "No Gear Details";

        temporaryMessage4 = temporaryMessage4.Where(RemoveInvalidPlates).ToList();
        realMessage4 = temporaryMessage4.Count > 1 ? string.Join(" ", temporaryMessage4.Where(s => !string.IsNullOrWhiteSpace(s))) : " ";

        // Return all messages
        return [realMessage1, realMessage2, realMessage3, realMessage4];
    }
    
    private string ResolveName(string tpl)
    {
        if (string.IsNullOrWhiteSpace(tpl) || tpl.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return "Unknown";

        var itemName = itemHelper.GetItemName(tpl);
        return itemName; 
    }
    
    private string ResolvePlateClass(string tpl)
    {
        if (string.IsNullOrWhiteSpace(tpl) || tpl.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return "69420";

        var item = itemHelper.GetItem(tpl).Value;
        var plateClass = item?.Properties?.ArmorClass ?? 69420;
        return plateClass.ToString(); 
    }
}