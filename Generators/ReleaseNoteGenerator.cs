using System.Text;
using _progressiveBotSystem.Models;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Generators;

public class ReleaseNoteGenerator
{
    private readonly string _flagFile;
    private readonly string _jsonFile;
    private readonly string _outputFile;
    private readonly SemanticVersioning.Range _sptVersion;
    private readonly JsonUtil _jsonUtil;

    public ReleaseNoteGenerator(string modRootFolder, SemanticVersioning.Range sptVersion, JsonUtil jsonUtil)
    {
        _sptVersion = sptVersion;
        _jsonUtil = jsonUtil;

        _flagFile = Path.Combine(modRootFolder, "wwwroot", "files", "ReleaseNotesGenerated.flag");
        _jsonFile = Path.Combine(modRootFolder, "wwwroot", "files", "ReleaseNotes.json");
        _outputFile = Path.Combine(modRootFolder, "wwwroot", "files", "RELEASE_NOTES.txt");
    }

    public async Task GenerateIfFirstBuildAsync()
    {
        if (File.Exists(_flagFile)) return;
        if (!File.Exists(_jsonFile)) return;

        var allReleases = await _jsonUtil.DeserializeFromFileAsync<List<ReleaseNote>>(_jsonFile)
                          ?? throw new InvalidOperationException("Failed to deserialize ReleaseNotes.json");

        var latestRelease = allReleases.FirstOrDefault(r => r.IsLatest)
                            ?? allReleases.OrderByDescending(r => r.Version).First();

        var txt = new StringBuilder();

        txt.AppendLine($"### **This version will only work for SPT {_sptVersion}+**");
        txt.AppendLine();

        AppendSection(latestRelease.NewFeatures, "New Features", txt);
        AppendSection(latestRelease.Changes, "Changes", txt);
        AppendSection(latestRelease.BugFixes, "Bugs Squashed", txt);

        await File.WriteAllTextAsync(_outputFile, txt.ToString());
        await File.WriteAllTextAsync(_flagFile, DateTime.UtcNow.ToString("O"));
    }

    private void AppendSection(List<string>? items, string header, StringBuilder txt)
    {
        if (items == null || items.Count == 0) return;

        txt.AppendLine(header);
        foreach (var item in items)
        {
            txt.AppendLine($"- {item}");
        }
        txt.AppendLine();
    }
}