namespace CSVReconciliationTool.App.Interfaces;

public interface ICsvService
{
    Task<List<Dictionary<string, string>>> ReadCsvAsync(string filePath);
    Task WriteCsvAsync(string filePath, List<Dictionary<string, string>> records);
}