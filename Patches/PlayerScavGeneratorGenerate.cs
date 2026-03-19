using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using System.Reflection;
using _progressiveBotSystem.Globals;
using HarmonyLib;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils.Cloners;

namespace _progressiveBotSystem.Patches;

public class PlayerScavGeneratorGeneratePatch : AbstractPatch
{
    
    private static readonly ICloner Cloner = ServiceLocator.ServiceProvider.GetRequiredService<ICloner>();
    private static readonly SaveServer SaveServer = ServiceLocator.ServiceProvider.GetRequiredService<SaveServer>();
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(PlayerScavGenerator),"Generate");
    }

    [PatchPostfix]
    public static void Postfix(MongoId sessionID, ref PmcData __result)
    {
        if (!ModConfig.Config.PlayerScavConfig.Enable)
            return;
        
        if (!ModConfig.Config.PlayerScavConfig.UsePmcSkills)
            return;
        
        var profile = SaveServer.GetProfile(sessionID);
        var profileCharactersClone = Cloner.Clone(profile.CharacterData);

        if (profileCharactersClone is null)
            return;
        
        var pmcDataClone = Cloner.Clone(profileCharactersClone.PmcData);

        if (pmcDataClone?.Skills is null)
            return;

        if (__result.Skills is null)
            return;
        
        __result.Skills.Common = pmcDataClone.Skills.Common;
    }
}