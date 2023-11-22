using EucRepo.Models;
using EucRepo.ModelsFilter;
using EucRepo.Persistence.Repositories;

namespace EucRepo.Interfaces;

public interface IDaasEntitlementRepository
{
    Task<ReportBatch?> GetBatchByIdForUserAsync(Guid? id, string userName);
    Task<ReportBatch?> GetBatchByIdAsync(Guid? id);
    Task<List<ReportBatch>> GetBatchForUserAsync(string userName);
    Task<int> GetTotalEntitlementsCountAsync();
    Task AddBatchRequestLogAsync(ReportBatch batch, string userName, string page);
    Task<DaasEntitlementsDto> GetEntitlementsWithPagingAsync(DaasEntitlementsFilterModel filterModel, string userName, string callingPage);
    Task<DaasEntitlementsDto> GetEntitlementsAsync(DaasEntitlementsFilterModel filterModel, string userName, string callingPage);
}