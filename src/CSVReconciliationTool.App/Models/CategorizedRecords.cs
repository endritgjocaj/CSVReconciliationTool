namespace CSVReconciliationTool.App.Models;

public record CategorizedRecords(
    List<Dictionary<string, string>> Matched,
    List<Dictionary<string, string>> OnlyInFolderA,
    List<Dictionary<string, string>> OnlyInFolderB);
