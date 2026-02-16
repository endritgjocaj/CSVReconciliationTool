using CSVReconciliationTool.App.Interfaces;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;

namespace CSVReconciliationTool.App.Infrastructure;

/// <summary>
/// Service for reading and writing CSV files.
/// </summary>
public class CsvService : ICsvService
{
    private readonly char _separator;
    private readonly bool _hasHeader;

    public CsvService(char separator = ',', bool hasHeader = true)
    {
        _separator = separator;
        _hasHeader = hasHeader;
    }

    public async Task<List<Dictionary<string, string>>> ReadCsvAsync(string filePath)
    {
        return await Task.Run(() => ReadCsv(filePath));
    }

    // Reads a CSV file and converts it to a list of dictionaries (each row = dictionary of column -> value)
    public List<Dictionary<string, string>> ReadCsv(string filePath)
    {
        var records = new List<Dictionary<string, string>>();
        var fileLines = File.ReadAllLines(filePath);

        if (fileLines.Length == 0)
            return records;

        string[] columnNames;
        int dataStartRow;

        // Determine column names from header row
        if (_hasHeader)
        {
            columnNames = fileLines[0].Split(_separator); // Use first row as headers
            dataStartRow = 1; // Start reading data from row 2
        }
        else
        {
            // No header - generate column names: "Column1", "Column2", etc.
            var firstLineColumns = fileLines[0].Split(_separator);
            columnNames = Enumerable.Range(1, firstLineColumns.Length).Select(i => $"Column{i}").ToArray();
            dataStartRow = 0;
        }

        // Parse each data row into a dictionary
        for (int lineIndex = dataStartRow; lineIndex < fileLines.Length; lineIndex++)
        {
            var currentLine = fileLines[lineIndex];
            if (string.IsNullOrWhiteSpace(currentLine))
                continue; // Skip empty lines

            var columnValues = currentLine.Split(_separator);
            var record = new Dictionary<string, string>();

            // Map each column name to its value
            for (int colIndex = 0; colIndex < columnNames.Length; colIndex++)
            {
                var value = colIndex < columnValues.Length ? columnValues[colIndex] : string.Empty;
                record[columnNames[colIndex].Trim()] = value;
            }

            records.Add(record);
        }

        return records;
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
