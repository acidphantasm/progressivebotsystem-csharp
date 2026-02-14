using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Helpers;
using SPTarkov.Common.Extensions;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Services;
using _progressiveBotSystem.Utils;
using HarmonyLib;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Enums;

namespace _progressiveBotSystem.Patches;

public class GenerateBotLevel : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotLevelGenerator),(nameof(BotLevelGenerator.GenerateBotLevel)));
    }

    [PatchPrefix]
    public static bool Prefix(ref RandomisedBotLevelResult __result, MinMax<int> levelDetails, BotGenerationDetails botGenerationDetails, BotBase bot)
    {
        var databaseService = ServiceLocator.ServiceProvider.GetRequiredService<DatabaseService>();
        var randomUtil = ServiceLocator.ServiceProvider.GetRequiredService<RandomUtil>();
        var profileHelper = ServiceLocator.ServiceProvider.GetRequiredService<ProfileHelper>();
        var tierHelper = ServiceLocator.ServiceProvider.GetRequiredService<TierHelper>();

        if (RaidInformation.FreshProfile) return true;
        
        if (botGenerationDetails.IsPlayerScav)
        {
            var scavLevel = 1;
            if (RaidInformation.CurrentSessionId is not null)
            {
                scavLevel = RaidInformation.CurrentRaidLevel ?? 1;
            }
            var scavExp = profileHelper.GetExperience(scavLevel);
            bot.Info.AddToExtensionData("Tier", tierHelper.GetTierByLevel(scavLevel));
            botGenerationDetails.AddToExtensionData("Tier", tierHelper.GetTierByLevel(scavLevel));
            bot.Info.PrestigeLevel = 0;
            __result = new RandomisedBotLevelResult { Exp = scavExp, Level = scavLevel };
            return false;
        }

        var expTable = databaseService.GetGlobals().Configuration.Exp.Level.ExperienceTable;
        var botLevelRange = GetRelativePmcBotLevelRange(botGenerationDetails, levelDetails, expTable.Length);
        
        var level = ChooseBotLevel(botLevelRange.Min, botLevelRange.Max, 1, 1.15);
        var maxLevelIndex = expTable.Length - 1;
        level = Math.Clamp(level, 1, maxLevelIndex + 1);
        
        bot.Info.PrestigeLevel = SetBotPrestigeInfo(level, botGenerationDetails);
        bot.Info.AddToExtensionData("Tier", tierHelper.GetTierByLevel(level));
        bot.Info.AddToExtensionData("PrestigeLevel", bot.Info.PrestigeLevel);
        botGenerationDetails.AddToExtensionData("Tier", tierHelper.GetTierByLevel(level));
        
        var baseExp = expTable.Take(level).Sum(entry => entry.Experience);
        var fractionalExp = level < maxLevelIndex ? randomUtil.GetInt(0, expTable[level].Experience - 1) : 0;
        
        __result = new RandomisedBotLevelResult { Exp = baseExp + fractionalExp, Level = level };
        return false;
    }
    
    private static int ChooseBotLevel(double min, double max, int shift, double number)
    {
        var randomUtil = ServiceLocator.ServiceProvider.GetRequiredService<RandomUtil>();
        return (int)randomUtil.GetBiasedRandomNumber(min, max, shift, number);
    }
    
    private static MinMax<int> GetRelativePmcBotLevelRange(BotGenerationDetails botGenerationDetails, MinMax<int> levelDetails, int maxAvailableLevel)
    {
        var tierHelper = ServiceLocator.ServiceProvider.GetRequiredService<TierHelper>();
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

        var minLevel = playerLevel - tierHelper.GetTierLowerLevelDeviation(playerLevel);
        var maxLevel = playerLevel + tierHelper.GetTierUpperLevelDeviation(playerLevel);

        if (!botGenerationDetails.IsPmc && (botGenerationDetails.Role.Contains("assault") ||
                                            botGenerationDetails.Role.Contains("marksman")))
        {
            minLevel = playerLevel - tierHelper.GetScavTierLowerLevelDeviation(playerLevel);
            maxLevel = playerLevel + tierHelper.GetScavTierUpperLevelDeviation(playerLevel);
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
        var randomUtil = ServiceLocator.ServiceProvider.GetRequiredService<RandomUtil>();
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
                botPrestigeLevel = playerPrestigeLevel >= maxPrestige ? randomUtil.GetInt(0, maxPrestige) : randomUtil.GetInt(0, playerPrestigeLevel);
                hasBotTriedAlready = true;
            }

            if (level <= (20 - Math.Abs(playerLevel - 79)))
            {
                botPrestigeLevel = playerPrestigeLevel >= maxPrestige
                    ? randomUtil.GetInt(0, maxPrestige)
                    : randomUtil.GetInt(botPrestigeLevel, Math.Min(playerPrestigeLevel + 1, 4));
                hasBotTriedAlready = true;
            }

            if (isPlayerPrestiged && !hasBotTriedAlready)
            {
                botPrestigeLevel = randomUtil.GetInt(0, Math.Max(playerPrestigeLevel - 1, 0));
            }

            return botPrestigeLevel;
        }

        return 0;
    }
}