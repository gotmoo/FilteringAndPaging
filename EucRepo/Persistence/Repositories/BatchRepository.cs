using EucRepo.Helpers;
using EucRepo.Interfaces;
using EucRepo.Models;
using Microsoft.EntityFrameworkCore;

namespace EucRepo.Persistence.Repositories;

public class BatchRepository : IBatchRepository
{
    private readonly SqlDbContext _context;

    public BatchRepository(SqlDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReportBatch>> GetAllBatchesForCurrentUserAsync(string? userName)
    {
        return await _context.ReportBatches.AsNoTracking()
            .Include(r => r.Owners)
            .Include(r => r.Viewers).AsSplitQuery()
            .Include(r => r.Members)
            .Where(e => e.BatchTarget == ReportBatchTarget.EmployeeId || e.BatchTarget == ReportBatchTarget.LanId)
            .Where(e =>
                _context.ReportBatchOwners.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName) ||
                _context.ReportBatchViewers.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName)
            ).ToListAsync();
    }

    public async Task<ReportBatch?> GetBatchByIdAsync(Guid batchId, bool forEdit = false)
    {
        var batch = _context.ReportBatches.AsQueryable();
        if (!forEdit)
            batch = batch.AsNoTracking();
        return await batch
            .Include(r => r.Owners)
            .Include(r => r.Viewers).AsSplitQuery()
            .Include(r => r.Members)
            .Include(r => r.Requests).AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == batchId);
    }

    public async Task DeleteBatchByIdAsync(Guid batchId, string userName)
    {
        var batch = await GetBatchByIdAsync(batchId, true);
        
        if (batch is null)
            return ;
        if (!batch.CanEdit(userName))
            return ;
        _context.RemoveRange(batch.Members);
        _context.RemoveRange(batch.Owners);
        _context.RemoveRange(batch.Requests);
        _context.RemoveRange(batch.Viewers);
        _context.ReportBatches.Remove(batch);
        await _context.SaveChangesAsync();
        
    }

    public async Task<ReportBatchForm> UpdateBatchByIdFromReportBatchFormAsync(Guid batchId, ReportBatchForm reportBatch)
    {
        var batch = await GetBatchByIdAsync(batchId, true);
        if (batch is null)
        {
            batch = new ReportBatch
            {
                Id = reportBatch.Id,
                Name = reportBatch?.Name ?? "",
                Description = reportBatch?.Description ?? "",
                IsManaged = reportBatch!.IsManaged,
                IsVisibleWithLink = reportBatch!.IsVisibleWithLink,
                BatchTarget = reportBatch.BatchTarget,
                Created = DateTime.UtcNow,
                CreatedBy = reportBatch.CreatedBy,
                Members = new List<ReportBatchMember>(),
                Viewers = new List<ReportBatchViewer>(),
                Owners = new List<ReportBatchOwner>()
            };
            _context.ReportBatches.Add(batch);
            reportBatch.NewBatch = false;
        }
        else
        {
            await _context.ReportBatchMembers.Where(b => b.ReportBatch == batch).ExecuteDeleteAsync();
            await _context.ReportBatchOwners.Where(b => b.ReportBatch == batch).ExecuteDeleteAsync();
            await _context.ReportBatchViewers.Where(b => b.ReportBatch == batch).ExecuteDeleteAsync();
            batch = _context.ReportBatches.First(b => b.Id == reportBatch.Id);
            batch.Name = reportBatch.Name ?? "";
            batch.Description = reportBatch.Description ?? "";
            batch.BatchTarget = reportBatch.BatchTarget;
            batch.IsManaged = reportBatch.IsManaged;
            batch.IsVisibleWithLink = reportBatch.IsVisibleWithLink;
        }
        foreach (var item in reportBatch.Owners.SplitToStringArray().Where(s => !string.IsNullOrWhiteSpace(s)))
            batch.Owners.Add(new ReportBatchOwner() { UserName = item });

        foreach (var item in reportBatch.Viewers.SplitToStringArray().Where(s => !string.IsNullOrWhiteSpace(s)))
            batch.Viewers.Add(new ReportBatchViewer() { UserName = item });

        switch (batch.BatchTarget)
        {
            case ReportBatchTarget.EmployeeId:
                foreach (var item in reportBatch.Members.SplitToStringArray().Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    if (int.TryParse(item, out var parsedId))
                         batch.Members.Add(new ReportBatchMember() { EmployeeId = parsedId });
                }
                break;
            case ReportBatchTarget.LanId:
                foreach (var item in reportBatch.Members.SplitToStringArray().Where(s => !string.IsNullOrWhiteSpace(s)))
                    batch.Members.Add(new ReportBatchMember() { LanId = item });
                break;
        }
        await _context.SaveChangesAsync();

        return reportBatch;
    }
}