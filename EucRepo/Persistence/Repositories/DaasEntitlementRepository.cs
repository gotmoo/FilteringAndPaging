using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using EucRepo.Helpers;
using EucRepo.Interfaces;
using EucRepo.Models;
using EucRepo.ModelsExport;
using EucRepo.ModelsFilter;
using Microsoft.EntityFrameworkCore;

namespace EucRepo.Persistence.Repositories;

public class DaasEntitlementRepository : IDaasEntitlementRepository
{
    private readonly SqlDbContext _context;
    private readonly IMapper _mapper;

    public DaasEntitlementRepository(SqlDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ReportBatch?> GetBatchByIdAsync(Guid? id)
    {
        if (id is null)
            return null;
        return await _context.ReportBatches
            .Include(r => r.Owners)
            .Include(r => r.Viewers)
            .AsSplitQuery()
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == id);
    }


    private async Task<List<ReportBatch>> GetBatchForUserAsync(string userName)
    {
        var batches = await _context.ReportBatches.AsNoTracking()
            .Where(e => e.BatchTarget == ReportBatchTarget.EmployeeId || e.BatchTarget == ReportBatchTarget.LanId)
            .Where(e =>
                _context.ReportBatchOwners.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName) ||
                _context.ReportBatchViewers.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName)
            ).ToListAsync();
        return batches;
    }


    private async Task AddBatchRequestLogAsync(ReportBatch batch, string userName, string page)
    {
        _context.ReportBatchRequests.Add(new ReportBatchRequest
        {
            ReportBatch = batch,
            RequestedBy = userName,
            Requested = DateTime.UtcNow,
            Page = page
        });
        await _context.SaveChangesAsync();
    }

    public async Task<DaasEntitlementsDto> GetEntitlementsWithPagingAsync(DaasEntitlementsFilterModel filterModel,
        string userName, string callingPage)
    {
        var dto = new DaasEntitlementsDto();

        var entitlements = _context.DaasEntitlements.AsNoTracking().AsQueryable();
        dto.TotalRecords = await entitlements.CountAsync();

        //Manage initial batch filtering
        dto.ReportBatches = await GetBatchForUserAsync(userName);

        if (filterModel.Batch is not null)
        {
            //Validate access to the batch
            dto.ThisBatch = await GetBatchByIdAsync(filterModel.Batch);
            if (dto.ThisBatch is null)
            {
                dto.BatchRequestError = "The provided batch is not found.";
                return dto;
            }

            if (!dto.ThisBatch.CanView(userName) && !dto.ThisBatch.IsVisibleWithLink)
            {
                dto.BatchRequestError = "You don't have access to this batch.";
                return dto;
            }
            dto.ReportBatches.Add(dto.ThisBatch);
            entitlements =
                await FilterEntitlementsOnBatchMembers((Guid)filterModel.Batch, userName, dto, entitlements,
                    callingPage);
        }

        entitlements = FilterEntitlements(filterModel, entitlements);
        entitlements = SortDaasEntitlements(filterModel, entitlements);

        dto.FilteredRecords = await entitlements.CountAsync();
        dto.SearchOptions = await GetFilterListOptionsAsync(entitlements);

        // Identify items from the batch that are not included in the final filtered set
        if (filterModel.Batch is not null)
            await GetBatchMembersMissingFromFilteredData((Guid)filterModel.Batch, entitlements, dto);

        //Paging
        dto.PaginatedList = await PaginatedList<DaasEntitlement>.CreateAsync(entitlements,
            filterModel.Page ?? 1, filterModel.PageSize);

        return dto;
    }

    public async Task<DaasEntitlementsDto> GetEntitlementsAsync(DaasEntitlementsFilterModel filterModel,
        string userName, string callingPage)
    {
        var dto = new DaasEntitlementsDto();

        var entitlements = _context.DaasEntitlements.AsNoTracking().AsQueryable();
        dto.TotalRecords = await entitlements.CountAsync();
        dto.ReportBatches = await GetBatchForUserAsync(userName);

        //Manage initial batch filtering
        if (filterModel.Batch is not null)
            entitlements =
                await FilterEntitlementsOnBatchMembers((Guid)filterModel.Batch, userName, dto, entitlements,
                    callingPage);

        entitlements = FilterEntitlements(filterModel, entitlements);
        entitlements = SortDaasEntitlements(filterModel, entitlements);
        var results = await entitlements.AsNoTracking()
            .ProjectTo<DaasEntitlementExportModel>(_mapper.ConfigurationProvider).ToListAsync();

        // Identify items from the batch that are not included in the final filtered set
        if (filterModel.Batch is not null)
        {
            var missingEntitlements = await GetMissingEntitlementsForBatchMembersAsync(filterModel, entitlements, dto);
            results.AddRange(missingEntitlements);
        }

        dto.ExportDaasEntitlements = results;
        return dto;
    }

    private async Task<List<DaasEntitlementExportModel>> GetMissingEntitlementsForBatchMembersAsync(
        DaasEntitlementsFilterModel filterModel,
        IQueryable<DaasEntitlement> entitlements, DaasEntitlementsDto dto)
    {
        await GetBatchMembersMissingFromFilteredData((Guid)filterModel.Batch, entitlements, dto);
        var missingEntitlements = new List<DaasEntitlementExportModel>();
        switch (dto.ThisBatch!.BatchTarget)
        {
            case ReportBatchTarget.EmployeeId:
                foreach (var missingEntry in dto.ThisBatchMissingEntries.Where(e => !string.IsNullOrWhiteSpace(e)))
                {
                    missingEntitlements.Add(new DaasEntitlementExportModel()
                    {
                        UserName = "",
                        EmployeeId = Convert.ToInt32(Regex.Match(missingEntry!, @"\d+").Value),
                        DaasName = "NotFound",
                        MachineType = "NotFound"
                    });
                }

                break;
            case ReportBatchTarget.LanId:
                foreach (var missingEntry in dto.ThisBatchMissingEntries.Where(e => !string.IsNullOrWhiteSpace(e)))
                {
                    missingEntitlements.Add(new DaasEntitlementExportModel()
                    {
                        UserName = missingEntry,
                        EmployeeId = Convert.ToInt32(Regex.Match(missingEntry!, @"\d+").Value),
                        DaasName = "NotFound",
                        MachineType = "NotFound"
                    });
                }

                break;
        }

        return missingEntitlements;
    }

    private async Task GetBatchMembersMissingFromFilteredData(Guid batchId,
        IQueryable<DaasEntitlement> entitlements, DaasEntitlementsDto dto)
    {
        var entitlementsMissingCheck = entitlements;
        switch (dto.ThisBatch!.BatchTarget)
        {
            case ReportBatchTarget.EmployeeId:
                dto.ThisBatchMissingEntries = await GetReportBatchEmployeeIds(batchId)
                    .Select(b => b!.Value)
                    .Where(b =>
                        !entitlementsMissingCheck.Select(e => e.EmployeeId)
                            .Contains(b)).Select(i => i.ToString())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToListAsync();
                break;
            case ReportBatchTarget.LanId:
                var nullableResults = await GetReportBatchUserNames(batchId)!
                    .Where(b =>
                        !entitlementsMissingCheck.Select(e => e.UserName)
                            .Contains(b))
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToListAsync();
                dto.ThisBatchMissingEntries = nullableResults!;
                break;
        }
    }

    private async Task<IQueryable<DaasEntitlement>> FilterEntitlementsOnBatchMembers(Guid batchId, string userName,
        DaasEntitlementsDto dto, IQueryable<DaasEntitlement> entitlements, string callingPage)
    {
        dto.ThisBatch = await GetBatchByIdAsync(batchId);
        if (dto.ThisBatch!.CanEdit())
            dto.ThisBatchAccess = "Edit";
        else if (dto.ThisBatch!.CanView())
            dto.ThisBatchAccess = "View";

        switch (dto.ThisBatch.BatchTarget)
        {
            case ReportBatchTarget.EmployeeId:
                entitlements = entitlements.Where(e =>
                    GetReportBatchEmployeeIds(batchId).Contains(e.EmployeeId));
                break;
            case ReportBatchTarget.LanId:
                entitlements = entitlements.Where(e =>
                    GetReportBatchUserNames(batchId)!.Contains(e.UserName));
                break;
        }

        await AddBatchRequestLogAsync(dto.ThisBatch, userName, callingPage);

        return entitlements;
    }

    private IQueryable<string?>? GetReportBatchUserNames(Guid reportBatchId)
    {
        return _context.ReportBatchMembers
            .Where(r => r.ReportBatch.Id == reportBatchId)
            .Select(r => r.LanId).AsQueryable();
    }

    private IQueryable<int?> GetReportBatchEmployeeIds(Guid reportBatchId)
    {
        return _context.ReportBatchMembers
            .Where(r => r.ReportBatch.Id == reportBatchId)
            .Select(r => r.EmployeeId).AsQueryable();
    }

    private static IQueryable<DaasEntitlement> FilterEntitlements(DaasEntitlementsFilterModel filterModel,
        IQueryable<DaasEntitlement> entitlements)
    {
        filterModel ??= new DaasEntitlementsFilterModel();
        if (!string.IsNullOrWhiteSpace(filterModel.AdGroup))
            entitlements =
                entitlements.Where(e => EF.Functions.Like(e.AdGroup ?? string.Empty, $"%{filterModel.AdGroup}%"));
        if (!string.IsNullOrWhiteSpace(filterModel.DaasName))
            entitlements = entitlements.Where(e =>
                EF.Functions.Like(e.DaasName ?? string.Empty, $"%{filterModel.DaasName}%"));
        if (!string.IsNullOrWhiteSpace(filterModel.DcPair))
            entitlements =
                entitlements.Where(e => EF.Functions.Like(e.DcPair ?? string.Empty, $"%{filterModel.DcPair}%"));
        if (!string.IsNullOrWhiteSpace(filterModel.MachineType))
            entitlements = entitlements.Where(e =>
                EF.Functions.Like(e.MachineType ?? string.Empty, $"%{filterModel.MachineType}%"));
        if (!string.IsNullOrWhiteSpace(filterModel.UserName))
            entitlements = entitlements.Where(e =>
                EF.Functions.Like(e.UserName ?? string.Empty, $"%{filterModel.UserName}%"));
        if (!string.IsNullOrWhiteSpace(filterModel.Os))
            entitlements = entitlements.Where(e => EF.Functions.Like(e.Os ?? string.Empty, $"%{filterModel.Os}%"));
        if (!string.IsNullOrWhiteSpace(filterModel.LastSeen))
            if (DateTime.TryParse(filterModel.LastSeen, out DateTime tryDate))
                entitlements = entitlements.Where(e => e.LastSeen >= tryDate && e.LastSeen < (tryDate.AddDays(1)));
        if (!string.IsNullOrWhiteSpace(filterModel.Provisioned))
            if (DateTime.TryParse(filterModel.Provisioned, out DateTime tryDate))
                entitlements =
                    entitlements.Where(e => e.Provisioned >= tryDate && e.Provisioned < (tryDate.AddDays(1)));
        if (!string.IsNullOrWhiteSpace(filterModel.DaysActive))
        {
            var decodedString = HttpUtility.HtmlDecode(filterModel.DaysActive);
            if (int.TryParse(decodedString, out var activeNum))
            {
                entitlements = entitlements.Where(e => e.DaysActive == activeNum);
            }
            else if (decodedString.Length >= 2)
            {
                var filterOp = decodedString.Substring(0, 1);
                var filterOn = decodedString.Substring(1);
                if (int.TryParse(filterOn, out activeNum))
                {
                    switch (filterOp)
                    {
                        case ">":
                            entitlements = entitlements.Where(e => e.DaysActive > activeNum);
                            break;
                        case "<":
                            entitlements = entitlements.Where(e => e.DaysActive < activeNum);
                            break;
                        default:
                            filterModel.DaysActive = "";
                            break;
                    }
                }
            }
            else
            {
                filterModel.DaysActive = "";
            }
        }

        return entitlements;
    }

    private static IQueryable<DaasEntitlement> SortDaasEntitlements(DaasEntitlementsFilterModel filterModel,
        IQueryable<DaasEntitlement> entitlements)
    {
        filterModel.Order ??= "asc";
        switch (filterModel.OrderBy)
        {
            case "EmployeeID":
                entitlements = filterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.EmployeeId)
                    : entitlements.OrderByDescending(e => e.EmployeeId);
                break;
            case "EmployeeStatus":
                entitlements = filterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.EmployeeStatus)
                    : entitlements.OrderByDescending(e => e.EmployeeStatus);
                break;
            case "UserName":
                entitlements = filterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.UserName)
                    : entitlements.OrderByDescending(e => e.UserName);
                break;
            case "DcPair":
                entitlements = filterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.DcPair)
                    : entitlements.OrderByDescending(e => e.DcPair);
                break;
            case "DaasName":
                entitlements = filterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.DaasName)
                    : entitlements.OrderByDescending(e => e.DaasName);
                break;
            case "Os":
                entitlements = filterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.Os)
                    : entitlements.OrderByDescending(e => e.Os);
                break;
            case "LastSeen":
                entitlements = filterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.LastSeen)
                    : entitlements.OrderByDescending(e => e.LastSeen);
                break;
            case "DaysActive":
                entitlements = filterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.DaysActive)
                    : entitlements.OrderByDescending(e => e.DaysActive);
                break;
            case "Provisioned":
                entitlements = filterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.Provisioned)
                    : entitlements.OrderByDescending(e => e.Provisioned);
                break;
        }

        return entitlements;
    }

    private async Task<Dictionary<string, string[]>> GetFilterListOptionsAsync(
        IQueryable<DaasEntitlement> entitlements)
    {
        var filterListOptions = await entitlements.Select(d => new
        {
            d.AdGroup,
            d.DaasName,
            d.DcPair,
            d.MachineType,
            d.Os
        }).Distinct().ToListAsync();
        Dictionary<string, string[]> searchOptions = new();
        searchOptions.Add("AdGroup",
            filterListOptions.OrderBy(d => d.AdGroup).Select(d => d.AdGroup).Distinct().ToArray()!);
        searchOptions.Add("DaasName",
            filterListOptions.OrderBy(d => d.DaasName).Select(d => d.DaasName).Distinct().ToArray()!);
        searchOptions.Add("DcPair",
            filterListOptions.OrderBy(d => d.DcPair).Select(d => d.DcPair).Distinct().ToArray()!);
        searchOptions.Add("MachineType",
            filterListOptions.OrderBy(d => d.MachineType).Select(d => d.MachineType).Distinct().ToArray()!);
        searchOptions.Add("Os",
            filterListOptions.OrderBy(d => d.Os).Select(d => d.Os).Distinct().ToArray()!);

        return searchOptions;
    }
}