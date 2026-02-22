using CSVReconciliationTool.App.Helpers;
using CSVReconciliationTool.App.Infrastructure;
using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;
using CSVReconciliationTool.App.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CSVReconciliationTool.Tests.Integration;

public class ReconciliationIntegrationTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _folderA;
    private readonly string _folderB;
    private readonly string _output;

    public ReconciliationIntegrationTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "IntTest_" + Guid.NewGuid());
        _folderA = Path.Combine(_testRoot, "FolderA");
        _folderB = Path.Combine(_testRoot, "FolderB");
        _output = Path.Combine(_testRoot, "Output");

        Directory.CreateDirectory(_folderA);
        Directory.CreateDirectory(_folderB);
        Directory.CreateDirectory(_output);
    }

    [Fact]
    public async Task ReconcileAsync_FiveFilesInEachFolder_ProducesCorrectOutputs()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_folderA, "Employees.csv"),
            "Id,Name\nEMP001,John\nEMP002,Jane");
        File.WriteAllText(Path.Combine(_folderB, "Employees.csv"),
            "Id,Name\nEMP001,John\nEMP003,Bob");

        File.WriteAllText(Path.Combine(_folderA, "Orders.csv"),
            "Id,Amount\nORD001,100");
        File.WriteAllText(Path.Combine(_folderB, "Orders.csv"),
            "Id,Amount\nORD001,100");

        File.WriteAllText(Path.Combine(_folderA, "Products.csv"),
            "Id,Name\nPROD001,Laptop");
        File.WriteAllText(Path.Combine(_folderB, "Products.csv"),
            "Id,Name\nPROD001,Laptop");

        File.WriteAllText(Path.Combine(_folderA, "Customers.csv"),
            "Id,Name\nCUST001,Alice");
        File.WriteAllText(Path.Combine(_folderB, "Customers.csv"),
            "Id,Name\nCUST001,Alice");

        File.WriteAllText(Path.Combine(_folderA, "Invoices.csv"),
            "Id,Total\nINV001,200");
        File.WriteAllText(Path.Combine(_folderB, "Invoices.csv"),
            "Id,Total\nINV001,200");

        var config = new ReconciliationConfig
        {
            FolderA = _folderA,
            FolderB = _folderB,
            OutputFolder = _output,
            DegreeOfParallelism = 5,
            MatchingRule = new MatchingRule { MatchingFields = ["Id"], CaseSensitive = false, Trim = true },
            Separator = ',',
            HasHeaderRow = true
        };

        var csvService = new CsvService(config.Separator, config.HasHeaderRow);
        var matchingService = new MatchingService(config.MatchingRule);
        var outputWriter = new OutputWriter(csvService);
        var filePairProcessor = new FilePairProcessor(csvService, outputWriter, NullLogger<FilePairProcessor>.Instance, matchingService);
        var summaryReporter = new SummaryReporter(NullLogger<SummaryReporter>.Instance);
        var sut = new ReconciliationService(NullLogger<ReconciliationService>.Instance, filePairProcessor, summaryReporter);

        // Act
        var result = await sut.ReconcileAsync(config);

        // Assert
        Assert.Equal(5, result.SuccessfulPairs);
        Assert.True(result.TotalMatched >= 4);

        // Verify
        Assert.True(Directory.Exists(Path.Combine(_output, "Employees")));
        Assert.True(Directory.Exists(Path.Combine(_output, "Orders")));
        Assert.True(Directory.Exists(Path.Combine(_output, "Products")));
        Assert.True(Directory.Exists(Path.Combine(_output, "Customers")));
        Assert.True(Directory.Exists(Path.Combine(_output, "Invoices")));

        Assert.True(File.Exists(Path.Combine(_output, "global-summary.json")));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }
        catch { }
    }
}
