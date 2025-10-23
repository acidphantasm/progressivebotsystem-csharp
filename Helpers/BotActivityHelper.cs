using System.Reflection;
using _progressiveBotSystem.Constants;
using HarmonyLib;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class BotActivityHelper
{
    private readonly IEnumerable<string> AlwaysDisabled = typeof(AlwaysDisabledBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Bosses = typeof(BossBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Followers = typeof(FollowerBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Pmcs = typeof(PmcBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Scavs = typeof(ScavBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Specials = typeof(SpecialBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    private readonly IEnumerable<string> Events = typeof(EventBots).GetFields().Select(x => x.GetValue(null)).Cast<string>();
    
    public bool IsBotEnabled(string botType)
    {
        botType = botType.ToLower();

        if (botType == "usec" || botType == "bear" || botType == "pmc") botType = "pmcusec";

        if (DoesBotExist(botType) && !IsBotDisabled(botType)) return true;

        return false;
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
        if (Bosses.Contains(botType)) return false;
        if (Followers.Contains(botType)) return false;
        if (Pmcs.Contains(botType)) return false;
        if (Scavs.Contains(botType)) return false;
        if (Specials.Contains(botType)) return false;
        if (Events.Contains(botType)) return true;
        
        return false;
    }
}