using System.Reflection;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Models.Enums;
using _progressiveBotSystem.Services;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
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
        BotConfigHelper botConfigHelper,
        DataLoader dataLoader,
        BotBlacklistService  botBlacklistService,
        ItemImportService itemImportService,
        DateHelper dateHelper)
    {
        _apbsLogger = apbsLogger;
        _modHelper = modHelper;
        _jsonUtil = jsonUtil;
        _fileUtil = fileUtil;
        _botConfigHelper = botConfigHelper;
        _dataLoader = dataLoader;
        _botBlacklistService = botBlacklistService;
        _itemImportService = itemImportService;
        _dateHelper = dateHelper;
        _modPath = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }
    private static ApbsLogger _apbsLogger;
    private static ModHelper _modHelper;
    private static JsonUtil _jsonUtil;
    private static FileUtil _fileUtil;
    private static BotConfigHelper _botConfigHelper;
    private static DataLoader _dataLoader;
    private static BotBlacklistService _botBlacklistService;
    private static ItemImportService _itemImportService;
    private static DateHelper _dateHelper;
    
    public static ApbsServerConfig Config {get; private set;} = null!;
    public static ApbsServerConfig OriginalConfig {get; private set;} = null!;
    public static ApbsBlacklistConfig Blacklist { get; private set; } = null!;
    public static ApbsBlacklistConfig OriginalBlacklist { get; private set; } = null!;

    private static int _isActivelyProcessingFlag = 0;
    public static string _modPath = string.Empty;

    public static bool WttBackport;
    public static bool PrestigeBackport;
    public static bool WttPackNStrap;
    
    public static int CurrentVanillaMappingManifestVersion = 1;
    public static int CurrentPresetManifestVersion = 1;

    public async Task OnLoad()
    {
        var configPath = Path.Combine(_modPath, "config.json");
        var blacklistPath = Path.Combine(_modPath, "blacklists.json");
        var defaultConfigPath = Path.Combine(_modPath, "Data", "DefaultConfigs", "config.default.json");
        var defaultBlacklistPath = Path.Combine(_modPath, "Data", "DefaultConfigs", "blacklists.default.json");

        if (!File.Exists(configPath))
        {
            File.Copy(defaultConfigPath, configPath);
        }
        
        if (!File.Exists(blacklistPath))
        {
            File.Copy(defaultBlacklistPath, blacklistPath);
        }

        var rawConfig = await _fileUtil.ReadFileAsync(configPath);
        var rawBlacklist = await _fileUtil.ReadFileAsync(blacklistPath);
        var rawDefaultConfig = await _fileUtil.ReadFileAsync(defaultConfigPath);
        var rawDefaultBlacklist = await _fileUtil.ReadFileAsync(defaultBlacklistPath);

        Config = _jsonUtil.Deserialize<ApbsServerConfig>(rawConfig) ?? throw new ArgumentNullException();

        if (ConfigHelper.IsJsonOutdated(rawConfig, rawDefaultConfig, Config))
        {
            _apbsLogger.Warning("Config is missing new properties or has incorrect array sizes, updating...");
            await _fileUtil.WriteFileAsync(configPath, _jsonUtil.Serialize(Config, true)!);
            _apbsLogger.Success("Config updated and/or repaired.");
        }

        OriginalConfig = DeepClone(Config);

        Blacklist = _jsonUtil.Deserialize<ApbsBlacklistConfig>(rawBlacklist) ?? throw new ArgumentNullException();

        if (ConfigHelper.IsJsonOutdated(rawBlacklist, rawDefaultBlacklist))
        {
            _apbsLogger.Warning("Blacklist is missing new properties, updating...");
            await _fileUtil.WriteFileAsync(blacklistPath, _jsonUtil.Serialize(Blacklist, true)!);
            _apbsLogger.Success("Blacklist updated with new default values for missing properties.");
        }

        OriginalBlacklist = DeepClone(Blacklist);

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
            
            if (Config.UsePreset)
            {
                await _dataLoader.AssignJsonDataFromPreset(_modPath);
            }
            else
            {
                await _dataLoader.AssignJsonData(_modPath);
            }

            // DeepClone the Clean data into the Dirty data for use
            _dataLoader.AllTierDataDirty = DeepClone(_dataLoader.AllTierDataClean);

            await Task.Run(() => _dateHelper.OnLoad());
            await Task.Run(() => _botConfigHelper.ReapplyConfig());
            await _itemImportService.OnLoad();
            await Task.Run(() => _botBlacklistService.RunBlacklisting());

            _apbsLogger.Success("ModConfig reloaded successfully.");
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

    public static async Task<ConfigOperationResult> SaveConfig(bool savePresetToDisk = false, bool presetNameChange = false)
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
            
            if (Config.UsePreset)
            {
                var goodToReassignPreset = false;
                if (savePresetToDisk)
                {
                    if (await _dataLoader.SavePresetChangesToDisk(_modPath))
                    {
                        goodToReassignPreset = true;
                    }
                }
                if (presetNameChange || goodToReassignPreset)
                {
                    await _dataLoader.AssignJsonDataFromPreset(_modPath);
                }
            }
            else
            {
                await _dataLoader.AssignJsonData(_modPath);
            }
            
            // DeepClone the Clean data into the Dirty data for use
            _dataLoader.AllTierDataDirty = DeepClone(_dataLoader.AllTierDataClean);
            
            await Task.Run(() => _dateHelper.OnLoad());
            await Task.Run(() => _botConfigHelper.ReapplyConfig());
            await _itemImportService.OnLoad();
            await Task.Run(() => _botBlacklistService.RunBlacklisting());

            // Update 'Original' config stuff since we've saved so the 'Undo' function works
            OriginalConfig = DeepClone(Config);
            OriginalBlacklist = DeepClone(Blacklist);
            
            _apbsLogger.Success("ModConfig saved successfully.");
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
    
    public static T DeepClone<T>(T source)
    {
        var json = _jsonUtil.Serialize(source);
        return _jsonUtil.Deserialize<T>(json)!;
    }
}