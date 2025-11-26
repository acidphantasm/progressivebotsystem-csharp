using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Utils;
using HarmonyLib;
using SPTarkov.Server.Core.Constants;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;

namespace _progressiveBotSystem.Patches;

public class AddDogTagToBot_Patch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotGenerator),"AddDogtagToBot");
    }

    [PatchPrefix]
    public static bool Prefix(BotBase bot)
    {
        Item inventoryItem = new()
        {
            Id = new MongoId(),
            Template = GetDogtagTplByGameVersionAndSide(bot.Info!.Side!, bot.Info!.GameVersion!, bot.Info.PrestigeLevel),
            ParentId = bot.Inventory!.Equipment,
            SlotId = Slots.Dogtag,
            Upd = new Upd { SpawnedInSession = true },
        };

        bot.Inventory!.Items!.Add(inventoryItem);
        return false;
    }

    private static MongoId GetDogtagTplByGameVersionAndSide(string side, string gameVersion, int? prestigeLevel)
    {
        
        var result = (side.ToLowerInvariant(), gameVersion, prestigeLevel) switch
        {
            ("usec", GameEditions.UNHEARD, 0) => ItemTpl.BARTER_DOGTAG_USEC_TUE,
            ("usec", GameEditions.EDGE_OF_DARKNESS, 0) => ItemTpl.BARTER_DOGTAG_USEC_EOD,
            ("usec", _, 1) => ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_1,
            ("usec", _, 2) => ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_2,
            ("usec", _, 3) => ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_3,
            ("usec", _, >= 4) => ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_4,
            ("usec", _, 0) => ItemTpl.BARTER_DOGTAG_USEC,
            ("bear", GameEditions.UNHEARD, 0) => ItemTpl.BARTER_DOGTAG_BEAR_TUE,
            ("bear", GameEditions.EDGE_OF_DARKNESS, 0) => ItemTpl.BARTER_DOGTAG_BEAR_EOD,
            ("bear", _, 1) => ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_1,
            ("bear", _, 2) => ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_2,
            ("bear", _, 3) => ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_3,
            ("bear", _, >= 4) => ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_4,
            ("bear", _, 0) => ItemTpl.BARTER_DOGTAG_BEAR,
            (_, _, _) => ItemTpl.BARTER_DOGTAGT
        };

        return result;
    }
}