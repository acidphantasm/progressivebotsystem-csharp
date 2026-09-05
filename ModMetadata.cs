using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace ProgressiveBotSystem;

public record ModMetadata : IModMetadata, IModBlazorMetadata
{
    public string? WWWRootUrl { get; init; }
    public string? HomePage { get; init; } = "/progressivebotsystem";
    public string? HomePageDescription { get; init; } = "Configure APBS";
    public string ModGuid { get; init; } = "com.acidphantasm.progressivebotsystem";
    public string Name { get; init; } = "Acid's Progressive Bot System";
    public string Author { get; init; } = "acidphantasm";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("2.3.2");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; } = ["li.barlog.andern"];
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/acidphantasm/progressivebotsystem-csharp";
    public bool HasPrepatcher { get; init; } = false;
    public string License { get; init; } = "CC BY-NC-ND 4.0";
}
