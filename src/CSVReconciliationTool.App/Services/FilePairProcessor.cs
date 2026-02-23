using System.Collections.Concurrent;
using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;
using Microsoft.Extensions.Logging;

namespace CSVReconciliationTool.App.Services;

public class FilePairProcessor : IFilePairProcessor
{
    private readonly ICsvService _csvService;
    private readonly IOutputWriter _outputWriter;
    private readonly ILogger<FilePairProcessor> _logger;
    private readonly IMatchingService _matchingService;

    public FilePairProcessor(
        ICsvService csvService,
        IOutputWriter outputWriter,
        ILogger<FilePairProcessor> logger,
        IMatchingService matchingService)
    {
        _csvService = csvService;
        _outputWriter = outputWriter;
        _logger = logger;
        _matchingService = matchingService;
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

            // Read - Stream files and process in chunks with worker threads
            _logger.LogInformation("Reading file pair: {FileName}", fileName);

            // Step 1: Stream file B in chunks and build lookup dictionary
            var keysB = new Dictionary<string, Dictionary<string, string>>();
            var malformedRowsB = new ConcurrentQueue<string>();
            await foreach (var chunk in _csvService.ReadCsvAsync(pathB!, malformedRowsB))
            {
                foreach (var record in chunk)
                {
                    if (_matchingService.HasMatchingFields(record))
                    {
                        var key = _matchingService.GenerateMatchKey(record);
                        keysB[key] = record;
                    }
                }
            }

            var totalInB = keysB.Count;

            // Thread-safe collections for parallel processing
            var matched = new ConcurrentQueue<Dictionary<string, string>>();
            var onlyInA = new ConcurrentQueue<Dictionary<string, string>>();
            var matchedKeysInB = new ConcurrentDictionary<string, byte>();
            var malformedRowsA = new ConcurrentQueue<string>();

            // Step 2: Stream file A in chunks and process in parallel
            await Parallel.ForEachAsync(
                _csvService.ReadCsvAsync(pathA!, malformedRowsA),
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                (chunk, ct) =>
                {
                    foreach (var record in chunk)
                    {
                        if (!_matchingService.HasMatchingFields(record))
                            continue;

                        var key = _matchingService.GenerateMatchKey(record);
                        if (keysB.TryGetValue(key, out _))
                        {
                            matched.Enqueue(record);
                            matchedKeysInB.TryAdd(key, 0);
                        }
                        else
                        {
                            onlyInA.Enqueue(record);
                        }
                    }
                    return ValueTask.CompletedTask;
                });

            // Step 3: Calculate onlyInB (keys in B not matched)
            var onlyInB = keysB.Where(kv => !matchedKeysInB.ContainsKey(kv.Key))
                               .Select(kv => kv.Value)
                               .ToList();

            result.TotalInFolderA = matched.Count + onlyInA.Count;
            result.TotalInFolderB = totalInB;

            var categorized = new CategorizedRecords(
                [.. matched],
                [.. onlyInA],
                onlyInB,
                malformedRowsA.ToList(),
                malformedRowsB.ToList()
            );

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
