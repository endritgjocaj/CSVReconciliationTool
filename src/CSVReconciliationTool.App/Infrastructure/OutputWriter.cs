using System.Text.Json;
using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;

namespace CSVReconciliationTool.App.Infrastructure;

public class OutputWriter : IOutputWriter
{
    private readonly ICsvService _csvService;

    public OutputWriter(ICsvService csvService)
    {
        _csvService = csvService;
    }

    public async Task WriteResultsAsync(
        string fileName,
        string outputFolder,
        CategorizedRecords categorized,
        FilePairReconciliationResult summary)
    {
        var outputDir = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(fileName));
        Directory.CreateDirectory(outputDir);

        await _csvService.WriteCsvAsync(Path.Combine(outputDir, "matched.csv"), categorized.Matched);
        await _csvService.WriteCsvAsync(Path.Combine(outputDir, "only-in-folderA.csv"), categorized.OnlyInFolderA);
        await _csvService.WriteCsvAsync(Path.Combine(outputDir, "only-in-folderB.csv"), categorized.OnlyInFolderB);

        var summaryPath = Path.Combine(outputDir, "reconcile-summary.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(summary, options);
        await File.WriteAllTextAsync(summaryPath, json);
    }
}
