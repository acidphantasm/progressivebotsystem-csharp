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

public class SetBotAppearance_Patch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotGenerator).GetMethod("SetBotAppearance", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    [PatchPrefix]
    public static bool Prefix(BotBase bot, Appearance appearance, BotGenerationDetails botGenerationDetails)
    {
        return true;
    }
}