using SPTarkov.Reflection.Patching;
using System.Reflection;
using HarmonyLib;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;

namespace ProgressiveBotSystem.Patches;

public class GenerateBotPatch : AbstractPatch
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