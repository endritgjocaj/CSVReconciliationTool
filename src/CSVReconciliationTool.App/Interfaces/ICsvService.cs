using System.Collections.Concurrent;

namespace CSVReconciliationTool.App.Interfaces;

public interface ICsvService
{
    IAsyncEnumerable<List<Dictionary<string, string>>> ReadCsvAsync(string filePath, ConcurrentQueue<string>? malformedRows = null, int chunkSize = 1000);
    Task WriteCsvAsync(string filePath, List<Dictionary<string, string>> records);
}