﻿using System.Reflection;
using System.Text.Json.Serialization;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Models.Enums;
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
        _modPath = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
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

    private static int _isActivelyProcessingFlag = 0;
    public static string _modPath = string.Empty;


    public async Task OnLoad()
    {
        Config = await _jsonUtil.DeserializeFromFileAsync<ApbsServerConfig>(_modPath + "/config.json") ?? throw new ArgumentNullException();
        OriginalConfig = await _jsonUtil.DeserializeFromFileAsync<ApbsServerConfig>(_modPath + "/config.json") ?? throw new ArgumentNullException();
        
        Blacklist = await _jsonUtil.DeserializeFromFileAsync<ApbsBlacklistConfig>(_modPath + "/blacklists.json") ?? throw new ArgumentNullException();
        OriginalBlacklist = await _jsonUtil.DeserializeFromFileAsync<ApbsBlacklistConfig>(_modPath + "/blacklists.json") ?? throw new ArgumentNullException();
        
#if DEBUG
        Config.EnableDebugLog = true;
#endif
        if (Config.EnableDebugLog) _apbsLogger.Debug("ModConfig.OnLoad()");
    }

    public static async Task<ConfigOperationResult> ReloadConfig()
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
            return ConfigOperationResult.ActiveProcess;

        try
        {
            if (RaidInformation.IsInRaid)
                return ConfigOperationResult.InRaid;
            
            var configPath = Path.Combine(_modPath, "config.json");
            var blacklistPath = Path.Combine(_modPath, "blacklists.json");

            var configTask = _jsonUtil.DeserializeFromFileAsync<ApbsServerConfig>(configPath);
            var blacklistTask = _jsonUtil.DeserializeFromFileAsync<ApbsBlacklistConfig>(blacklistPath);

            await Task.WhenAll(configTask, blacklistTask);

            Config = configTask.Result ?? throw new ArgumentNullException(nameof(Config));
            OriginalConfig = DeepClone(Config);
            Blacklist = blacklistTask.Result ?? throw new ArgumentNullException(nameof(Blacklist));
            OriginalBlacklist = DeepClone(Blacklist);

            await Task.Run(() => _botConfigHelper.ReapplyConfig());

            _apbsLogger.Warning("ModConfig reloaded successfully.");
            return ConfigOperationResult.Success;
        }
        catch (Exception ex)
        {
            _apbsLogger.Error($"Failed to reload config: {ex.Message}");
            return ConfigOperationResult.Failure;
        }
        finally
        {
            Interlocked.Exchange(ref _isActivelyProcessingFlag, 0);
        }
    }
    
    public static async Task<ConfigOperationResult> SaveConfig()
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
            return ConfigOperationResult.ActiveProcess;

        try
        {
            if (RaidInformation.IsInRaid)
                return ConfigOperationResult.InRaid;
            
            var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var configPath = Path.Combine(pathToMod, "config.json");
            var blacklistPath = Path.Combine(pathToMod, "blacklists.json");

            var serializedConfigTask = Task.Run(() => _jsonUtil.Serialize(Config, true));
            var serializedBlacklistTask = Task.Run(() => _jsonUtil.Serialize(Blacklist, true));
            await Task.WhenAll(serializedConfigTask, serializedBlacklistTask);

            var writeConfigTask = _fileUtil.WriteFileAsync(configPath, serializedConfigTask.Result!);
            var writeBlacklistTask = _fileUtil.WriteFileAsync(blacklistPath, serializedBlacklistTask.Result!);
            await Task.WhenAll(writeConfigTask, writeBlacklistTask);

            await Task.Run(() => _botConfigHelper.ReapplyConfig());

            _apbsLogger.Warning("ModConfig saved successfully.");
            return ConfigOperationResult.Success;
        }
        catch (Exception ex)
        {
            _apbsLogger.Error($"Failed to save config: {ex.Message}");
            return ConfigOperationResult.Failure;
        }
        finally
        {
            Interlocked.Exchange(ref _isActivelyProcessingFlag, 0);
        }
    }
    
    private static T DeepClone<T>(T source)
    {
        var json = _jsonUtil.Serialize(source);
        return _jsonUtil.Deserialize<T>(json)!;
    }
}