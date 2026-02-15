using System.Text.Json;
using System.Text.Json.Serialization;
using CSVReconciliationTool.App.Helpers;
using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;
using Microsoft.Extensions.Logging;

namespace CSVReconciliationTool.App.Services;

public class ReconciliationService : IReconciliationService
{
    private readonly ILogger<ReconciliationService> _logger;
    private readonly FilePairProcessor _filePairProcessor;
    private readonly SummaryReporter _summaryReporter;

    public ReconciliationService(
        ILogger<ReconciliationService> logger,
        FilePairProcessor filePairProcessor,
        SummaryReporter summaryReporter)
    {
        _logger = logger;
        _filePairProcessor = filePairProcessor;
        _summaryReporter = summaryReporter;
    }

    public async Task<ReconciliationResult> ReconcileAsync(ReconciliationConfig config)
    {
        var startTime = DateTime.Now;
        var result = new ReconciliationResult { StartTime = startTime };

        try
        {
            ValidateInputFolders(config);

            var folderAFiles = GetCsvFiles(config.FolderA);
            var folderBFiles = GetCsvFiles(config.FolderB);

            _logger.LogInformation("Found {CountAFiles} CSV files in FolderA", folderAFiles.Count);
            _logger.LogInformation("Found {CountBFiles} CSV files in FolderB", folderBFiles.Count);

            var filePairs = FilePairIdentifier.IdentifyFilePairs(config.FolderA, config.FolderB, folderAFiles, folderBFiles);

            if (filePairs.Count == 0)
            {
                _logger.LogWarning("No file pairs to reconcile");
                result.EndTime = DateTime.Now;
                result.TotalProcessingTimeMs = (long)(result.EndTime - startTime).TotalMilliseconds;
                return result;
            }

            // Process file pairs concurrently to improve performance (I/O-bound, independent tasks)
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = config.DegreeOfParallelism };
            var lockObj = new object();

            _logger.LogInformation("Processing {Count} file pairs with degree of parallelism: {Parallelism}", filePairs.Count, config.DegreeOfParallelism);

            await Parallel.ForEachAsync(filePairs, parallelOptions, async (pair, ct) =>
            {
                // Process each file pair in parallel - on its own thread
                var pairResult = await _filePairProcessor.ReconcileAsync(pair.Key, pair.Value.Item1, pair.Value.Item2, config.OutputFolder);

                lock (lockObj) // Only ONE thread can enter at a time to prevent updating the same result object simultaneously
                {
                    result.FilePairResults.Add(pairResult);

                    result.SuccessfulPairs++;
                    result.TotalInFolderA += pairResult.TotalInFolderA;
                    result.TotalInFolderB += pairResult.TotalInFolderB;
                    result.TotalMatched += pairResult.MatchedCount;
                    result.TotalOnlyInFolderA += pairResult.OnlyInFolderACount;
                    result.TotalOnlyInFolderB += pairResult.OnlyInFolderBCount;
                    result.TotalProcessingTimeMs += pairResult.ProcessingTimeMs;

                    if (pairResult.TotalInFolderA == 0 && pairResult.TotalInFolderB == 0)
                    {
                        result.MissingFiles++;
                    }
                }
            });

            await _summaryReporter.GenerateGlobalSummaryAsync(result, config.OutputFolder);

            result.EndTime = DateTime.Now;

            _logger.LogInformation("Reconciliation completed successfully");
            _summaryReporter.PrintConsoleSummary(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reconciliation failed: {Message}", ex.Message);
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    private void ValidateInputFolders(ReconciliationConfig config)
    {
        if (!Directory.Exists(config.FolderA))
            throw new DirectoryNotFoundException($"FolderA not found: {config.FolderA}");

        if (!Directory.Exists(config.FolderB))
            throw new DirectoryNotFoundException($"FolderB not found: {config.FolderB}");

        if (!Directory.Exists(config.OutputFolder))
            Directory.CreateDirectory(config.OutputFolder);
    }

    private List<string> GetCsvFiles(string folderPath)
    {
        return Directory.GetFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToList()!;

        // Returns: new List<string> { "file1.csv", "file2.csv", "file3.csv" }
    }
}