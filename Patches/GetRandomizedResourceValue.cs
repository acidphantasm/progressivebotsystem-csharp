using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Utils;
using HarmonyLib;
using SPTarkov.Server.Core.Constants;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services;

namespace _progressiveBotSystem.Patches;

public class GetRandomizedResourceValue_Patch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotGeneratorHelper),"GetRandomizedResourceValue");
    }

    [PatchPrefix]
    public static bool Prefix(ref double __result, double maxResource, RandomisedResourceValues? randomizationValues)
    {
        var randomUtil = ServiceLocator.ServiceProvider.GetRequiredService<RandomUtil>();
        
        if (randomizationValues is null || randomUtil.GetChance100(randomizationValues.ChanceMaxResourcePercent))
        {
            __result = maxResource;
            return false;
        }

        if (maxResource.Approx(1))
        {
            __result = 1;
            return false;
        }

        var min = Math.Max(1, randomUtil.GetPercentOfValue(randomizationValues.ResourcePercent, maxResource, 0));
        
        __result = randomUtil.GetDouble(min, maxResource);
        
        return false;
    }
}