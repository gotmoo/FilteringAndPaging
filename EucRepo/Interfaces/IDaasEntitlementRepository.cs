using EucRepo.Models;
using EucRepo.ModelsFilter;
using EucRepo.Persistence.Repositories;

namespace EucRepo.Interfaces;

public interface IDaasEntitlementRepository
{
    Task<ReportBatch?> GetBatchByIdAsync(Guid? id);
    Task<DaasEntitlementsDto> GetEntitlementsWithPagingAsync(DaasEntitlementsFilterModel filterModel, string userName, string callingPage);
    Task<DaasEntitlementsDto> GetEntitlementsAsync(DaasEntitlementsFilterModel filterModel, string userName, string callingPage);
}