using EucRepo.Models;

namespace EucRepo.Interfaces;

public interface IDaasEntitlementRepository
{
    Task<ReportBatch?> GetBatchByIdForUserAsync(Guid? id, string userName);
    Task<List<ReportBatch>> GetBatchForUserAsync(string userName);
}