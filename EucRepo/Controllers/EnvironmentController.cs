using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using CsvHelper;
using EucRepo.Helpers;
using EucRepo.Models;
using EucRepo.ModelsExport;
using EucRepo.ModelsFilter;
using EucRepo.ModelsView;
using EucRepo.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EucRepo.Controllers;

[Authorize]
public class EnvironmentController : Controller
{
    private readonly SqlDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public EnvironmentController(SqlDbContext context, IMapper mapper, IConfiguration configuration)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
    }

    // GET

    

    [Authorize]
    [Route("[controller]/[action]/{id?}")]
    public IActionResult EntitlementsDataFeed([FromQuery] DaasEntitlementsFilterModel? filterSort, string id = "json")
    {
        filterSort ??= new DaasEntitlementsFilterModel();
        var missingEntitlements = new List<DaasEntitlementExportModel>();
        var entitlements = _context.DaasEntitlements.AsQueryable();
        entitlements = FilterEntitlements(filterSort, entitlements);
        var userName = User.Identity?.Name??"";
        var batch = _context.ReportBatches
            .Where(e => e.BatchTarget == ReportBatchTarget.EmployeeId || e.BatchTarget == ReportBatchTarget.LanId)
            .FirstOrDefault(e =>
                e.Id == filterSort.Batch && (
                    e.IsVisibleWithLink ||
                    _context.ReportBatchOwners.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                        .Contains(userName) ||
                    _context.ReportBatchViewers.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                        .Contains(userName)
                )
            );
        if (filterSort.Batch is not null)
        {
            if (batch is null)
            {
                return Unauthorized("You don't have access to this collection");
            }
            batch.LastRequested = DateTime.UtcNow;
            batch.LastRequestedBy = userName;
            _context.ReportBatchRequests.Add(new ReportBatchRequest
            {
                ReportBatch = batch,
                RequestedBy = userName,
                Requested = DateTime.UtcNow,
                Page = $"EntitlementsDataFeed/{id}"
            });
            _context.SaveChanges();

            List<string> missingEntries = new();
            if (batch.BatchTarget == ReportBatchTarget.EmployeeId)
            {
                var batchQueryEmployeeId = _context.ReportBatchMembers
                    .Where(r => r.ReportBatch.Id == filterSort.Batch && r.EmployeeId != null)
                    .Select(r => r.EmployeeId!.Value).AsQueryable();
                entitlements = entitlements.Where(e =>
                    batchQueryEmployeeId.Contains(e.EmployeeId));

                missingEntries = batchQueryEmployeeId.Where(b =>
                    !entitlements.Select(e => e.EmployeeId)
                        .Contains(b)).Select(i => i.ToString()).ToList();
                foreach (var missingEntry in missingEntries.Where(e => !string.IsNullOrWhiteSpace(e)))
                {
                    missingEntitlements.Add(new DaasEntitlementExportModel()
                    {
                        UserName = "",
                        EmployeeId = Convert.ToInt32(Regex.Match(missingEntry!, @"\d+").Value),
                        DaasName = "NotFound",
                        MachineType = "NotFound"
                    });
                }
            }
            else
            {
                var batchQueryLanId = _context.ReportBatchMembers.Where(r => r.ReportBatch.Id == filterSort.Batch)
                    .Select(r => r.LanId).AsQueryable();
                entitlements = entitlements.Where(e =>
                    batchQueryLanId.Contains(e.UserName));
                missingEntries = batchQueryLanId.Where(b =>
                    !entitlements.Select(e => e.UserName)
                        .Contains(b)).Select(i => i!.ToString()).ToList();
                foreach (var missingEntry in missingEntries.Where(e => !string.IsNullOrWhiteSpace(e)))
                {
                    missingEntitlements.Add(new DaasEntitlementExportModel()
                    {
                        UserName = missingEntry,
                        EmployeeId = Convert.ToInt32(Regex.Match(missingEntry!, @"\d+").Value),
                        DaasName = "NotFound",
                        MachineType = "NotFound"
                    });
                }
            }
        }

        var results = entitlements.AsNoTracking()
            .ProjectTo<DaasEntitlementExportModel>(_mapper.ConfigurationProvider).ToList();
        results.AddRange(missingEntitlements);

        if (id != "csv") return Ok(results);
        var filename = "SearchSaveEntitlement_" + DateTime.UtcNow.ToString("yyyy-MM-dd_HHmm") + ".csv";
        var stream = new MemoryStream();
        using (StreamWriter writeFile =
               new StreamWriter(stream, System.Text.Encoding.Default, 1000000, leaveOpen: true))
        {
            var csv = new CsvWriter(writeFile, CultureInfo.InvariantCulture, true);
            csv.WriteRecordsAsync(results);
        }

        stream.Position = 0; //reset stream
        return File(stream, "application/octet-stream", filename);
    }

    [Authorize]
    public IActionResult Entitlements([FromQuery] DaasEntitlementsFilterModel? filterSort)
    {
        filterSort ??= new DaasEntitlementsFilterModel();
        var userName = User.Identity?.Name??"";
        var dataRefreshTime = DateTime.Now;
        var viewModel = new DaasEntitlementsViewModel
        {
            FilterModel = filterSort,
            DataRefreshTime = dataRefreshTime
        };

        var entitlements = _context.DaasEntitlements.AsQueryable();
        viewModel.TotalRecords = entitlements.Count();
        var batches = _context.ReportBatches
            .Where(e => e.BatchTarget == ReportBatchTarget.EmployeeId || e.BatchTarget == ReportBatchTarget.LanId)
            .Where(e =>
                _context.ReportBatchOwners.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName) ||
                _context.ReportBatchViewers.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName)
            ).ToList();
        viewModel.Batches = batches;

        //Store search params for paging buttons. Loop through each of the populated values and store in dictionary
        viewModel.SearchParams = GetSearchParamsForPagingButtons(viewModel.FilterModel);

        //Filtering
        if (filterSort.Batch is not null)
        {
            viewModel.ThisBatch = _context.ReportBatches.Include(r => r.Owners).Include(r => r.Viewers)
                .FirstOrDefault(r => r.Id == filterSort.Batch);
            if (viewModel.ThisBatch is null)
            {
                return BadRequest("The provided batch is not found.");
            }

            if (!viewModel.ThisBatch.CanView(userName) && !viewModel.ThisBatch.IsVisibleWithLink)
            {
                return BadRequest("You don't have access to this batch.");
            }
            viewModel.ThisBatch.LastRequested = DateTime.UtcNow;
            viewModel.ThisBatch.LastRequestedBy = userName;
            _context.ReportBatchRequests.Add(new ReportBatchRequest
            {
                ReportBatch = viewModel.ThisBatch,
                RequestedBy = userName,
                Requested = DateTime.UtcNow,
                Page = "Entitlements"
            });
            _context.SaveChanges();
            if (!viewModel.Batches.Contains(viewModel.ThisBatch)) viewModel.Batches.Add(viewModel.ThisBatch);

            if (viewModel.ThisBatch.BatchTarget == ReportBatchTarget.EmployeeId)
            {
                var batchQueryEmployeeId = _context.ReportBatchMembers
                    .Where(r => r.ReportBatch.Id == filterSort.Batch && r.EmployeeId != null)
                    .Select(r => r.EmployeeId!.Value).AsQueryable();
                entitlements = entitlements.Where(e =>
                    batchQueryEmployeeId.Contains(e.EmployeeId));
            }
            else
            {
                var batchQueryLanId = _context.ReportBatchMembers.Where(r => r.ReportBatch.Id == filterSort.Batch)
                    .Select(r => r.LanId).AsQueryable();
                entitlements = entitlements.Where(e =>
                    batchQueryLanId.Contains(e.UserName));
            }
        }

        entitlements = FilterEntitlements(viewModel.FilterModel, entitlements);
        viewModel.FilteredRecords = entitlements.Count();

        //Select available options for datalist boxes. Store in dictionary
        var filterListOptions = entitlements.Select(d => new
        {
            d.AdGroup,
            d.DaasName,
            d.DcPair,
            d.MachineType,
            d.Os
        }).Distinct().ToList();
        viewModel.SearchOptions.Add("AdGroup",
            filterListOptions.OrderBy(d => d.AdGroup).Select(d => d.AdGroup).Distinct().ToArray()!);
        viewModel.SearchOptions.Add("DaasName",
            filterListOptions.OrderBy(d => d.DaasName).Select(d => d.DaasName).Distinct().ToArray()!);
        viewModel.SearchOptions.Add("DcPair",
            filterListOptions.OrderBy(d => d.DcPair).Select(d => d.DcPair).Distinct().ToArray()!);
        viewModel.SearchOptions.Add("MachineType",
            filterListOptions.OrderBy(d => d.MachineType).Select(d => d.MachineType).Distinct().ToArray()!);
        viewModel.SearchOptions.Add("Os", filterListOptions.OrderBy(d => d.Os).Select(d => d.Os).Distinct().ToArray()!);
        viewModel.SearchOptions.Add("Batch", batches.Select(b => b.Id.ToString()).ToArray()!);
        //sorting
        entitlements = SortDaasEntitlements(viewModel, entitlements);

        //extract items missing if this is a batch
        if (filterSort.Batch is not null)
        {
            if (viewModel.ThisBatch?.BatchTarget == ReportBatchTarget.EmployeeId)
            {
                var batchQueryEmployeeId = _context.ReportBatchMembers
                    .Where(r => r.ReportBatch.Id == filterSort.Batch && r.EmployeeId != null)
                    .Select(r => r.EmployeeId!.Value).AsQueryable();
                var missingEntries = batchQueryEmployeeId.Where(b =>
                    !entitlements.Select(e => e.EmployeeId)
                        .Contains(b));
                //
                viewModel.BatchMissingEntries = missingEntries.Select(i => i.ToString()).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            }
            else
            {
                var batchQueryLanId = _context.ReportBatchMembers.Where(r => r.ReportBatch.Id == filterSort.Batch)
                    .Select(r => r.LanId).AsQueryable();

                var missingEntries = batchQueryLanId.Where(b =>
                    !entitlements.Select(e => e.UserName)
                        .Contains(b));
                viewModel.BatchMissingEntries = missingEntries.Where(e => !string.IsNullOrWhiteSpace(e)).ToList()!;
            }
        }

        //Paging
        var pagedRecords = PaginatedList<DaasEntitlement>.Create(entitlements.AsNoTracking(),
            viewModel.FilterModel.Page ?? 1, viewModel.FilterModel.PageSize);

        viewModel.DaasEntitlements = pagedRecords;
        viewModel.StartRecord = pagedRecords.StartRecord;
        viewModel.EndRecord = pagedRecords.EndRecord;
        viewModel.FirstPage = viewModel.FilterModel.Page == 1;
        viewModel.LastPage = viewModel.FilteredRecords == pagedRecords.EndRecord;
        viewModel.TotalPages = pagedRecords.TotalPages;
        viewModel.FilterModel.Page = viewModel.FilterModel.Page ?? 1;
        List<int> pageSizes = new List<int>
        {
            10,
            25,
            50,
            100,
            250,
            500
        };
        ViewBag.SelectPageSize = new SelectList(pageSizes, viewModel.FilterModel.PageSize);
        ViewBag.SelectPages = new SelectList(Enumerable.Range(1, viewModel.TotalPages), filterSort.Page);
        ViewBag.SelectBatch = new SelectList(viewModel.Batches, "Id", "DisplayName", filterSort.Batch.ToString());
        return View(viewModel);
    }

    private static IQueryable<DaasEntitlement> SortDaasEntitlements(DaasEntitlementsViewModel viewModel,
        IQueryable<DaasEntitlement> entitlements)
    {
        viewModel.FilterModel.Order ??= "asc";
        switch (viewModel.FilterModel.OrderBy)
        {
            case "EmployeeID":
                entitlements = viewModel.FilterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.EmployeeId)
                    : entitlements.OrderByDescending(e => e.EmployeeId);
                break;
            case "EmployeeStatus":
                entitlements = viewModel.FilterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.EmployeeStatus)
                    : entitlements.OrderByDescending(e => e.EmployeeStatus);
                break;
            case "UserName":
                entitlements = viewModel.FilterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.UserName)
                    : entitlements.OrderByDescending(e => e.UserName);
                break;
            case "DcPair":
                entitlements = viewModel.FilterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.DcPair)
                    : entitlements.OrderByDescending(e => e.DcPair);
                break;
            case "DaasName":
                entitlements = viewModel.FilterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.DaasName)
                    : entitlements.OrderByDescending(e => e.DaasName);
                break;
            case "Os":
                entitlements = viewModel.FilterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.Os)
                    : entitlements.OrderByDescending(e => e.Os);
                break;
            case "LastSeen":
                entitlements = viewModel.FilterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.LastSeen)
                    : entitlements.OrderByDescending(e => e.LastSeen);
                break;
            case "DaysActive":
                entitlements = viewModel.FilterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.DaysActive)
                    : entitlements.OrderByDescending(e => e.DaysActive);
                break;
            case "Provisioned":
                entitlements = viewModel.FilterModel.Order == "asc"
                    ? entitlements.OrderBy(e => e.Provisioned)
                    : entitlements.OrderByDescending(e => e.Provisioned);
                break;
        }

        return entitlements;
    }

    private static Dictionary<string, string?> GetSearchParamsForPagingButtons(DaasEntitlementsFilterModel filterModel)
    {
        var searchParams = new Dictionary<string, string?>();

        foreach (var prop in filterModel.GetType().GetProperties())
        {
            if (prop.PropertyType == typeof(int))
            {
                searchParams.Add(prop.Name, prop.GetValue(filterModel)?.ToString());
            }
            else if (prop.PropertyType == typeof(string) &&
                     !string.IsNullOrWhiteSpace(prop.GetValue(filterModel)?.ToString()))
            {
                searchParams.Add(prop.Name, prop.GetValue(filterModel)?.ToString());
            }
            else if (prop.PropertyType == typeof(Guid?))
            {
                searchParams.Add(prop.Name, prop.GetValue(filterModel)?.ToString());
            }
        }

        return searchParams;
    }

    
    private static IQueryable<DaasEntitlement> FilterEntitlements(DaasEntitlementsFilterModel? filterModel,
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

   
    public async Task<IActionResult> SaveSearch([FromForm] DaasEntitlementsFilterModel? filterSort)
    {
        var entitlements = _context.DaasEntitlements.AsNoTracking().AsQueryable();

        entitlements = FilterEntitlements(filterSort, entitlements);
        var result = await entitlements.ToListAsync();

        var map = _mapper.Map<List<DaasEntitlementExportModel>>(result);
        var filename = "SearchSaveEntitlement_" + DateTime.UtcNow.ToString("yyyy-MM-dd_HHmm") + ".csv";
        var stream = new MemoryStream();
        await using (StreamWriter writeFile =
                     new StreamWriter(stream, System.Text.Encoding.Default, 1000000, leaveOpen: true))
        {
            var csv = new CsvWriter(writeFile, CultureInfo.InvariantCulture, true);
            await csv.WriteRecordsAsync(map);
        }

        stream.Position = 0; //reset stream
        return File(stream, "application/octet-stream", filename);
    }

    public IActionResult DirectorView(string id, string farm, string item)
    {
        return Redirect($"/api/DirectorView?id={id}&farm={farm}&item={item}");
    }

}