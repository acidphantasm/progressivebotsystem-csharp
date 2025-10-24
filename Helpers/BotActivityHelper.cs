using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Utils;
using HarmonyLib;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader + 20)]
public class BotActivityHelper(ApbsLogger apbsLogger): IOnLoad
{
    private readonly IEnumerable<string> AlwaysDisabled = typeof(AlwaysDisabledBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Bosses = typeof(BossBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Followers = typeof(FollowerBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Pmcs = typeof(PmcBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Scavs = typeof(ScavBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Specials = typeof(SpecialBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Events = typeof(EventBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();

    public Task OnLoad()
    {
        if (ModConfig.Config.EnableDebugLog) apbsLogger.Debug("BotActivityHelper.OnLoad()");
        return Task.CompletedTask;
    }

    public bool IsBotEnabled(string botType)
    {
        botType = botType.ToLower();

        if (botType == "usec" || botType == "bear" || botType == "pmc") botType = "pmcusec";

        return DoesBotExist(botType) && !IsBotDisabled(botType);
    }

    private bool DoesBotExist(string botType)
    {
        botType = botType.ToLower();

        return Bosses.Contains(botType) || Followers.Contains(botType) || Pmcs.Contains(botType) ||
               Scavs.Contains(botType) || Specials.Contains(botType) || Events.Contains(botType);
    }

    private bool IsBotDisabled(string botType)
    {
        botType = botType.ToLower();

        if (AlwaysDisabled.Contains(botType)) return true;
        if (Events.Contains(botType)) return true;
        if (Bosses.Contains(botType)) return !ModConfig.Config.BossBots.Enable;
        if (Followers.Contains(botType)) return !ModConfig.Config.FollowerBots.Enable;
        if (Pmcs.Contains(botType)) return !ModConfig.Config.PmcBots.Enable;
        if (Scavs.Contains(botType)) return !ModConfig.Config.ScavBots.Enable;
        if (Specials.Contains(botType)) return !ModConfig.Config.SpecialBots.Enable;
        
        Console.WriteLine($"Bot type {botType} is not enabled");
        return false;
    }
}