using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using SPTarkov.Common.Extensions;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Services;
using HarmonyLib;
using ProgressiveBotSystem.Globals;
using ProgressiveBotSystem.Helpers;
using ProgressiveBotSystem.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace ProgressiveBotSystem.Patches;

[Injectable]
public class GenerateBotLevel : AbstractPatch
{
    private static GlobalTable _globalTable = default!;
    private static RandomUtil _randomUtil = default!;
    private static ProfileHelper _profileHelper = default!;
    private static TierHelper _tierHelper = default!;

    public GenerateBotLevel(GlobalTable globalTable, RandomUtil randomUtil, ProfileHelper profileHelper, TierHelper tierHelper)
    {
        _globalTable = globalTable;
        _randomUtil = randomUtil;
        _profileHelper = profileHelper;
        _tierHelper = tierHelper;
    }
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotLevelGenerator),(nameof(BotLevelGenerator.GenerateBotLevel)));
    }

    [PatchPrefix]
    public static bool Prefix(ref RandomisedBotLevelResult __result, MinMax<int> levelDetails, BotGenerationDetails botGenerationDetails, BotBase bot)
    {
        if (RaidInformation.FreshProfile) return true;
        
        if (botGenerationDetails.IsPlayerScav)
        {
            var scavLevel = 1;
            if (RaidInformation.CurrentSessionId is not null)
            {
                scavLevel = RaidInformation.CurrentRaidLevel ?? 1;
            }
            var scavExp = _profileHelper.GetExperience(scavLevel);
            bot.Info.AddToExtensionData("Tier", _tierHelper.GetTierByLevel(scavLevel));
            botGenerationDetails.AddToExtensionData("Tier", _tierHelper.GetTierByLevel(scavLevel));
            bot.Info.PrestigeLevel = 0;
            __result = new RandomisedBotLevelResult { Exp = scavExp, Level = scavLevel };
            return false;
        }

        var expTable = _globalTable.Configuration.Exp.Level.ExperienceTable;
        var botLevelRange = GetRelativePmcBotLevelRange(botGenerationDetails, levelDetails, expTable.Length);
        
        var level = ChooseBotLevel(botLevelRange.Min, botLevelRange.Max, 1, 1.15);
        var maxLevelIndex = expTable.Length - 1;
        level = Math.Clamp(level, 1, maxLevelIndex + 1);
        
        bot.Info.PrestigeLevel = SetBotPrestigeInfo(level, botGenerationDetails);
        bot.Info.AddToExtensionData("Tier", _tierHelper.GetTierByLevel(level));
        bot.Info.AddToExtensionData("PrestigeLevelData", bot.Info.PrestigeLevel);
        botGenerationDetails.AddToExtensionData("Tier", _tierHelper.GetTierByLevel(level));
        
        var baseExp = expTable.Take(level).Sum(entry => entry.Experience);
        var fractionalExp = level < maxLevelIndex ? _randomUtil.GetInt(0, expTable[level].Experience - 1) : 0;
        
        __result = new RandomisedBotLevelResult { Exp = baseExp + fractionalExp, Level = level };
        return false;
    }
    
    private static int ChooseBotLevel(double min, double max, int shift, double number)
    {
        return (int)_randomUtil.GetBiasedRandomNumber(min, max, shift, number);
    }
    
    private static MinMax<int> GetRelativePmcBotLevelRange(BotGenerationDetails botGenerationDetails, MinMax<int> levelDetails, int maxAvailableLevel)
    {
        var levelOverride = botGenerationDetails.LocationSpecificPmcLevelOverride;
        var playerLevel = Math.Max(1, botGenerationDetails.PlayerLevel ?? 1);
        
        var minPossibleLevel = levelOverride is not null
            ? Math.Min(
                Math.Max(levelDetails.Min, levelOverride.Min),
                maxAvailableLevel
            )
            : Math.Clamp(levelDetails.Min, 1, maxAvailableLevel);

        var maxPossibleLevel = levelOverride is not null
            ? Math.Min(levelOverride.Max, maxAvailableLevel)
            : Math.Min(levelDetails.Max, maxAvailableLevel);

        var minLevel = playerLevel - _tierHelper.GetTierLowerLevelDeviation(playerLevel);
        var maxLevel = playerLevel + _tierHelper.GetTierUpperLevelDeviation(playerLevel);

        if (!botGenerationDetails.IsPmc && (botGenerationDetails.Role.Contains("assault") ||
                                            botGenerationDetails.Role.Contains("marksman")))
        {
            minLevel = playerLevel - _tierHelper.GetScavTierLowerLevelDeviation(playerLevel);
            maxLevel = playerLevel + _tierHelper.GetScavTierUpperLevelDeviation(playerLevel);
        }

        if (ModConfig.Config.PmcBots.AdditionalOptions.EnablePrestiging && ModConfig.Config.PmcBots.AdditionalOptions.EnablePrestigeAnyLevel && RaidInformation.HighestPrestigeLevel != 0)
        {
            maxLevel = 79;
            minLevel = 1;
        }
        
        maxLevel = Math.Clamp(maxLevel, minPossibleLevel, maxPossibleLevel);
        minLevel = Math.Clamp(minLevel, minPossibleLevel, maxPossibleLevel);

        return new MinMax<int>(minLevel, maxLevel);
    }

    private static int SetBotPrestigeInfo(int level, BotGenerationDetails botGenerationDetails)
    {
        if (!ModConfig.Config.PmcBots.AdditionalOptions.EnablePrestiging) return 0;
        if (!botGenerationDetails.IsPmc) return 0;
        
        var playerLevel = botGenerationDetails.PlayerLevel ?? 1;
        var isPlayerPrestiged = RaidInformation.HighestPrestigeLevel != 0 ? true : false;
        var playerPrestigeLevel = RaidInformation.HighestPrestigeLevel;
        var minPlayerLevelForBotsToPrestige = 61;
        var maxPrestige = ModConfig.PrestigeBackport ? 6 : 4;

        var botCanPrestige = playerLevel >= minPlayerLevelForBotsToPrestige ||
                             playerLevel >= (level - 15) && isPlayerPrestiged || isPlayerPrestiged;

        if (botCanPrestige)
        {
            var botPrestigeLevel = 0;
            var hasBotTriedAlready = false;
            if (playerLevel >= (level - 15) && isPlayerPrestiged)
            {
                botPrestigeLevel = playerPrestigeLevel >= maxPrestige ? _randomUtil.GetInt(0, maxPrestige) : _randomUtil.GetInt(0, playerPrestigeLevel);
                hasBotTriedAlready = true;
            }

            if (level <= (20 - Math.Abs(playerLevel - 79)))
            {
                botPrestigeLevel = playerPrestigeLevel >= maxPrestige
                    ? _randomUtil.GetInt(0, maxPrestige)
                    : _randomUtil.GetInt(botPrestigeLevel, Math.Min(playerPrestigeLevel + 1, 4));
                hasBotTriedAlready = true;
            }

            if (isPlayerPrestiged && !hasBotTriedAlready)
            {
                botPrestigeLevel = _randomUtil.GetInt(0, Math.Max(playerPrestigeLevel - 1, 0));
            }

            return botPrestigeLevel;
        }

        return 0;
    }
}