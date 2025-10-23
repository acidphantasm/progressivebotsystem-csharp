using System.Reflection;
using System.Text.Json.Serialization;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;

namespace _progressiveBotSystem.Globals;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class ModConfig(ModHelper modHelper, ApbsLogger apbsLogger) : IOnLoad
{
    private ApbsLogger _apbsLogger;
    public static ApbsServerConfig? Config {get; private set;}

    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        Config = modHelper.GetJsonDataFromFile<ApbsServerConfig>(pathToMod, "config.json");
        _apbsLogger = apbsLogger;
        
#if DEBUG
        Config.EnableDebugLog = true;
#endif
        if (Config.EnableDebugLog) _apbsLogger.Debug("ModConfig.OnLoad()");
        
        return Task.CompletedTask;
    }
}