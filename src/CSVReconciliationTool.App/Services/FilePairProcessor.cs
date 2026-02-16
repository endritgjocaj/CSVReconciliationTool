using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;
using Microsoft.Extensions.Logging;

namespace CSVReconciliationTool.App.Services;

public class FilePairProcessor : IFilePairProcessor
{
    private readonly ICsvService _csvService;
    private readonly RecordCategorizer _categorizer;
    private readonly IOutputWriter _outputWriter;
    private readonly ILogger<FilePairProcessor> _logger;

    public FilePairProcessor(
        ICsvService csvService,
        RecordCategorizer categorizer,
        IOutputWriter outputWriter,
        ILogger<FilePairProcessor> logger)
    {
        _csvService = csvService;
        _categorizer = categorizer;
        _outputWriter = outputWriter;
        _logger = logger;
    }

    // Processes a single file pair and produces reconciliation results
    // Flow: Read -> Categorize -> Write -> Return result
    public async Task<FilePairReconciliationResult> ReconcileAsync(string fileName, string? pathA, string? pathB, string outputFolder)
    {
        var pairStartTime = DateTime.Now;
        var result = new FilePairReconciliationResult { FileName = fileName };

        try
        {
            // Check if both files exist
            if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB))
            {
                var missingFile = string.IsNullOrEmpty(pathA) ? "FolderA" : "FolderB";
                _logger.LogWarning("File '{FileName}' missing in {MissingFile}", fileName, missingFile);
                result.ProcessingTimeMs = (long)(DateTime.Now - pairStartTime).TotalMilliseconds;
                return result;
            }

            // Read - Load CSV records from both files
            _logger.LogInformation("Reading file pair: {FileName}", fileName);
            var recordsA = await _csvService.ReadCsvAsync(pathA!);
            var recordsB = await _csvService.ReadCsvAsync(pathB!);

            result.TotalInFolderA = recordsA.Count;
            result.TotalInFolderB = recordsB.Count;

            // Categorize - Match records and identify differences
            var categorized = _categorizer.Categorize(recordsA, recordsB, fileName);

            result.MatchedCount = categorized.Matched.Count;
            result.OnlyInFolderACount = categorized.OnlyInFolderA.Count;
            result.OnlyInFolderBCount = categorized.OnlyInFolderB.Count;

            // Write - Save categorized results to output files
            await _outputWriter.WriteResultsAsync(fileName, outputFolder, categorized, result);

            result.ProcessingTimeMs = (long)(DateTime.Now - pairStartTime).TotalMilliseconds;
            _logger.LogInformation("Completed reconciliation for '{FileName}': Matched={Matched}, OnlyA={OnlyA}, OnlyB={OnlyB}, Time={Time}ms",
                fileName, result.MatchedCount, result.OnlyInFolderACount, result.OnlyInFolderBCount, result.ProcessingTimeMs);
        }
        catch (Exception ex)
        {
            result.ProcessingTimeMs = (long)(DateTime.Now - pairStartTime).TotalMilliseconds;
            _logger.LogError(ex, "Error reconciling '{FileName}': {Message}", fileName, ex.Message);
        }

        return result;
    }
}
