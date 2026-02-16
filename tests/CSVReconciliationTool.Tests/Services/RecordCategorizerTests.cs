using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CSVReconciliationTool.Tests.Services;

public class RecordCategorizerTests
{
    private readonly Mock<IMatchingService> _mockMatchingService;
    private readonly Mock<ILogger<RecordCategorizer>> _mockLogger;
    private readonly RecordCategorizer _sut;

    public RecordCategorizerTests()
    {
        _mockMatchingService = new Mock<IMatchingService>();
        _mockLogger = new Mock<ILogger<RecordCategorizer>>();
        _sut = new RecordCategorizer(_mockMatchingService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Categorize_MatchingRecords_ReturnsMatched()
    {
        // Arrange
        var recordsA = new List<Dictionary<string, string>> { new() { ["EmployeeId"] = "EMP001" } };
        var recordsB = new List<Dictionary<string, string>> { new() { ["EmployeeId"] = "EMP001" } };

        _mockMatchingService.Setup(m => m.HasMatchingFields(It.IsAny<Dictionary<string, string>>())).Returns(true);
        _mockMatchingService.Setup(m => m.GenerateMatchKey(It.IsAny<Dictionary<string, string>>())).Returns("EMP001|");

        // Act
        var result = _sut.Categorize(recordsA, recordsB, "test.csv");

        // Assert
        Assert.Single(result.Matched);
        Assert.Empty(result.OnlyInFolderA);
        Assert.Empty(result.OnlyInFolderB);
    }

    [Fact]
    public void Categorize_MixedRecords_CategorizesCorrectly()
    {
        // Arrange
        var recordsA = new List<Dictionary<string, string>>
        {
            new() { ["EmployeeId"] = "EMP001" },
            new() { ["EmployeeId"] = "EMP002" }
        };
        var recordsB = new List<Dictionary<string, string>>
        {
            new() { ["EmployeeId"] = "EMP001" },
            new() { ["EmployeeId"] = "EMP003" }
        };

        _mockMatchingService.Setup(m => m.HasMatchingFields(It.IsAny<Dictionary<string, string>>())).Returns(true);
        _mockMatchingService.Setup(m => m.GenerateMatchKey(It.Is<Dictionary<string, string>>(r => r["EmployeeId"] == "EMP001"))).Returns("EMP001|");
        _mockMatchingService.Setup(m => m.GenerateMatchKey(It.Is<Dictionary<string, string>>(r => r["EmployeeId"] == "EMP002"))).Returns("EMP002|");
        _mockMatchingService.Setup(m => m.GenerateMatchKey(It.Is<Dictionary<string, string>>(r => r["EmployeeId"] == "EMP003"))).Returns("EMP003|");

        // Act
        var result = _sut.Categorize(recordsA, recordsB, "test.csv");

        // Assert
        Assert.Single(result.Matched);
        Assert.Single(result.OnlyInFolderA);
        Assert.Single(result.OnlyInFolderB);
    }

    [Fact]
    public void Categorize_RecordsMissingMatchingFields_LogsWarning()
    {
        // Arrange
        var recordsA = new List<Dictionary<string, string>> { new() { ["Name"] = "John" } };
        var recordsB = new List<Dictionary<string, string>>();

        _mockMatchingService.Setup(m => m.HasMatchingFields(It.IsAny<Dictionary<string, string>>())).Returns(false);

        // Act
        var result = _sut.Categorize(recordsA, recordsB, "test.csv");

        // Assert
        Assert.Empty(result.Matched);
        Assert.Empty(result.OnlyInFolderA);
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("missing matching fields")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Categorize_EmptyRecords_ReturnsEmptyResults()
    {
        // Arrange
        var recordsA = new List<Dictionary<string, string>>();
        var recordsB = new List<Dictionary<string, string>>();

        // Act
        var result = _sut.Categorize(recordsA, recordsB, "test.csv");

        // Assert
        Assert.Empty(result.Matched);
        Assert.Empty(result.OnlyInFolderA);
        Assert.Empty(result.OnlyInFolderB);
    }
}
