namespace ProgressiveBotSystem.Globals;

using System.Reflection;
using Helpers;
using Models;
using Models.Enums;
using Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Utils;
using Utils;

[Injectable(TypePriority = OnLoadOrder.Preload)]
public class ModConfig : IOnLoad
{
    private static ApbsLogger _apbsLogger;
    private static ModHelper _modHelper;
    private static JsonUtil _jsonUtil;
    private static FileUtil _fileUtil;
    private static BotConfigHelper _botConfigHelper;
    private static DataLoader _dataLoader;
    private static BotBlacklistService _botBlacklistService;
    private static ItemImportService _itemImportService;
    private static DateHelper _dateHelper;

    private static int _isActivelyProcessingFlag;
    public static string _modPath = string.Empty;

    public static bool WttBackport;
    public static bool PrestigeBackport;
    public static bool WttPackNStrap;

    public static int CurrentVanillaMappingManifestVersion = 2;
    public static int CurrentPresetManifestVersion = 1;

    public ModConfig(
        ModHelper modHelper,
        ApbsLogger apbsLogger,
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        BotConfigHelper botConfigHelper,
        DataLoader dataLoader,
        BotBlacklistService botBlacklistService,
        ItemImportService itemImportService,
        DateHelper dateHelper
    )
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

    public static ApbsServerConfig Config { get; private set; } = null!;
    public static ApbsServerConfig OriginalConfig { get; private set; } = null!;
    public static ApbsBlacklistConfig Blacklist { get; private set; } = null!;
    public static ApbsBlacklistConfig OriginalBlacklist { get; private set; } = null!;

    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(_modPath, "config.json");
        var blacklistPath = Path.Combine(_modPath, "blacklists.json");
        var defaultConfigPath = Path.Combine(
            _modPath,
            "Data",
            "DefaultConfigs",
            "config.default.json"
        );
        var defaultBlacklistPath = Path.Combine(
            _modPath,
            "Data",
            "DefaultConfigs",
            "blacklists.default.json"
        );

        if (!File.Exists(configPath))
        {
            File.Copy(defaultConfigPath, configPath);
        }

        if (!File.Exists(blacklistPath))
        {
            File.Copy(defaultBlacklistPath, blacklistPath);
        }

        var rawConfig = await _fileUtil.ReadFileAsync(configPath, cancellationToken);
        var rawBlacklist = await _fileUtil.ReadFileAsync(blacklistPath, cancellationToken);
        var rawDefaultConfig = await _fileUtil.ReadFileAsync(defaultConfigPath, cancellationToken);
        var rawDefaultBlacklist = await _fileUtil.ReadFileAsync(
            defaultBlacklistPath,
            cancellationToken
        );

        Config =
            _jsonUtil.Deserialize<ApbsServerConfig>(rawConfig) ?? throw new ArgumentNullException();

        if (ConfigHelper.IsJsonOutdated(rawConfig, rawDefaultConfig, Config))
        {
            await _fileUtil.WriteFileAsync(
                configPath,
                _jsonUtil.Serialize(Config, true)!,
                cancellationToken
            );
            _apbsLogger.Success("Config updated and/or repaired.");
        }

        OriginalConfig = DeepClone(Config);

        Blacklist =
            _jsonUtil.Deserialize<ApbsBlacklistConfig>(rawBlacklist)
            ?? throw new ArgumentNullException();

        if (ConfigHelper.IsJsonOutdated(rawBlacklist, rawDefaultBlacklist))
        {
            _apbsLogger.Warning("Blacklist is missing new properties, updating...");
            await _fileUtil.WriteFileAsync(
                blacklistPath,
                _jsonUtil.Serialize(Blacklist, true)!,
                cancellationToken
            );
            _apbsLogger.Success(
                "Blacklist updated with new default values for missing properties."
            );
        }

        OriginalBlacklist = DeepClone(Blacklist);

#if DEBUG
        Config.EnableDebugLog = true;
#endif
        _apbsLogger.Debug("ModConfig.OnLoad()");
    }

    public static async Task<ConfigOperationResult> ReloadConfig(
        CancellationToken cancellationToken = default
    )
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
        {
            return ConfigOperationResult.ActiveProcess;
        }

        try
        {
            if (RaidInformation.IsInRaid)
            {
                return ConfigOperationResult.InRaid;
            }

            var configPath = Path.Combine(_modPath, "config.json");
            var blacklistPath = Path.Combine(_modPath, "blacklists.json");

            var configTask = _jsonUtil.DeserializeFromFileAsync<ApbsServerConfig>(
                configPath,
                cancellationToken
            );
            var blacklistTask = _jsonUtil.DeserializeFromFileAsync<ApbsBlacklistConfig>(
                blacklistPath,
                cancellationToken
            );

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

            await Task.Run(() => _dateHelper.OnLoadAsync(cancellationToken), cancellationToken);
            await Task.Run(() => _botConfigHelper.ReapplyConfig(), cancellationToken);
            await _itemImportService.OnLoadAsync(cancellationToken);
            await Task.Run(() => _botBlacklistService.RunBlacklisting(), cancellationToken);

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

    public static async Task<ConfigOperationResult> SaveConfig(
        bool savePresetToDisk = false,
        bool presetNameChange = false,
        CancellationToken cancellationToken = default
    )
    {
        if (Interlocked.CompareExchange(ref _isActivelyProcessingFlag, 1, 0) != 0)
        {
            return ConfigOperationResult.ActiveProcess;
        }

        try
        {
            if (RaidInformation.IsInRaid)
            {
                return ConfigOperationResult.InRaid;
            }

            var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            var configPath = Path.Combine(pathToMod, "config.json");
            var blacklistPath = Path.Combine(pathToMod, "blacklists.json");

            var serializedConfigTask = Task.Run(
                () => _jsonUtil.Serialize(Config, true),
                cancellationToken
            );
            var serializedBlacklistTask = Task.Run(
                () => _jsonUtil.Serialize(Blacklist, true),
                cancellationToken
            );
            await Task.WhenAll(serializedConfigTask, serializedBlacklistTask);

            var writeConfigTask = _fileUtil.WriteFileAsync(
                configPath,
                serializedConfigTask.Result!,
                cancellationToken
            );
            var writeBlacklistTask = _fileUtil.WriteFileAsync(
                blacklistPath,
                serializedBlacklistTask.Result!,
                cancellationToken
            );
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

            await Task.Run(() => _dateHelper.OnLoadAsync(cancellationToken), cancellationToken);
            await Task.Run(() => _botConfigHelper.ReapplyConfig(), cancellationToken);
            await _itemImportService.OnLoadAsync(cancellationToken);
            await Task.Run(() => _botBlacklistService.RunBlacklisting(), cancellationToken);

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
