using System.Reflection;
using _progressiveBotSystem.Constants;
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
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.1");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.2");
    public override List<string>? Incompatibilities { get; init; } = ["li.barlog.andern"];
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "CC BY-NC-ND 4.0";
}

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class ProgressiveBotSystem(
    ISptLogger<ProgressiveBotSystem> logger,
    DatabaseService databaseService,
    ModHelper modHelper)
    : IOnLoad
{
    public Task OnLoad()
    {
        CreateLogFiles();
        return Task.CompletedTask;
    }

    private void CreateLogFiles()
    {
        CreateLogFile(LoggingFolders.Debug, $"Log File Created - Acid's Progressive Bot System - Version: {new ModMetadata().Version}\n");
        CreateLogFile(LoggingFolders.Boss, $"Log File Created - Acid's Progressive Bot System - Version: {new ModMetadata().Version}\n");
        CreateLogFile(LoggingFolders.Event, $"Log File Created - Acid's Progressive Bot System - Version: {new ModMetadata().Version}\n");
        CreateLogFile(LoggingFolders.Pmc, $"Log File Created - Acid's Progressive Bot System - Version: {new ModMetadata().Version}\n");
        CreateLogFile(LoggingFolders.Scav, $"Log File Created - Acid's Progressive Bot System - Version: {new ModMetadata().Version}\n");
        CreateLogFile(LoggingFolders.Special, $"Log File Created - Acid's Progressive Bot System - Version: {new ModMetadata().Version}\n");
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
}