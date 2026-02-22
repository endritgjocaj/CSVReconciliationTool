using CSVReconciliationTool.App.Infrastructure;

namespace CSVReconciliationTool.Tests.Unit;

public class ConfigurationLoaderTests
{
    private readonly string _testDirectory;

    public ConfigurationLoaderTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "ConfigLoaderTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void Load_ValidJsonWithSingleMatchingField_ReturnsConfig()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "valid-single.json");
        var json = """
        {
            "folderA": "./folderA",
            "folderB": "./folderB",
            "outputFolder": "./output",
            "matchingRule": {
                "matchingFields": ["OrderId"],
                "caseSensitive": false,
                "trim": true
            },
            "degreeOfParallelism": 4
        }
        """;
        File.WriteAllText(configPath, json);

        // Act
        var config = ConfigurationLoader.Load(configPath);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("./folderA", config.FolderA);
        Assert.Single(config.MatchingRule.MatchingFields);
        Assert.Equal("OrderId", config.MatchingRule.MatchingFields[0]);
        Assert.False(config.MatchingRule.CaseSensitive);
        Assert.True(config.MatchingRule.Trim);
        Assert.Equal(4, config.DegreeOfParallelism);
    }

    [Fact]
    public void Load_ValidJsonWithCompositeMatchingFields_ReturnsConfig()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "valid-composite.json");
        var json = """
        {
            "folderA": "./folderA",
            "folderB": "./folderB",
            "outputFolder": "./output",
            "matchingRule": {
                "matchingFields": ["FirstName", "LastName"],
                "caseSensitive": true
            }
        }
        """;
        File.WriteAllText(configPath, json);

        // Act
        var config = ConfigurationLoader.Load(configPath);

        // Assert
        Assert.Equal(2, config.MatchingRule.MatchingFields.Count);
        Assert.Equal("FirstName", config.MatchingRule.MatchingFields[0]);
        Assert.Equal("LastName", config.MatchingRule.MatchingFields[1]);
        Assert.True(config.MatchingRule.CaseSensitive);
    }

    [Fact]
    public void Load_MissingOptionalFields_UsesDefaults()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "minimal.json");
        var json = """
        {
            "folderA": "./folderA",
            "folderB": "./folderB",
            "outputFolder": "./output",
            "matchingRule": {
                "matchingFields": ["OrderId"]
            }
        }
        """;
        File.WriteAllText(configPath, json);

        // Act
        var config = ConfigurationLoader.Load(configPath);

        // Assert
        Assert.Equal(',', config.Separator);
        Assert.True(config.HasHeaderRow);
        Assert.Equal(Environment.ProcessorCount, config.DegreeOfParallelism);
    }

    [Fact]
    public void Load_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => ConfigurationLoader.Load(nonExistentPath));
        Assert.Contains("Configuration file not found", exception.Message);
    }

    [Fact]
    public void Load_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "invalid.json");
        var invalidJson = "{ this is not valid json }";
        File.WriteAllText(configPath, invalidJson);

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => ConfigurationLoader.Load(configPath));
    }
}
