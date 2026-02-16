namespace CSVReconciliationTool.App.Models;

public class ReconciliationConfig
{
    public string FolderA { get; set; } = string.Empty;
    public string FolderB { get; set; } = string.Empty;
    public string OutputFolder { get; set; } = string.Empty;
    public MatchingRule MatchingRule { get; set; } = new();
    public int DegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    public char Separator { get; set; } = ',';
    public bool HasHeaderRow { get; set; } = true;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(FolderA)
            && !string.IsNullOrWhiteSpace(FolderB)
            && !string.IsNullOrWhiteSpace(OutputFolder)
            && MatchingRule.IsValid()
            && DegreeOfParallelism > 0;
    }
}
