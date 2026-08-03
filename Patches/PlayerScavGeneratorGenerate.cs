using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using System.Reflection;
using HarmonyLib;
using ProgressiveBotSystem.Globals;
using ProgressiveBotSystem.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils.Cloners;

namespace ProgressiveBotSystem.Patches;

[Injectable]
public class PlayerScavGeneratorGeneratePatch : AbstractPatch
{
    private static ICloner _cloner = default!;
    private static SaveServer _saveServer = default!;
    private static CustomBotLootCacheService _customBotLootCacheService = default!;

    public PlayerScavGeneratorGeneratePatch(
        ICloner cloner,
        SaveServer saveServer,
        CustomBotLootCacheService customBotLootCacheService)
    {
        _cloner = cloner;
        _saveServer = saveServer;
        _customBotLootCacheService = customBotLootCacheService;
    }
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(PlayerScavGenerator),"Generate");
    }

    [PatchPostfix]
    public static void Postfix(MongoId sessionID, ref PmcData __result)
    {
        _customBotLootCacheService.ClearApbsCache();
        
        if (!ModConfig.Config.PlayerScavConfig.Enable)
            return;
        
        if (!ModConfig.Config.PlayerScavConfig.UsePmcSkills)
            return;
        
        var profile = _saveServer.GetProfile(sessionID);
        var profileCharactersClone = _cloner.Clone(profile.CharacterData);

        if (profileCharactersClone is null)
            return;
        
        var pmcDataClone = _cloner.Clone(profileCharactersClone.PmcData);

        if (pmcDataClone?.Skills is null)
            return;

        if (__result.Skills is null)
            return;
        
        __result.Skills.Common = pmcDataClone.Skills.Common;
    }
}