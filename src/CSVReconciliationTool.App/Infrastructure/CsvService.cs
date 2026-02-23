using CSVReconciliationTool.App.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CSVReconciliationTool.App.Infrastructure;

/// <summary>
/// Service for reading and writing CSV files.
/// </summary>
public class CsvService : ICsvService
{
    private readonly char _separator;
    private readonly bool _hasHeader;
    private readonly ILogger<CsvService> _logger;

    public CsvService(char separator = ',', bool hasHeader = true, ILogger<CsvService>? logger = null)
    {
        _separator = separator;
        _hasHeader = hasHeader;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CsvService>.Instance;
    }

    // Reads CSV as a stream in chunks without loading entire file into memory
    public async IAsyncEnumerable<List<Dictionary<string, string>>> ReadCsvAsync(string filePath, ConcurrentQueue<string>? malformedRows = null, int chunkSize = 1000)
    {
        using var reader = new StreamReader(filePath);

        string[] columnNames;
        if (_hasHeader)
        {
            var headerLine = await reader.ReadLineAsync();
            if (headerLine == null) yield break;
            columnNames = headerLine.Split(_separator);
        }
        else
        {
            yield break;
        }

        var chunk = new List<Dictionary<string, string>>();
        var fileName = Path.GetFileName(filePath);
        int malformedCount = 0;
        int lineNumber = 1; // starts after header
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split(_separator);
            if (values.Length != columnNames.Length)
            {
                malformedCount++;

                // First malformed row - add the header line so errors.csv has column names
                if (malformedRows != null && malformedRows.IsEmpty)
                    malformedRows.Enqueue(string.Join(_separator, columnNames));

                // Pad missing columns with empty strings
                var paddedValues = Enumerable.Range(0, columnNames.Length)
                    .Select(i => i < values.Length ? values[i] : string.Empty);
                malformedRows?.Enqueue(string.Join(_separator, paddedValues));

                _logger.LogWarning(
                    "Malformed row in '{FileName}' at line {LineNumber}: Expected {ExpectedColumns} columns, but found {ActualColumns}",
                    fileName, lineNumber, columnNames.Length, values.Length);
                continue;
            }

            var record = new Dictionary<string, string>();
            for (int i = 0; i < columnNames.Length; i++)
            {
                record[columnNames[i].Trim()] = values[i];
            }
            chunk.Add(record);

            if (chunk.Count >= chunkSize)
            {
                yield return chunk;
                chunk = new List<Dictionary<string, string>>();
            }
        }

        if (malformedCount > 0)
            _logger.LogWarning("Total malformed rows in '{FileName}': {MalformedCount}", fileName, malformedCount);

        if (chunk.Count > 0)
            yield return chunk;
    }

    // Writes a list of dictionaries to a CSV file (each dictionary = one row)
    public async Task WriteCsvAsync(string filePath, List<Dictionary<string, string>> records)
    {
        if (records.Count == 0)
        {
            await File.WriteAllTextAsync(filePath, string.Empty);
            return;
        }

        // Get column headers from first record
        var columnHeaders = records[0].Keys.ToList();

        var outputLines = new List<string>
        {
            string.Join(_separator, columnHeaders)
        };

        // Convert each record to a CSV row
        foreach (var record in records)
        {
            var rowValues = columnHeaders.Select(header => 
                record.TryGetValue(header, out var value) ? value : string.Empty);

            outputLines.Add(string.Join(_separator, rowValues));
        }

        await File.WriteAllTextAsync(filePath, string.Join(Environment.NewLine, outputLines));
    }
}
