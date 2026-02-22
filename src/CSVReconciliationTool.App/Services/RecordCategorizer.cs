using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;
using Microsoft.Extensions.Logging;

namespace CSVReconciliationTool.App.Services;

public class RecordCategorizer
{
    private readonly IMatchingService _matchingService;
    private readonly ILogger<RecordCategorizer> _logger;

    public RecordCategorizer(IMatchingService matchingService, ILogger<RecordCategorizer> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    // Categorizes records: Generate match keys -> Compare -> Return(Matched, OnlyInA, OnlyInB, Errors)
    public CategorizedRecords Categorize(
        List<Dictionary<string, string>> recordsA,
        List<Dictionary<string, string>> recordsB,
        string fileName,
        List<string> malformedRowsA = null,
        List<string> malformedRowsB = null)
    {
        var keysA = new Dictionary<string, Dictionary<string, string>>();
        var invalidCountA = 0;

        foreach (var record in recordsA)
        {
            // Check if record has required matching fields (e.g., "EmployeeId")
            if (_matchingService.HasMatchingFields(record))
            {
                // Generate a unique key based on matching fields (e.g., "EmployeeId")
                var key = _matchingService.GenerateMatchKey(record);
                keysA[key] = record;
            }
            else
            {
                invalidCountA++;
            }
        }

        var keysB = new Dictionary<string, Dictionary<string, string>>();
        var invalidCountB = 0;

        foreach (var record in recordsB)
        {
            if (_matchingService.HasMatchingFields(record))
            {
                // Generates a match key configured matching fields
                var key = _matchingService.GenerateMatchKey(record);
                keysB[key] = record;
            }
            else
            {
                invalidCountB++;
            }
        }

        if (invalidCountA > 0)
            _logger.LogWarning("Found {Count} records missing matching fields in {FileName} (FolderA)", invalidCountA, fileName);

        if (invalidCountB > 0)
            _logger.LogWarning("Found {Count} records missing matching fields in {FileName} (FolderB)", invalidCountB, fileName);

        var matchedRecords = new List<Dictionary<string, string>>();
        var onlyInFolderARecords = new List<Dictionary<string, string>>();

        // Assuming all records from folder B are "only in folder B"
        var onlyInFolderBRecords = new List<Dictionary<string, string>>(keysB.Values);

        // Match keys from both folders and categorize records
        foreach (var keyA in keysA.Keys)
        {
            if (keysB.ContainsKey(keyA)) // Key exists in both
            {
                matchedRecords.Add(keysA[keyA]);
                onlyInFolderBRecords.Remove(keysB[keyA]);
            }
            else // Key only in FolderA
            {
                onlyInFolderARecords.Add(keysA[keyA]);
            }
        }

        return new CategorizedRecords(
            matchedRecords, 
            onlyInFolderARecords, 
            onlyInFolderBRecords,
            malformedRowsA ?? new List<string>(),
            malformedRowsB ?? new List<string>());
    }
}
