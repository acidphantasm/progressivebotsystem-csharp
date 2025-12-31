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
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

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

    private static Dictionary<string, Dictionary<MongoId, double>> dogTagDictionary = new()
    {
        { "usec", new Dictionary<MongoId, double>
            {
                { ItemTpl.BARTER_DOGTAG_USEC, 45 },
                { new MongoId("68f15dbab2b53abd200b9378"), 1 }, // Square
                { new MongoId("68f15e53103c5d9d4f022c78"), 1 }, // Melted
                { new MongoId("5b9b9020e7ef6f5716480215"), 1 }, // Twitch
                { new MongoId("68fb4143a854bc7ae80fad3e"), 1 }, // Preorder 1
                { new MongoId("68fb4157b280c103230e3b3c"), 1 }, // Preorder 2
            }
        },
        { "bear", new Dictionary<MongoId, double>
            {
                { ItemTpl.BARTER_DOGTAG_BEAR, 45 },
                { new MongoId("68f15cf222c8979ee308f495"), 1 }, // Square
                { new MongoId("68f15e26f1aa7e100a0ca208"), 1 }, // Melted
                { new MongoId("68f153aa7da590b6df0515da"), 1 }, // Twitch
                { new MongoId("68fb41120760c7891606613c"), 1 }, // Preorder 1
                { new MongoId("68fb412b0760c7891606613e"), 1 }, // Preorder 2
            }
        }
    };

    private static MongoId GetDogtagTplByGameVersionAndSide(string side, string gameVersion, int? prestigeLevel)
    {
        side = side.ToLowerInvariant();
        var levelRequested = prestigeLevel ?? 0;

        switch (gameVersion)
        {
            case GameEditions.UNHEARD:
                return side == "usec" ? ItemTpl.BARTER_DOGTAG_USEC_TUE : ItemTpl.BARTER_DOGTAG_BEAR_TUE;
            case GameEditions.EDGE_OF_DARKNESS:
                return side == "usec" ? ItemTpl.BARTER_DOGTAG_USEC_EOD : ItemTpl.BARTER_DOGTAG_BEAR_EOD;
        }

        return GetPrestigeDogtag(side, levelRequested);
    }
    
    private static MongoId GetPrestigeDogtag(string side, int level)
    {
        var weightedRandomHelper = ServiceLocator.ServiceProvider.GetRequiredService<WeightedRandomHelper>();
        var canUseWttBackportDogtags = ModConfig.WttBackport && ModConfig.Config.CompatibilityConfig.WttBackPortAllowDogtags;
        
        return side switch
        {
            "usec" => level switch
            {
                1 => ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_1,
                2 => ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_2,
                3 => ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_3,
                4 => ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_4,
                5 => canUseWttBackportDogtags ? new MongoId("68f0f64f183146ea530330aa") : ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_4,
                >= 6 => canUseWttBackportDogtags? new MongoId("68f0f662859ebec8d501b76a") : ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_4,
                _ => canUseWttBackportDogtags ? weightedRandomHelper.GetWeightedValue(dogTagDictionary[side]) : ItemTpl.BARTER_DOGTAG_USEC
            },
            "bear" => level switch
            {
                1 => ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_1,
                2 => ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_2,
                3 => ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_3,
                4 => ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_4,
                5 => canUseWttBackportDogtags ? new MongoId("68f0f60a121d878a2303eedb") : ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_4,
                >= 6 => canUseWttBackportDogtags ? new MongoId("68f0f63c645c14a02104142a") : ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_4,
                _ => canUseWttBackportDogtags ? weightedRandomHelper.GetWeightedValue(dogTagDictionary[side]) : ItemTpl.BARTER_DOGTAG_BEAR
            },
            _ => throw new ArgumentException($"Unknown side: {side}")
        };
    }
}