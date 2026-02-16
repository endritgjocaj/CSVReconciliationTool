using System.Text.Json;
using CSVReconciliationTool.App.Models;

namespace CSVReconciliationTool.App.Infrastructure;

/// <summary>
/// Utility for loading and parsing configuration files.
/// </summary>
public static class ConfigurationLoader
{
    public static ReconciliationConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Configuration file not found: {configPath}");

        var json = File.ReadAllText(configPath);
        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };

        var config = JsonSerializer.Deserialize<ReconciliationConfig>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize configuration");

        if (config.DegreeOfParallelism <= 0)
            config.DegreeOfParallelism = Environment.ProcessorCount;

        return config;
    }
}
