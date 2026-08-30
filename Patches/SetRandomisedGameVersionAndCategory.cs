using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using HarmonyLib;
using ProgressiveBotSystem.Globals;
using ProgressiveBotSystem.Helpers;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace ProgressiveBotSystem.Patches;

[Injectable]
public class SetRandomisedGameVersionAndCategoryPatch : AbstractPatch
{
    private static RandomUtil _randomUtil = default!;
    private static ProfileHelper _profileHelper = default!;
    private static TierHelper _tierHelper = default!;

    public SetRandomisedGameVersionAndCategoryPatch(
        RandomUtil randomUtil,
        ProfileHelper profileHelper,
        TierHelper tierHelper)
    {
        _randomUtil = randomUtil;
        _profileHelper = profileHelper;
        _tierHelper = tierHelper;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotGenerator), "SetRandomisedGameVersionAndCategory");
    }

    [PatchPrefix]
    public static bool Prefix(Info botInfo, string __result)
    {
        if (ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevNames.Enable && ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevNames.NameList.Contains(botInfo.Nickname ?? "abcd1234fakename"))
        {
            botInfo.GameVersion = GameEditions.UNHEARD;
            botInfo.MemberCategory = MemberCategory.Developer;

            if (ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevLevels.Enable)
            {
                var minLevel = ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevLevels.Min;
                var maxLevel = ModConfig.Config.PmcBots.Secrets.DeveloperSettings.DevLevels.Max;

                var level = _randomUtil.GetInt(minLevel, maxLevel);
                var exp = _profileHelper.GetExperience(level);

                botInfo.Experience = exp;
                botInfo.Level = level;

                var botInfoExtensionData = botInfo.GetExtensionData();
                botInfoExtensionData["Tier"] = _tierHelper.GetTierByLevel(level);
            }

            __result = botInfo.GameVersion;
            return false;
        }

        return true;
    }
}