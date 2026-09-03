namespace ProgressiveBotSystem.Generators;

using System.Text;
using Models;
using SemanticVersioning;
using SPTarkov.Server.Core.Utils;

public class ReleaseNoteGenerator
{
    private readonly string _jsonFile;
    private readonly JsonUtil _jsonUtil;
    private readonly string _outputFile;
    private readonly Range _sptVersion;

    public ReleaseNoteGenerator(string modRootFolder, Range sptVersion, JsonUtil jsonUtil)
    {
        _sptVersion = sptVersion;
        _jsonUtil = jsonUtil;

        _jsonFile = Path.Combine(modRootFolder, "wwwroot", "files", "ReleaseNotes.json");
        _outputFile = Path.Combine(modRootFolder, "wwwroot", "files", "RELEASE_NOTES.txt");
    }

    public async Task GenerateIfFirstBuildAsync()
    {
        var allReleases =
            await _jsonUtil.DeserializeFromFileAsync<List<ReleaseNote>>(_jsonFile)
            ?? throw new InvalidOperationException("Failed to deserialize ReleaseNotes.json");

        var latestRelease =
            allReleases.FirstOrDefault(r => r.IsLatest)
            ?? allReleases.OrderByDescending(r => r.Version).First();

        var txt = new StringBuilder();

        txt.AppendLine($"### **This version will only work for SPT {_sptVersion}+**");
        txt.AppendLine();

        AppendSection(latestRelease.NewFeatures, "New Features", txt);
        AppendSection(latestRelease.Changes, "Changes", txt);
        AppendSection(latestRelease.BugFixes, "Bugs Squashed", txt);

        await File.WriteAllTextAsync(_outputFile, txt.ToString());
    }

    private void AppendSection(List<string>? items, string header, StringBuilder txt)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }

        txt.AppendLine(header);
        foreach (var item in items)
        {
            txt.AppendLine($"- {item}");
        }
        txt.AppendLine();
    }
}
