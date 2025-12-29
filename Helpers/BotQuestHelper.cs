using System.Reflection;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class BotQuestHelper : IOnLoad
{
    public BotQuestHelper(
        RandomUtil randomUtil,
        ModHelper modHelper,
        ApbsLogger apbsLogger)
    {
        _apbsLogger = apbsLogger;
        _randomUtil = randomUtil;
        _modHelper = modHelper;
    }
    private QuestDataJson? QuestDataJson { get; set; }
    private readonly ApbsLogger _apbsLogger;
    private readonly RandomUtil _randomUtil;
    private readonly ModHelper _modHelper;
    
    public Task OnLoad()
    {
        var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        
        _apbsLogger.Debug("BotQuestHelper.OnLoad()");
        QuestDataJson = _modHelper.GetJsonDataFromFile<QuestDataJson>(pathToMod, "Data/Quests/QuestData.json");
        
        return Task.CompletedTask;
    }
    public bool ShouldBotHaveQuest(bool isPmc)
    {
        return isPmc && 
               !ModConfig.Config.GeneralConfig.BlickyMode &&
               ModConfig.Config.PmcBots.QuestConfig.Enable &&
               _randomUtil.GetChance100(ModConfig.Config.PmcBots.QuestConfig.Chance);
    }

    public QuestData? SelectQuest(int botLevel, string? location)
    {
        var availableQuests = GetAvailableQuests(botLevel, location);
        return availableQuests.Count == 0 ? null : _randomUtil.DrawRandomFromList(availableQuests).FirstOrDefault();
    }

    private List<QuestData> GetAvailableQuests(int botLevel, string location)
    {
        var availableQuests = new List<QuestData>();
        foreach (var quest in QuestDataJson!.AvailableQuests)
        {
            if (!quest.IsQuestEnabled) continue;
            if (quest.MinLevel <= botLevel && quest.MaxLevel >= botLevel)
            {
                if (quest.RequiredMap.Count != 0 && !quest.RequiredMap.Contains(location.ToLowerInvariant())) continue;
                
                availableQuests.Add(quest);
            }
        }
        return availableQuests;
    }
}