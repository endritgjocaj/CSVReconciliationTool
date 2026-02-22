using CSVReconciliationTool.App.Helpers;
using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;
using CSVReconciliationTool.App.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CSVReconciliationTool.Tests.Unit;

public class ReconciliationServiceTests
{
    private readonly Mock<ILogger<ReconciliationService>> _mockLogger;
    private readonly Mock<IFilePairProcessor> _mockFilePairProcessor;
    private readonly Mock<SummaryReporter> _mockSummaryReporter;
    private readonly ReconciliationService _sut;
    private readonly string _testFolderA;
    private readonly string _testFolderB;
    private readonly string _testOutput;

    public ReconciliationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ReconciliationService>>();
        _mockFilePairProcessor = new Mock<IFilePairProcessor>();
        _mockSummaryReporter = new Mock<SummaryReporter>(null!);

        _sut = new ReconciliationService(_mockLogger.Object, _mockFilePairProcessor.Object, _mockSummaryReporter.Object);

        _testFolderA = Path.Combine(Path.GetTempPath(), "testFolderA_" + Guid.NewGuid());
        _testFolderB = Path.Combine(Path.GetTempPath(), "testFolderB_" + Guid.NewGuid());
        _testOutput = Path.Combine(Path.GetTempPath(), "testOutput_" + Guid.NewGuid());

        Directory.CreateDirectory(_testFolderA);
        Directory.CreateDirectory(_testFolderB);
        Directory.CreateDirectory(_testOutput);
    }

    [Fact]
    public async Task ReconcileAsync_ValidFolders_ProcessesFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testFolderA, "test.csv"), "Id\n1");
        File.WriteAllText(Path.Combine(_testFolderB, "test.csv"), "Id\n1");

        var config = new ReconciliationConfig
        {
            FolderA = _testFolderA,
            FolderB = _testFolderB,
            OutputFolder = _testOutput,
            DegreeOfParallelism = 1,
            MatchingRule = new MatchingRule { MatchingFields = ["Id"] }
        };

        _mockFilePairProcessor
            .Setup(p => p.ReconcileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new FilePairReconciliationResult { FileName = "test", MatchedCount = 1 });

        // Act
        var result = await _sut.ReconcileAsync(config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SuccessfulPairs);
        _mockFilePairProcessor.Verify(p => p.ReconcileAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        CleanupTestFolders();
    }

    [Fact]
    public async Task ReconcileAsync_NoFiles_ReturnsEmptyResult()
    {
        // Arrange
        var config = new ReconciliationConfig
        {
            FolderA = _testFolderA,
            FolderB = _testFolderB,
            OutputFolder = _testOutput,
            DegreeOfParallelism = 1,
            MatchingRule = new MatchingRule { MatchingFields = ["Id"] }
        };

        // Act
        var result = await _sut.ReconcileAsync(config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessfulPairs);
        _mockFilePairProcessor.Verify(p => p.ReconcileAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        CleanupTestFolders();
    }

    [Fact]
    public async Task ReconcileAsync_MultipleFiles_ProcessesInParallel()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testFolderA, "file1.csv"), "Id\n1");
        File.WriteAllText(Path.Combine(_testFolderB, "file1.csv"), "Id\n1");
        File.WriteAllText(Path.Combine(_testFolderA, "file2.csv"), "Id\n2");
        File.WriteAllText(Path.Combine(_testFolderB, "file2.csv"), "Id\n2");

        var config = new ReconciliationConfig
        {
            FolderA = _testFolderA,
            FolderB = _testFolderB,
            OutputFolder = _testOutput,
            DegreeOfParallelism = 2,
            MatchingRule = new MatchingRule { MatchingFields = ["Id"] }
        };

        _mockFilePairProcessor
            .Setup(p => p.ReconcileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new FilePairReconciliationResult { FileName = "test", TotalInFolderA = 1, TotalInFolderB = 1, MatchedCount = 1 });

        // Act
        var result = await _sut.ReconcileAsync(config);

        // Assert
        Assert.Equal(2, result.SuccessfulPairs);
        Assert.Equal(2, result.TotalMatched);
        _mockFilePairProcessor.Verify(p => p.ReconcileAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));

        CleanupTestFolders();
    }

    private void CleanupTestFolders()
    {
        try
        {
            if (Directory.Exists(_testFolderA)) Directory.Delete(_testFolderA, true);
            if (Directory.Exists(_testFolderB)) Directory.Delete(_testFolderB, true);
            if (Directory.Exists(_testOutput)) Directory.Delete(_testOutput, true);
        }
        catch { }
    }
}
