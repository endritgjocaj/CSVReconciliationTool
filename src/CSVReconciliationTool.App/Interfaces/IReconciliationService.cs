using CSVReconciliationTool.App.Models;

namespace CSVReconciliationTool.App.Interfaces;

public interface IReconciliationService
{
    Task<ReconciliationResult> ReconcileAsync(ReconciliationConfig config);
}
