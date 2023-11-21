using EucRepo.Models;

namespace EucRepo.Interfaces;

public interface IDaasEntitlementRepository
{
    Task<ReportBatch?> GetBatchByIdForUserAsync(Guid? id, string userName);
    Task<ReportBatch?> GetBatchByIdAsync(Guid? id);
    Task<List<ReportBatch>> GetBatchForUserAsync(string userName);
    Task<int> GetTotalEntitlementsCountAsync();
    Task AddBatchRequestLogAsync(ReportBatch batch, string userName, string page);
}