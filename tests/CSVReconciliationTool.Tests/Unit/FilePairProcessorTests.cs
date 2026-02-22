using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;
using CSVReconciliationTool.App.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CSVReconciliationTool.Tests.Unit;

public class FilePairProcessorTests
{
    private readonly Mock<ICsvService> _mockCsvService;
    private readonly Mock<IMatchingService> _mockMatchingService;
    private readonly Mock<IOutputWriter> _mockOutputWriter;
    private readonly Mock<ILogger<FilePairProcessor>> _mockLogger;
    private readonly FilePairProcessor _sut;

    public FilePairProcessorTests()
    {
        _mockCsvService = new Mock<ICsvService>();
        _mockMatchingService = new Mock<IMatchingService>();
        _mockOutputWriter = new Mock<IOutputWriter>();
        _mockLogger = new Mock<ILogger<FilePairProcessor>>();

        var categorizer = new RecordCategorizer(_mockMatchingService.Object, Mock.Of<ILogger<RecordCategorizer>>());

        _sut = new FilePairProcessor(
            _mockCsvService.Object,
            categorizer,
            _mockOutputWriter.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ReconcileAsync_BothFilesExist_ProcessesSuccessfully()
    {
        // Arrange
        var recordsA = new List<Dictionary<string, string>> { new() { ["Id"] = "1" } };
        var recordsB = new List<Dictionary<string, string>> { new() { ["Id"] = "1" } };

        _mockCsvService.Setup(c => c.ReadCsvAsync("pathA")).ReturnsAsync(recordsA);
        _mockCsvService.Setup(c => c.ReadCsvAsync("pathB")).ReturnsAsync(recordsB);
        _mockMatchingService.Setup(m => m.HasMatchingFields(It.IsAny<Dictionary<string, string>>())).Returns(true);
        _mockMatchingService.Setup(m => m.GenerateMatchKey(It.IsAny<Dictionary<string, string>>())).Returns("1|");

        // Act
        var result = await _sut.ReconcileAsync("test", "pathA", "pathB", "output");

        // Assert
        Assert.Equal("test", result.FileName);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(1, result.TotalInFolderA);
        Assert.Equal(1, result.TotalInFolderB);
        _mockOutputWriter.Verify(o => o.WriteResultsAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<CategorizedRecords>(), It.IsAny<FilePairReconciliationResult>()), Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_MissingFileInFolderA_LogsWarningAndReturns()
    {
        // Arrange
        string? pathA = null;
        string pathB = "pathB";

        // Act
        var result = await _sut.ReconcileAsync("test", pathA, pathB, "output");

        // Assert
        Assert.Equal("test", result.FileName);
        Assert.Equal(0, result.MatchedCount);
        _mockCsvService.Verify(c => c.ReadCsvAsync(It.IsAny<string>()), Times.Never);
        _mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("missing")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_MissingFileInFolderB_LogsWarningAndReturns()
    {
        // Arrange
        string pathA = "pathA";
        string? pathB = null;

        // Act
        var result = await _sut.ReconcileAsync("test", pathA, pathB, "output");

        // Assert
        Assert.Equal("test", result.FileName);
        Assert.Equal(0, result.MatchedCount);
        _mockCsvService.Verify(c => c.ReadCsvAsync(It.IsAny<string>()), Times.Never);
        _mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("missing")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_ExceptionDuringProcessing_LogsErrorAndReturnsResult()
    {
        // Arrange
        _mockCsvService.Setup(c => c.ReadCsvAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _sut.ReconcileAsync("test", "pathA", "pathB", "output");

        // Assert
        Assert.Equal("test", result.FileName);
        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error reconciling")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
