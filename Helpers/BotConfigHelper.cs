using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Utils;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class BotConfigHelper : IOnLoad
{
    public BotConfigHelper(
        DatabaseService databaseService,
        ConfigServer configServer,
        ApbsLogger apbsLogger,
        BotActivityHelper botActivityHelper)
    {
        _databaseService = databaseService;
        _botConfig = configServer.GetConfig<BotConfig>();
        _apbsLogger = apbsLogger;
        _botActivityHelper = botActivityHelper;
    }
    
    private ApbsLogger _apbsLogger;
    private DatabaseService _databaseService;
    private BotConfig _botConfig;
    private BotActivityHelper _botActivityHelper;
    
    public Task OnLoad()
    {
        if (ModConfig.Config.EnableDebugLog) _apbsLogger.Debug("BotConfigHelper.OnLoad()");
        
        SetBotLevels();
        RemoveUnnecessaryDetails();
        AdjustNVGs();
        SetWeaponModLimits();
        
        return Task.CompletedTask;
    }

    private void SetBotLevels()
    {
        var allBotTypes = _databaseService.GetBots().Types;
        foreach (var (bot, botData) in allBotTypes)
        {
            allBotTypes[bot].BotExperience.Level.Min = 1;
            allBotTypes[bot].BotExperience.Level.Max = 79;
        }
    }

    private void RemoveUnnecessaryDetails()
    {
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, _) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            
            botConfigEquipment[botType].Randomisation = new List<RandomisationDetails>();
            botConfigEquipment[botType].WeightingAdjustmentsByBotLevel = new List<WeightingAdjustmentDetails>();
        }
    }

    private void AdjustNVGs()
    {
        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, _) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;
            
            botConfigEquipment[botType].FaceShieldIsActiveChancePercent = 90;
            botConfigEquipment[botType].LightIsActiveDayChancePercent = 7;
            botConfigEquipment[botType].LightIsActiveNightChancePercent = 25;
            botConfigEquipment[botType].LaserIsActiveChancePercent = 50;
            botConfigEquipment[botType].NvgIsActiveChanceDayPercent = 0;
            botConfigEquipment[botType].NvgIsActiveChanceNightPercent = 95;
        }
    }

    private void SetWeaponModLimits()
    {
        if (!ModConfig.Config.GeneralConfig.ForceWeaponModLimits) return;

        var botConfigEquipment = _botConfig.Equipment;
        foreach (var (botType, _) in botConfigEquipment)
        {
            if (!_botActivityHelper.IsBotEnabled(botType)) continue;

            if (botConfigEquipment[botType].WeaponModLimits is null)
            {
                botConfigEquipment[botType].WeaponModLimits = new ModLimits()
                {
                    ScopeLimit = 2,
                    LightLaserLimit = 1
                };
            }
            botConfigEquipment[botType].WeaponModLimits.ScopeLimit = ModConfig.Config.GeneralConfig.ScopeLimit;
            botConfigEquipment[botType].WeaponModLimits.LightLaserLimit = ModConfig.Config.GeneralConfig.TacticalLimit;
        }
    }
    
}