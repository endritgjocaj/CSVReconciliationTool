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
    public async Task ReconcileAsync_TwoFilesWithMixedRecords_ProducesCorrectOutputs()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_folderA, "Employees.csv"),
            "EmployeeId,Name,Department\nEMP001,John,IT\nEMP002,Jane,HR\nEMP003,Bob,Sales");

        File.WriteAllText(Path.Combine(_folderB, "Employees.csv"),
            "EmployeeId,Name,Department\nEMP001,John,IT\nEMP002,Jane,HR\nEMP004,Alice,Finance");

        var config = new ReconciliationConfig
        {
            FolderA = _folderA,
            FolderB = _folderB,
            OutputFolder = _output,
            DegreeOfParallelism = 2,
            MatchingRule = new MatchingRule { MatchingFields = ["EmployeeId"], CaseSensitive = false, Trim = true },
            Separator = ',',
            HasHeaderRow = true
        };

        var csvService = new CsvService(config.Separator, config.HasHeaderRow);
        var matchingService = new MatchingService(config.MatchingRule);
        var categorizer = new RecordCategorizer(matchingService, NullLogger<RecordCategorizer>.Instance);
        var outputWriter = new OutputWriter(csvService);
        var filePairProcessor = new FilePairProcessor(csvService, categorizer, outputWriter, NullLogger<FilePairProcessor>.Instance);
        var summaryReporter = new SummaryReporter(NullLogger<SummaryReporter>.Instance);
        var sut = new ReconciliationService(NullLogger<ReconciliationService>.Instance, filePairProcessor, summaryReporter);

        // Act
        var result = await sut.ReconcileAsync(config);

        // Assert
        Assert.Equal(1, result.SuccessfulPairs);
        Assert.Equal(2, result.TotalMatched);

        Assert.True(Directory.Exists(Path.Combine(_output, "Employees")));
        Assert.True(File.Exists(Path.Combine(_output, "Employees", "matched.csv")));
        Assert.True(File.Exists(Path.Combine(_output, "Employees", "only-in-folderA.csv")));
        Assert.True(File.Exists(Path.Combine(_output, "Employees", "only-in-folderB.csv")));

        var employeesMatched = File.ReadAllLines(Path.Combine(_output, "Employees", "matched.csv"));
        Assert.Equal(3, employeesMatched.Length);

        var onlyInA = File.ReadAllLines(Path.Combine(_output, "Employees", "only-in-folderA.csv"));
        Assert.Equal(2, onlyInA.Length);

        var onlyInB = File.ReadAllLines(Path.Combine(_output, "Employees", "only-in-folderB.csv"));
        Assert.Equal(2, onlyInB.Length);

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
