using System.Globalization;
using AutoMapper;
using CsvHelper;
using EucRepo.Interfaces;
using EucRepo.ModelsFilter;
using EucRepo.ModelsView;
using EucRepo.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EucRepo.Controllers;

[Authorize]
public class EnvironmentController : Controller
{
    private readonly IDaasEntitlementRepository _daasEntitlement;

    public EnvironmentController( IDaasEntitlementRepository daasEntitlement)
    {
        _daasEntitlement = daasEntitlement;
    }


    
    [Authorize]
    public async Task<IActionResult> Entitlements([FromQuery] DaasEntitlementsFilterModel? filterSort)
    {
        filterSort ??= new DaasEntitlementsFilterModel();
        var userName = User.Identity?.Name??"";
        var dataRefreshTime = DateTime.Now;

        var viewModel = new DaasEntitlementsViewModel
        {
            FilterModel = filterSort,
            DataRefreshTime = dataRefreshTime
        };
        if (filterSort.Batch is not null)
        {
            viewModel.ThisBatch = await _daasEntitlement.GetBatchByIdAsync(filterSort.Batch);
            if (viewModel.ThisBatch is null)
                return BadRequest("The provided batch is not found.");

            if (!viewModel.ThisBatch.CanView(userName) && !viewModel.ThisBatch.IsVisibleWithLink)
                return BadRequest("You don't have access to this batch.");
        }

       
        var dto = await _daasEntitlement.GetEntitlementsWithPagingAsync(filterSort, userName, "Entitlements");

        viewModel.TotalRecords = dto.TotalRecords;
        viewModel.Batches = dto.ReportBatches;

        //Store search params for paging buttons. Loop through each of the populated values and store in dictionary
        viewModel.SearchParams = GetSearchParamsForPagingButtons(viewModel.FilterModel);
        viewModel.SearchOptions = dto.SearchOptions;
        viewModel.FilteredRecords = dto.FilteredRecords;

        viewModel.BatchMissingEntries = dto.ThisBatchMissingEntries;

        //Paging
        viewModel.DaasEntitlements = dto.PaginatedList;
        viewModel.StartRecord = dto.PaginatedList!.StartRecord;
        viewModel.EndRecord = dto.PaginatedList.EndRecord;
        viewModel.FirstPage = viewModel.FilterModel.Page == 1;
        viewModel.LastPage = viewModel.FilteredRecords == dto.PaginatedList.EndRecord;
        viewModel.TotalPages = dto.PaginatedList.TotalPages;
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


    [Authorize]
    [Route("[controller]/[action]/{id?}")]
    public async Task<IActionResult> EntitlementsDataFeed([FromQuery] DaasEntitlementsFilterModel? filterSort, string id = "json")
    {
        filterSort ??= new DaasEntitlementsFilterModel();
        var userName = User.Identity?.Name??"";
        if (filterSort.Batch is not null)
        {
            var thisBatch = await _daasEntitlement.GetBatchByIdAsync(filterSort.Batch);
            if (thisBatch is null)
                return BadRequest("The provided batch is not found.");

            if (!thisBatch.CanView(userName) && !thisBatch.IsVisibleWithLink)
                return BadRequest("You don't have access to this batch.");
        }

        var dto = await _daasEntitlement.GetEntitlementsAsync(filterSort, userName, $"EntitlementsDataFeed/{id}");


        if (id != "csv") return Ok(dto.ExportDaasEntitlements);
        
        var filename = "SearchSaveEntitlement_" + DateTime.UtcNow.ToString("yyyy-MM-dd_HHmm") + ".csv";
        var stream = new MemoryStream();
        await using (StreamWriter writeFile =
                     new StreamWriter(stream, System.Text.Encoding.Default, 1000000, leaveOpen: true))
        {
            var csv = new CsvWriter(writeFile, CultureInfo.InvariantCulture, true);
            await csv.WriteRecordsAsync(dto.ExportDaasEntitlements);
        }
        stream.Position = 0; //reset stream
        return File(stream, "application/octet-stream", filename);
    }
    [Authorize]
    public async Task<IActionResult> SaveSearch([FromForm] DaasEntitlementsFilterModel? filterSort)
    {
        filterSort ??= new DaasEntitlementsFilterModel();
        var userName = User.Identity?.Name??"";
        if (filterSort.Batch is not null)
        {
            var thisBatch = await _daasEntitlement.GetBatchByIdAsync(filterSort.Batch);
            if (thisBatch is null)
                return BadRequest("The provided batch is not found.");

            if (!thisBatch.CanView(userName) && !thisBatch.IsVisibleWithLink)
                return BadRequest("You don't have access to this batch.");
        }

        var dto = await _daasEntitlement.GetEntitlementsAsync(filterSort, userName,$"SaveSearch/csv");


        var filename = "SearchSaveEntitlement_" + DateTime.UtcNow.ToString("yyyy-MM-dd_HHmm") + ".csv";
        var stream = new MemoryStream();
        await using (StreamWriter writeFile =
                     new StreamWriter(stream, System.Text.Encoding.Default, 1000000, leaveOpen: true))
        {
            var csv = new CsvWriter(writeFile, CultureInfo.InvariantCulture, true);
            await csv.WriteRecordsAsync(dto.ExportDaasEntitlements);
        }

        stream.Position = 0; //reset stream
        return File(stream, "application/octet-stream", filename);
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


    public IActionResult DirectorView(string id, string farm, string item)
    {
        return Redirect($"/api/DirectorView?id={id}&farm={farm}&item={item}");
    }

}