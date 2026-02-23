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

        // Merge all columns from matched, onlyInA, and onlyInB to get complete column set
        var allColumns = GetAllUniqueColumns(categorized);

        // Ensure all records have all columns (fill missing with empty strings)
        var normalizedMatched = NormalizeRecords(categorized.Matched, allColumns);
        var normalizedOnlyInA = NormalizeRecords(categorized.OnlyInFolderA, allColumns);
        var normalizedOnlyInB = NormalizeRecords(categorized.OnlyInFolderB, allColumns);

        await _csvService.WriteCsvAsync(Path.Combine(outputDir, "matched.csv"), normalizedMatched);
        await _csvService.WriteCsvAsync(Path.Combine(outputDir, "only-in-folderA.csv"), normalizedOnlyInA);
        await _csvService.WriteCsvAsync(Path.Combine(outputDir, "only-in-folderB.csv"), normalizedOnlyInB);

        // Write errors.csv if there are malformed rows
        var allMalformedRows = categorized.MalformedRowsA.Concat(categorized.MalformedRowsB).ToList();
        if (allMalformedRows.Count > 0)
        {
            var errorsPath = Path.Combine(outputDir, "errors.csv");
            await File.WriteAllLinesAsync(errorsPath, allMalformedRows);
        }

        var summaryPath = Path.Combine(outputDir, "reconcile-summary.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(summary, options);
        await File.WriteAllTextAsync(summaryPath, json);
    }

    private HashSet<string> GetAllUniqueColumns(CategorizedRecords categorized) =>
        categorized.Matched
            .Concat(categorized.OnlyInFolderA)
            .Concat(categorized.OnlyInFolderB)
            .SelectMany(r => r.Keys)
            .ToHashSet();

    private List<Dictionary<string, string>> NormalizeRecords(List<Dictionary<string, string>> records, HashSet<string> allColumns)
    {
        var normalized = new List<Dictionary<string, string>>();

        foreach (var record in records)
        {
            var normalizedRecord = new Dictionary<string, string>();

            // Add all columns, use empty string if column doesn't exist in this record
            foreach (var column in allColumns)
            {
                normalizedRecord[column] = record.TryGetValue(column, out var value) ? value : string.Empty;
            }

            normalized.Add(normalizedRecord);
        }

        return normalized;
    }
}
