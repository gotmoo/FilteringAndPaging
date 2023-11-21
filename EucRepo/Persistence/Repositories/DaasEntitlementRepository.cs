using EucRepo.Interfaces;
using EucRepo.Models;
using Microsoft.EntityFrameworkCore;

namespace EucRepo.Persistence.Repositories;

public class DaasEntitlementRepository : IDaasEntitlementRepository
{
    private readonly SqlDbContext _context;

    public DaasEntitlementRepository(SqlDbContext context)
    {
        _context = context;
    }
    public async Task<ReportBatch?> GetBatchByIdForUserAsync(Guid? id, string userName)
    {
        if (id is null) 
            return null;
        var batch = await _context.ReportBatches
            .Where(e => e.BatchTarget == ReportBatchTarget.EmployeeId || e.BatchTarget == ReportBatchTarget.LanId)
            .FirstOrDefaultAsync(e =>
                e.Id == id && (
                    e.IsVisibleWithLink ||
                    _context.ReportBatchOwners.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                        .Contains(userName) ||
                    _context.ReportBatchViewers.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                        .Contains(userName)
                )
            );
        return batch;
    }

    public async Task<ReportBatch?> GetBatchByIdAsync(Guid? id)
    {
        if (id is null)
            return null;
        return await _context.ReportBatches.Include(r => r.Owners).Include(r => r.Viewers)
            .FirstOrDefaultAsync(r => r.Id == id);
    }


    public async Task<List<ReportBatch>> GetBatchForUserAsync(string userName)
    {
        var batches  = await _context.ReportBatches
            .Where(e => e.BatchTarget == ReportBatchTarget.EmployeeId || e.BatchTarget == ReportBatchTarget.LanId)
            .Where(e =>
                _context.ReportBatchOwners.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName) ||
                _context.ReportBatchViewers.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName)
            ).ToListAsync();
        return batches;
    }

    public async Task<int> GetTotalEntitlementsCountAsync()
    {
        return await _context.DaasEntitlements.CountAsync();
    }

    public async Task AddBatchRequestLogAsync(ReportBatch batch, string userName, string page)
    {
        _context.ReportBatchRequests.Add(new ReportBatchRequest
        {
            ReportBatch = batch,
            RequestedBy = userName,
            Requested = DateTime.UtcNow,
            Page = page
        });
        await _context.SaveChangesAsync();    }
}