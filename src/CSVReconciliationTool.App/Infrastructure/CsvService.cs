using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;
using Microsoft.Extensions.Logging;

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

    public async Task<List<Dictionary<string, string>>> ReadCsvAsync(string filePath)
    {
        return await Task.Run(() => ReadCsv(filePath));
    }

    // Reads CSV and returns only valid records
    public List<Dictionary<string, string>> ReadCsv(string filePath)
    {
        return ReadCsvWithErrors(filePath).ValidRecords;
    }

    // Reads CSV and returns both valid records and malformed rows
    public CsvReadResult ReadCsvWithErrors(string filePath)
    {
        var result = new CsvReadResult();
        var fileLines = File.ReadAllLines(filePath);

        if (fileLines.Length == 0)
            return result;

        string[] columnNames;
        int dataStartRow;

        // Determine column names from header row
        if (_hasHeader)
        {
            columnNames = fileLines[0].Split(_separator);
            dataStartRow = 1;
        }
        else
        {
            // No header - generate column names: "Column1", "Column2", etc.
            var firstLineColumns = fileLines[0].Split(_separator);
            columnNames = Enumerable.Range(1, firstLineColumns.Length).Select(i => $"Column{i}").ToArray();
            dataStartRow = 0;
        }

        var fileName = Path.GetFileName(filePath);

        // Parse each data row into a dictionary
        for (int lineIndex = dataStartRow; lineIndex < fileLines.Length; lineIndex++)
        {
            var currentLine = fileLines[lineIndex];
            if (string.IsNullOrWhiteSpace(currentLine))
                continue;

            var columnValues = currentLine.Split(_separator);

            // Track malformed rows separately
            if (columnValues.Length != columnNames.Length)
            {
                result.MalformedRows.Add(currentLine);
                _logger.LogWarning(
                    "Malformed row in '{FileName}' at line {LineNumber}: Expected {ExpectedColumns} columns, but found {ActualColumns}",
                    fileName, lineIndex + 1, columnNames.Length, columnValues.Length);
                continue; // Skip - don't add to valid records
            }

            var record = new Dictionary<string, string>();
            for (int colIndex = 0; colIndex < columnNames.Length; colIndex++)
            {
                record[columnNames[colIndex].Trim()] = columnValues[colIndex];
            }

            result.ValidRecords.Add(record);
        }

        if (result.MalformedRows.Count > 0)
            _logger.LogWarning("Total malformed rows in '{FileName}': {MalformedCount}", fileName, result.MalformedRows.Count);

        return result;
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
