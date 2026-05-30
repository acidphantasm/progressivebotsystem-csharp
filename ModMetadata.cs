using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;

namespace ProgressiveBotSystem;

public record ModMetadata : AbstractModMetadata, IModWebMetadata
{
    public override string ModGuid { get; init; } = "com.acidphantasm.progressivebotsystem";
    public override string Name { get; init; } = "Acid's Progressive Bot System";
    public override string Author { get; init; } = "acidphantasm";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("2.2.1");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.9");
    public override List<string>? Incompatibilities { get; init; } = ["li.barlog.andern"];
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "CC BY-NC-ND 4.0";
}