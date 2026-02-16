using CSVReconciliationTool.App.Models;

namespace CSVReconciliationTool.App.Interfaces;

public interface IFilePairProcessor
{
    Task<FilePairReconciliationResult> ReconcileAsync(string fileName, string? pathA, string? pathB, string outputFolder);
}
