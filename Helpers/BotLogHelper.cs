using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class BotLogHelper 
{
    private readonly ApbsLogger? _apbsLogger;

    public BotLogHelper(ApbsLogger apbsLogger)
    {
        _apbsLogger = apbsLogger;
    }
}