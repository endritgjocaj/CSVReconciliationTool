using CSVReconciliationTool.App.Models;
using CSVReconciliationTool.App.Services;

namespace CSVReconciliationTool.Tests.Unit;

public class MatchingServiceTests
{
    [Fact]
    public void GenerateMatchKey_SingleField_ReturnsCorrectKey()
    {
        // Arrange
        var rule = new MatchingRule { MatchingFields = ["EmployeeId"], CaseSensitive = false, Trim = true };
        var sut = new MatchingService(rule);
        var record = new Dictionary<string, string> { ["EmployeeId"] = "EMP001", ["Name"] = "John" };

        // Act
        var key = sut.GenerateMatchKey(record);

        // Assert
        Assert.Equal("EMP001|", key);
    }

    [Fact]
    public void GenerateMatchKey_CompositeKey_ReturnsCombinedKey()
    {
        // Arrange
        var rule = new MatchingRule { MatchingFields = ["FirstName", "LastName"], CaseSensitive = false, Trim = true };
        var sut = new MatchingService(rule);
        var record = new Dictionary<string, string> { ["FirstName"] = "John", ["LastName"] = "Smith" };

        // Act
        var key = sut.GenerateMatchKey(record);

        // Assert
        Assert.Equal("JOHN|SMITH|", key);
    }

    [Fact]
    public void GenerateMatchKey_CaseSensitive_PreservesCase()
    {
        // Arrange
        var rule = new MatchingRule { MatchingFields = ["Name"], CaseSensitive = true, Trim = true };
        var sut = new MatchingService(rule);
        var record = new Dictionary<string, string> { ["Name"] = "John" };

        // Act
        var key = sut.GenerateMatchKey(record);

        // Assert
        Assert.Equal("John|", key);
    }

    [Fact]
    public void GenerateMatchKey_WithWhitespace_TrimsValue()
    {
        // Arrange
        var rule = new MatchingRule { MatchingFields = ["Name"], CaseSensitive = false, Trim = true };
        var sut = new MatchingService(rule);
        var record = new Dictionary<string, string> { ["Name"] = "  John  " };

        // Act
        var key = sut.GenerateMatchKey(record);

        // Assert
        Assert.Equal("JOHN|", key);
    }

    [Fact]
    public void HasMatchingFields_AllFieldsPresent_ReturnsTrue()
    {
        // Arrange
        var rule = new MatchingRule { MatchingFields = ["EmployeeId", "Department"] };
        var sut = new MatchingService(rule);
        var record = new Dictionary<string, string> { ["EmployeeId"] = "EMP001", ["Department"] = "IT" };

        // Act
        var result = sut.HasMatchingFields(record);

        // Assert
        Assert.True(result);
    }
}
