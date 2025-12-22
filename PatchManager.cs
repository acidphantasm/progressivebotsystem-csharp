using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using _progressiveBotSystem.Patches;
using _progressiveBotSystem.Utils;

namespace _progressiveBotSystem;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 90001)]
public class PatchManager : IOnLoad
{
    public PatchManager(ApbsLogger apbsLogger)
    {
        _apbsLogger = apbsLogger;
    }
    private ApbsLogger _apbsLogger;
    
    public Task OnLoad()
    {
        if (ModConfig.Config.EnableDebugLog)
        {
            _apbsLogger.Debug("PatchManager.OnLoad()");
        }
        
        new GenerateInventory_Patch().Enable();
        new GenerateBotLevel().Enable();
        new SetBotAppearance_Patch().Enable();
        new AddDogTagToBot_Patch().Enable();
        new SetRandomisedGameVersionAndCategory_Patch().Enable();
        new GetRandomizedMaxArmorDurability_Patch().Enable();
        
        return Task.CompletedTask;
    }
}