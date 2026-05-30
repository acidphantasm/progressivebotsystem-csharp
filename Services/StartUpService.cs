using System.Reflection;
using ProgressiveBotSystem.Constants;
using ProgressiveBotSystem.Generators;
using ProgressiveBotSystem.Globals;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace ProgressiveBotSystem.Services;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class StartUpService(
    IReadOnlyList<SptMod> installedMods,
    ModHelper modHelper,
    JsonUtil jsonUtil)
    : IOnLoad
{
    public Task OnLoad()
    {
        CreateLogFiles();
        CheckForMods();
        
        var releaseNoteGenerator = new ReleaseNoteGenerator(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), new ModMetadata().SptVersion, jsonUtil);
        _ = releaseNoteGenerator.GenerateIfFirstBuildAsync();

        var patchManager = new PatchManager
        {
            PatcherName = "com.acidphantasm.progressivebotsystem",
            AutoPatch = true
        };
        
        patchManager.EnablePatches();
        
        return Task.CompletedTask;
    }

    private void CreateLogFiles()
    {
        var version = new ModMetadata().Version;
        CreateLogFile(LoggingFolders.Debug, $"Debug Log Start - Acid's Progressive Bot System - Version: {version}\n");
        CreateLogFile(LoggingFolders.Warning, $"Warning Log Start - Acid's Progressive Bot System - Version: {version}\n");
        CreateLogFile(LoggingFolders.Error, $"Error Log Start - Acid's Progressive Bot System - Version: {version}\n");
        CreateLogFile(LoggingFolders.Success, $"Success Log Start - Acid's Progressive Bot System - Version: {version}\n");
        CreateLogFile(LoggingFolders.Boss, $"Boss Log Start - Acid's Progressive Bot System - Version: {version}\n");
        CreateLogFile(LoggingFolders.Event, $"Event Log Start - Acid's Progressive Bot System - Version: {version}\n");
        CreateLogFile(LoggingFolders.Pmc, $"Pmc Log Start - Acid's Progressive Bot System - Version: {version}\n");
        CreateLogFile(LoggingFolders.Scav, $"Scav Log Start - Acid's Progressive Bot System - Version: {version}\n");
        CreateLogFile(LoggingFolders.Special, $"Special Log Start - Acid's Progressive Bot System - Version: {version}\n");
        CreateLogFile(LoggingFolders.UnhandledBots, $"Unhandled Log Start - Acid's Progressive Bot System - Version: {version}\n");
    }
    
    private void CreateLogFile(string logType, string logData)
    {
        var filePath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/logs/{logType}.txt";
        var directoryName = Path.GetDirectoryName(filePath);
        
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        if (!File.Exists(filePath))
        {
            var stream = File.Create(filePath);
            stream.Close();
        }
        
        File.WriteAllText(filePath, logData);
    }

    private void CheckForMods()
    {
        ModConfig.WttBackport = installedMods.Any(x => x.ModMetadata.ModGuid == "com.wtt.contentbackport");
        ModConfig.PrestigeBackport = installedMods.Any(x => x.ModMetadata.ModGuid == "wtf.archangel.contentbackportprestiges");
        ModConfig.WttPackNStrap = installedMods.Any(x => x.ModMetadata.ModGuid == "com.wtt.packnstrap");
    }
}