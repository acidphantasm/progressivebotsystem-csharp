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
    private readonly IEnumerable<string> _alwaysDisabled = typeof(AlwaysDisabledBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> _bosses = typeof(BossBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> _followers = typeof(FollowerBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> _pmcs = typeof(PmcBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> _scavs = typeof(ScavBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> _specials = typeof(SpecialBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> _events = typeof(EventBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();

    public Task OnLoad()
    {
        apbsLogger.Debug("BotActivityHelper.OnLoad()");
        return Task.CompletedTask;
    }

    public bool IsBotEnabled(string botType)
    {
        botType = botType.ToLowerInvariant();

        if (botType is "usec" or "bear" or "pmc") botType = "pmcusec";

        return DoesBotExist(botType) && !IsBotDisabled(botType);
    }

    private bool DoesBotExist(string botType)
    {
        botType = botType.ToLowerInvariant();

        return _bosses.Contains(botType) || _followers.Contains(botType) || _pmcs.Contains(botType) ||
               _scavs.Contains(botType) || _specials.Contains(botType) || _events.Contains(botType);
    }

    private bool IsBotDisabled(string botType)
    {
        botType = botType.ToLowerInvariant();

        if (_alwaysDisabled.Contains(botType)) return true;
        if (_events.Contains(botType)) return true;
        if (_bosses.Contains(botType)) return !ModConfig.Config.BossBots.Enable;
        if (_followers.Contains(botType)) return !ModConfig.Config.FollowerBots.Enable;
        if (_pmcs.Contains(botType)) return !ModConfig.Config.PmcBots.Enable;
        if (_scavs.Contains(botType)) return !ModConfig.Config.ScavBots.Enable;
        if (_specials.Contains(botType)) return !ModConfig.Config.SpecialBots.Enable;
        
        apbsLogger.Warning($"Bot type {botType} is not enabled");
        return true;
    }
}