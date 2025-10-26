using System.Reflection;
using System.Text.Json.Serialization;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using _progressiveBotSystem.Web.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Globals;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class ModConfig : IOnLoad
{
    public ModConfig(
        ModHelper modHelper,
        ApbsLogger apbsLogger,
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        BotConfigHelper botConfigHelper)
    {
        _apbsLogger = apbsLogger;
        _modHelper = modHelper;
        _jsonUtil = jsonUtil;
        _fileUtil = fileUtil;
        _botConfigHelper = botConfigHelper;
    }
    private static ApbsLogger _apbsLogger;
    private static ModHelper _modHelper;
    private static JsonUtil _jsonUtil;
    private static FileUtil _fileUtil;
    private static BotConfigHelper _botConfigHelper;
    public static ApbsServerConfig Config {get; private set;} = null!;
    public static ApbsServerConfig OriginalConfig {get; private set;} = null!;
    public static ApbsBlacklistConfig Blacklist { get; private set; } = null!;
    public static ApbsBlacklistConfig OriginalBlacklist { get; private set; } = null!;

    public async Task OnLoad()
    {
        var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        Config = await _jsonUtil.DeserializeFromFileAsync<ApbsServerConfig>(pathToMod + "/config.json") ?? throw new ArgumentNullException();
        OriginalConfig = await _jsonUtil.DeserializeFromFileAsync<ApbsServerConfig>(pathToMod + "/config.json") ?? throw new ArgumentNullException();
        
        Blacklist = await _jsonUtil.DeserializeFromFileAsync<ApbsBlacklistConfig>(pathToMod + "/blacklists.json") ?? throw new ArgumentNullException();
        OriginalBlacklist = await _jsonUtil.DeserializeFromFileAsync<ApbsBlacklistConfig>(pathToMod + "/blacklists.json") ?? throw new ArgumentNullException();
        
#if DEBUG
        Config.EnableDebugLog = true;
#endif
        if (Config.EnableDebugLog) _apbsLogger.Debug("ModConfig.OnLoad()");
    }

    public static async Task<bool> ReloadConfig()
    {
        if (RaidInformation.IsInRaid) return false;
        
        var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        Config = await _jsonUtil.DeserializeFromFileAsync<ApbsServerConfig>(pathToMod + "/config.json") ?? throw new ArgumentNullException();
        OriginalConfig = await _jsonUtil.DeserializeFromFileAsync<ApbsServerConfig>(pathToMod + "/config.json") ?? throw new ArgumentNullException();
        
        Blacklist = await _jsonUtil.DeserializeFromFileAsync<ApbsBlacklistConfig>(pathToMod + "/blacklists.json") ?? throw new ArgumentNullException();
        OriginalBlacklist = await _jsonUtil.DeserializeFromFileAsync<ApbsBlacklistConfig>(pathToMod + "/blacklists.json") ?? throw new ArgumentNullException();
        
        var reapplied = Task.Run(() => _botConfigHelper.ReapplyConfig());
        reapplied.Wait();
        if (reapplied.IsCompletedSuccessfully) _apbsLogger.Warning("ModConfig Reloaded.");

        return true;
    }
    
    public static async Task<bool> SaveConfig()
    {
        if (RaidInformation.IsInRaid) return false;
        
        var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        
        var serializedConfig = _jsonUtil.Serialize<ApbsServerConfig>(Config, true);
        await _fileUtil.WriteFileAsync(pathToMod + "/config.json", serializedConfig!);
        
        var serializedBlacklistConfig = _jsonUtil.Serialize<ApbsBlacklistConfig>(Blacklist, true);
        await _fileUtil.WriteFileAsync(pathToMod + "/blacklists.json", serializedBlacklistConfig!);
        
        var reapplied = Task.Run(() => _botConfigHelper.ReapplyConfig());
        reapplied.Wait();
        if (reapplied.IsCompletedSuccessfully) _apbsLogger.Warning("ModConfig Saved.");
        
        return true;
    }
}