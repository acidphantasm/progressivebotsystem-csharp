using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Web;
using Path = System.IO.Path;

namespace _progressiveBotSystem;

public record ModMetadata : AbstractModMetadata, IModWebMetadata
{
    public override string ModGuid { get; init; } = "com.acidphantasm.progressivebotsystem";
    public override string Name { get; init; } = "Acid's Progressive Bot System";
    public override string Author { get; init; } = "acidphantasm";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("2.1.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.9");
    public override List<string>? Incompatibilities { get; init; } = ["li.barlog.andern"];
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "CC BY-NC-ND 4.0";
}

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class ProgressiveBotSystem(
    IReadOnlyList<SptMod> installedMods)
    : IOnLoad
{
    public Task OnLoad()
    {
        CreateLogFiles();
        CheckForMods();
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

        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
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
    }
}