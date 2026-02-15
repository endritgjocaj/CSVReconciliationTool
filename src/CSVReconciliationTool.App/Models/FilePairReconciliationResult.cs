namespace CSVReconciliationTool.App.Models;

public class FilePairReconciliationResult
{
    public string FileName { get; set; } = string.Empty;
    public long TotalInFolderA { get; set; }
    public long TotalInFolderB { get; set; }
    public long MatchedCount { get; set; }
    public long OnlyInFolderACount { get; set; }
    public long OnlyInFolderBCount { get; set; }
    public long ProcessingTimeMs { get; set; }
}
