namespace CSVReconciliationTool.App.Interfaces;

public interface IMatchingService
{
    string GenerateMatchKey(Dictionary<string, string> record);
    bool HasMatchingFields(Dictionary<string, string> record);
}
