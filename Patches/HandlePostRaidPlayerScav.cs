using SPTarkov.Reflection.Patching;
using System.Reflection;
using HarmonyLib;
using ProgressiveBotSystem.Globals;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Services;

namespace ProgressiveBotSystem.Patches;

public class HandlePostRaidPlayerScavPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(LocationLifecycleService),"HandlePostRaidPlayerScav");
    }

    [PatchPrefix]
    public static void Prefix(MongoId sessionId, PmcData pmcProfile, PmcData scavProfile, bool isDead, bool isTransfer, bool isSurvived, EndLocalRaidRequestData request)
    {
        if (!ModConfig.Config.PlayerScavConfig.Enable)
            return;
        
        if (!ModConfig.Config.PlayerScavConfig.WritebackSkillsToPmc)
            return;
        
        var postRaidProfile = request.Results?.Profile;
        if (postRaidProfile is null)
            return;

        foreach (var skill in postRaidProfile.Skills?.Common ?? [])
        {
            if (skill.PointsEarnedDuringSession <= 0D)
                continue;
            
            var matchingPmcSkill = pmcProfile.Skills?.Common.FirstOrDefault(x => x.Id == skill.Id);
            if (matchingPmcSkill is null)
                continue;
            
            if (matchingPmcSkill.Progress >= 5100)
                continue;

            var newProgress = matchingPmcSkill.Progress + skill.PointsEarnedDuringSession;
            var clampedProgress = Math.Min(newProgress, 5100);
            matchingPmcSkill.Progress = clampedProgress;
        }
    }
}