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
using SPTarkov.Common.Extensions;
using SPTarkov.Server.Core.Constants;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;

namespace _progressiveBotSystem.Patches;

public class SetRandomisedGameVersionAndCategory_Patch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotGenerator),"SetRandomisedGameVersionAndCategory");
    }

    [PatchPrefix]
    public static bool Prefix(Info botInfo, string __result)
    {
        if (ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevNames.Enable && ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevNames.NameList.Contains(botInfo.Nickname))
        {
            botInfo.GameVersion = GameEditions.UNHEARD;
            botInfo.MemberCategory = MemberCategory.Developer;

            if (ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevLevels.Enable)
            {
                var minLevel = ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevLevels.Min;
                var maxLevel = ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevLevels.Max;
                
                var randomUtil = ServiceLocator.ServiceProvider.GetService<RandomUtil>();
                var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
                var level = randomUtil.GetInt(minLevel, maxLevel);
                var exp = profileHelper.GetExperience(level);
                
                botInfo.Experience = exp;
                botInfo.Level = level;
                
                
                var tierHelper = ServiceLocator.ServiceProvider.GetService<TierHelper>();
                var botInfoExtensionData = botInfo.GetExtensionData();
                botInfoExtensionData["Tier"] = tierHelper.GetTierByLevel(level);
            }

            __result = botInfo.GameVersion;
            return false;
        }
        
        return true;
    }
}