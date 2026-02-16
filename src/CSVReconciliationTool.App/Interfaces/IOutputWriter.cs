using CSVReconciliationTool.App.Models;

namespace CSVReconciliationTool.App.Interfaces;

public interface IOutputWriter
{
    Task WriteResultsAsync(
        string fileName,
        string outputFolder,
        CategorizedRecords categorized,
        FilePairReconciliationResult summary);
}
