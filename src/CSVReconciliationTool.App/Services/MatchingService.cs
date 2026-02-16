using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Models;

namespace CSVReconciliationTool.App.Services;

/// <summary>
/// Service for matching records based on defined rules.
/// </summary>
public class MatchingService : IMatchingService
{
    private readonly MatchingRule _rule;

    public MatchingService(MatchingRule rule)
    {
        _rule = rule ?? throw new ArgumentNullException(nameof(rule));
    }

    // Generates a match key for the record based on the configured matching fields and rules in config.json
    public string GenerateMatchKey(Dictionary<string, string> record)
    {
        var key = string.Empty;

        foreach (var field in _rule.MatchingFields)
        {
            var value = record.TryGetValue(field, out var v) ? v : string.Empty;

            if (_rule.Trim)
                value = value?.Trim() ?? string.Empty;

            if (!_rule.CaseSensitive)
                value = value?.ToUpperInvariant() ?? string.Empty;

            key += value + "|";
        }

        // eg. { "EmployeeId": "EMP001", "EmployeeName": "John Smith", ... }
        // If matchingFields is ["EmployeeId"], the key = "EMP001|"
        // If matchingFields is ["FirstName", "LastName"], the key = "JOHN|SMITH|"
        return key;
    }

    public bool HasMatchingFields(Dictionary<string, string> record)
    {
        return _rule.MatchingFields.All(field => record.ContainsKey(field));
    }
}
