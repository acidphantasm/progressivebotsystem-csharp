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

public class GenerateBot_Patch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotGenerator),"GenerateBot");
    }

    [PatchPostfix]
    public static void Postfix(BotBase bot, BotGenerationDetails botGenerationDetails)
    {
        // Fix the BotInfo tier from any poverty changes inside the patched GenerateInventory
        if (botGenerationDetails.ExtensionData != null && botGenerationDetails.ExtensionData.TryGetValue("Tier", out var tierValue))
        {
            bot.Info.ExtensionData["Tier"] = tierValue;
        }
    }
}