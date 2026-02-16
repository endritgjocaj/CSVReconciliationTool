namespace CSVReconciliationTool.App.Models;

public class ReconciliationResult
{
    public List<FilePairReconciliationResult> FilePairResults { get; set; } = new();

    public long TotalInFolderA { get; set; }
    public long TotalInFolderB { get; set; }
    public long TotalMatched { get; set; }
    public long TotalOnlyInFolderA { get; set; }
    public long TotalOnlyInFolderB { get; set; }
    public long TotalProcessingTimeMs { get; set; }
    public int SuccessfulPairs { get; set; }
    public int FailedPairs { get; set; }
    public int MissingFiles { get; set; }
    public string? SummaryJsonPath { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
