using System.Text.Json;
using System.Text.Json.Serialization;
using CSVReconciliationTool.App.Models;
using Microsoft.Extensions.Logging;

namespace CSVReconciliationTool.App.Helpers;

public class SummaryReporter
{
    private readonly ILogger<SummaryReporter> _logger;

    public SummaryReporter(ILogger<SummaryReporter> logger) => _logger = logger;

    public async Task GenerateGlobalSummaryAsync(ReconciliationResult result, string outputFolder)
    {
        var summaryPath = Path.Combine(outputFolder, "global-summary.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(result, options);
        await File.WriteAllTextAsync(summaryPath, json);
        result.SummaryJsonPath = summaryPath;
    }

    public void PrintConsoleSummary(ReconciliationResult result)
    {
        _logger.LogInformation("=== RECONCILIATION SUMMARY ===");
        _logger.LogInformation("Total records in FolderA: {TotalA}", result.TotalInFolderA);
        _logger.LogInformation("Total records in FolderB: {TotalB}", result.TotalInFolderB);
        _logger.LogInformation("Matched records: {Matched}", result.TotalMatched);
        _logger.LogInformation("Only in FolderA: {OnlyA}", result.TotalOnlyInFolderA);
        _logger.LogInformation("Only in FolderB: {OnlyB}", result.TotalOnlyInFolderB);
        _logger.LogInformation("Successful pairs: {Success}", result.SuccessfulPairs);
        _logger.LogInformation("Failed pairs: {Failed}", result.FailedPairs);
        _logger.LogInformation("Missing files: {Missing}", result.MissingFiles);
        _logger.LogInformation("Total processing time: {Time}ms", result.TotalProcessingTimeMs);
        _logger.LogInformation("Summary saved to: {Summary}", result.SummaryJsonPath);
    }
}
