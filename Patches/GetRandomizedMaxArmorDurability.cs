using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Generators;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using HarmonyLib;
using SPTarkov.Common.Extensions;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Exceptions.Helpers;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Patches;

public class GetRandomizedMaxArmorDurability_Patch : AbstractPatch
{
    private static readonly HashSet<string> ScavRoles = typeof(ScavBots).GetFields().Select(x => (string)x.GetValue(null)).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> BossRoles = typeof(BossBots).GetFields().Select(x => (string)x.GetValue(null)).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> FollowerRoles = typeof(FollowerBots).GetFields().Select(x => (string)x.GetValue(null)).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> SpecialRoles = typeof(SpecialBots).GetFields().Select(x => (string)x.GetValue(null)).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> PmcRoles = typeof(PmcBots).GetFields().Select(x => (string)x.GetValue(null)).ToHashSet(StringComparer.Ordinal);
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(DurabilityLimitsHelper), nameof(DurabilityLimitsHelper.GetRandomizedMaxArmorDurability));
    }

    [PatchPrefix]
    public static bool Prefix(ref double __result, TemplateItem? itemTemplate, string? botRole = null)
    {
        var botActivityHelper = ServiceLocator.ServiceProvider.GetRequiredService<BotActivityHelper>();
        var logger = ServiceLocator.ServiceProvider.GetRequiredService<ISptLogger<DurabilityLimitsHelper>>();

        if (RaidInformation.FreshProfile || RaidInformation.CurrentSessionId is null || botRole is null || !botActivityHelper.IsBotEnabled(botRole)) return true;
        if (!ModConfig.Config.ScavBots.ArmourDurability.Enable && ScavRoles.Contains(botRole)) return true;
        if (!ModConfig.Config.BossBots.ArmourDurability.Enable && BossRoles.Contains(botRole)) return true;
        if (!ModConfig.Config.FollowerBots.ArmourDurability.Enable && FollowerRoles.Contains(botRole)) return true;
        if (!ModConfig.Config.SpecialBots.ArmourDurability.Enable && SpecialRoles.Contains(botRole)) return true;
        if (!ModConfig.Config.PmcBots.ArmourDurability.Enable && PmcRoles.Contains(botRole)) return true;
        
        var itemMaxDurability = itemTemplate?.Properties?.MaxDurability;
        if (!itemMaxDurability.HasValue)
        {
            const string message = "Item max durability amount is null when trying to get max armor durability";
            logger.Error(message);
            throw new DurabilityHelperException(message);
        }

        __result = GenerateMaxArmorDurability(botRole, itemMaxDurability.Value);
        return false;
    }
    
    private static double GenerateMaxArmorDurability(string botRole, double itemMaxDurability)
    {
        var botConfig = ServiceLocator.ServiceProvider.GetRequiredService<ConfigServer>().GetConfig<BotConfig>();
        var randomUtil = ServiceLocator.ServiceProvider.GetRequiredService<RandomUtil>();
        var logger = ServiceLocator.ServiceProvider.GetRequiredService<ISptLogger<DurabilityLimitsHelper>>();

        (double min, double max)? range = null;

        if (botConfig.Durability.BotDurabilities.TryGetValue(botRole, out var durability))
        {
            range = (durability.Armor.LowestMaxPercent, durability.Armor.HighestMaxPercent);
        }
        else if (FollowerRoles.Contains(botRole))
        {
            var armor = botConfig.Durability.BotDurabilities["follower"].Armor;
            range = (armor.LowestMaxPercent, armor.HighestMaxPercent);
        }
        else if (BossRoles.Contains(botRole))
        {
            var armor = botConfig.Durability.BotDurabilities["boss"].Armor;
            range = (armor.LowestMaxPercent, armor.HighestMaxPercent);
        }
        else if (PmcRoles.Contains(botRole))
        {
            var armor = botConfig.Durability.Pmc.Armor;
            range = (armor.LowestMaxPercent, armor.HighestMaxPercent);
        }
        
        if (range is null)
        {
            logger.Error($"[ARMOUR DURABILITY] {botRole} <- REPORT THIS PLEASE | Defaulting to 90 - 100 Durability");
            range = (90, 100);
        }
        
        var multiplier = randomUtil.GetDouble(range.Value.min, range.Value.max);
        return itemMaxDurability * (multiplier / 100);
    }
}