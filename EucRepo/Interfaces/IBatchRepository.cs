using EucRepo.Models;

namespace EucRepo.Interfaces;

public interface IBatchRepository
{
 Task<List<ReportBatch>> GetAllBatchesForCurrentUserAsync(string? userName);
 Task<ReportBatch?> GetBatchByIdAsync(Guid batchId, bool forEdit = false);
 Task DeleteBatchByIdAsync(Guid batchId, string userName);
 Task<ReportBatchForm> UpdateBatchByIdFromReportBatchFormAsync(Guid batchId, ReportBatchForm reportBatch);
}