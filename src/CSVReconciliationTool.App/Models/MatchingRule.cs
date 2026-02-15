namespace CSVReconciliationTool.App.Models;

public class MatchingRule
{
    public List<string> MatchingFields { get; set; } = new();
    public bool CaseSensitive { get; set; } = false;
    public bool Trim { get; set; } = true;

    public bool IsValid()
    {
        return MatchingFields.Count > 0;
    }
}
