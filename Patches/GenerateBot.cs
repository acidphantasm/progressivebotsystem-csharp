namespace ProgressiveBotSystem.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;

[Injectable]
public class GenerateBotPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(BotGenerator), "GenerateBot");

    [PatchPostfix]
    public static void Postfix(BotBase bot, BotGenerationDetails botGenerationDetails)
    {
        // Fix the BotInfo tier from any poverty changes inside the patched GenerateInventory
        if (
            botGenerationDetails.ExtensionData != null
            && botGenerationDetails.ExtensionData.TryGetValue("Tier", out var tierValue)
        )
        {
            bot.Info?.ExtensionData?["Tier"] = tierValue;
        }
    }
}
