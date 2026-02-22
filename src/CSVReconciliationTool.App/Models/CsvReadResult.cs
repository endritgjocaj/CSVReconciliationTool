namespace CSVReconciliationTool.App.Models;

/// <summary>
/// Result of reading a CSV file, including both valid and malformed rows.
/// </summary>
public class CsvReadResult
{
    public List<Dictionary<string, string>> ValidRecords { get; set; } = new();
    public List<string> MalformedRows { get; set; } = new();
}
