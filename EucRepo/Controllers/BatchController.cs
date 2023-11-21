using System.Diagnostics;
using EucRepo.Helpers;
using EucRepo.Models;
using EucRepo.ModelsView;
using EucRepo.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EucRepo.Controllers;

[Authorize]
public class BatchController : Controller
{
    private readonly SqlDbContext _context;

    // GET
    public BatchController(SqlDbContext context)
    {
        _context = context;
    }

    [Authorize]
    public IActionResult Index()
    {
        var userName = User.Identity?.Name;
        var batches = _context.ReportBatches
            .Include(r => r.Owners)
            .Include(r => r.Viewers).AsSplitQuery()
            .Include(r => r.Members)
            .Where(e => e.BatchTarget == ReportBatchTarget.EmployeeId || e.BatchTarget == ReportBatchTarget.LanId)
            .Where(e =>
                _context.ReportBatchOwners.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName) ||
                _context.ReportBatchViewers.Where(o => o.ReportBatch.Id == e.Id).Select(o => o.UserName)
                    .Contains(userName)
            ).ToList();

        var viewModel = new BatchListViewModel()
        {
            ReportBatches = batches
        };
        return View(viewModel);
    }

    [Authorize]
    public IActionResult View(Guid id, string view = "full")
    {
        var thisUser = User.Identity?.Name ?? "";
        var batch = _context.ReportBatches
            .Include(r => r.Owners)
            .Include(r => r.Viewers).AsSplitQuery()
            .Include(r => r.Members)
            .FirstOrDefault(r => r.Id == id);
        if (batch is null)
            return NotFound($"Batch with id {id} not found.");
        if (!batch.CanView(thisUser))
            return Unauthorized($"Not allowed to access {batch.DisplayName}: {id}.");
        if (view == "partial")
            return PartialView(batch);
        return View(batch);
    }

    [Authorize]
    [Route("/[controller]/Manage/New")]
    public IActionResult ManageNewRedirect()
    {
        Guid? id = null;
        return RedirectToAction("Manage", new { id });
    }

    [Authorize]
    [Route("/[controller]/Manage/{id:guid?}")]
    public IActionResult Manage([FromRoute] Guid? id)
    {
        if (id == null)
        {
            var vm = new ReportBatchForm
            {
                Id = Guid.NewGuid(),
                NewBatch = true,
                CreatedBy = @User.Identity?.Name ?? string.Empty,
                Owners = @User.Identity?.Name,
                Name = NewNameGenerator.GenerateRandomName(),
                Description = "",
                BatchTarget = ReportBatchTarget.LanId,
                IsVisibleWithLink = false
            };
            return View(vm);
        }


        var batch = _context.ReportBatches
            .Where(b => b.Id == id)
            .Include(b => b.Members)
            .Include(b => b.Viewers)
            .Include(b => b.Owners)
            .FirstOrDefault();
        if (batch is null)
            return RedirectToAction("Index");

        // if (!batch.CanEdit())
        //     return RedirectToAction("View", new{id});

        var batchMembers = batch!.BatchTarget == ReportBatchTarget.EmployeeId
            ? batch.Members.Select(b => b.EmployeeId.ToString())
            : batch.Members.Select(b => b.LanId);

        var batchVm = new ReportBatchForm
        {
            Id = batch!.Id,
            Name = batch.Name,
            Description = batch.Description,
            IsManaged = batch.IsManaged,
            IsVisibleWithLink = batch.IsVisibleWithLink,
            Created = batch.Created,
            BatchTarget = batch.BatchTarget,
            CreatedBy = batch.CreatedBy,
            Owners = batch.Owners.Select(o => o.UserName).JoinToList(),
            Members = string.Join("\n", batchMembers),
            Viewers = batch.Viewers.Select(o => o.UserName).JoinToList(),
            Message = null
        };
        var x = batch.Owners.Select(o => o.UserName);
        return View(batchVm);
    }

    [HttpPost]
    [Route("/[controller]/Manage/{id:guid?}")]
    public async Task<IActionResult> Manage([FromForm] ReportBatchForm reportBatch, [FromRoute] Guid? id)
    {
        if (!ModelState.IsValid)
        {
            var query = from state in ModelState.Values
                from error in state.Errors
                select error.ErrorMessage;
            var errorMessages = query.ToList();
            return BadRequest($"Model not valid:\n{string.Join("\n", errorMessages)}");
        }

        var batch = _context.ReportBatches.FirstOrDefault(b => b.Id == reportBatch.Id);
        if (batch is null)
        {
            Debug.Assert(reportBatch != null, nameof(reportBatch) + " != null");
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
            await _context.SaveChangesAsync();
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
        {
            batch.Owners.Add(new ReportBatchOwner() { UserName = item });
        }

        foreach (var item in reportBatch.Viewers.SplitToStringArray().Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            batch.Viewers.Add(new ReportBatchViewer() { UserName = item });
        }

        foreach (var item in reportBatch.Members.SplitToStringArray().Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            if (batch.BatchTarget == ReportBatchTarget.EmployeeId && int.TryParse(item, out int parsedId))
            {
                batch.Members.Add(new ReportBatchMember() { EmployeeId = parsedId });
            }

            if (batch.BatchTarget == ReportBatchTarget.LanId)
            {
                batch.Members.Add(new ReportBatchMember() { LanId = item });
            }
        }

        await _context.SaveChangesAsync();
        reportBatch.NewBatch = false;

        return View(reportBatch);
    }

    [Authorize]
    [HttpGet]
    public IActionResult Delete(Guid id)
    {
        var thisUser = User.Identity?.Name ?? "";
        var batch = _context.ReportBatches
            .Include(r => r.Owners)
            .Include(r => r.Viewers).AsSplitQuery()
            .Include(r => r.Members).AsSplitQuery()
            .Include(r => r.Requests)
            .FirstOrDefault(r => r.Id == id);
        if (batch is null)
            return RedirectToAction("Index");
        if (!batch.CanEdit(thisUser))
            return RedirectToAction("Index");

        return View(batch);
    }

    [Authorize]
    [HttpPost]
    [Route("[Controller]/Delete/{id:guid}")]
    public IActionResult DeletePost(Guid id)
    {
        var thisUser = User.Identity?.Name ?? "";
        var batch = _context.ReportBatches
            .Include(r => r.Owners)
            .FirstOrDefault(r => r.Id == id);
        if (batch is null)
            return RedirectToAction("Index");
        if (!batch.CanEdit(thisUser))
            return RedirectToAction("Index");
        // ReSharper disable MethodHasAsyncOverload
        _context.ReportBatchMembers.Where(b => b.ReportBatch == batch).ExecuteDelete();
        _context.ReportBatchViewers.Where(b => b.ReportBatch == batch).ExecuteDelete();
        _context.ReportBatchRequests.Where(b => b.ReportBatch == batch).ExecuteDelete();
        _context.ReportBatches.Remove(batch);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [Authorize]
    public IActionResult CreateNewBatch(ReportBatchTarget target, int memberCount = 1000)
    {
        List<ReportBatchOwner> owners = new List<ReportBatchOwner>
        {
            new()
            {
                UserName = @User.Identity?.Name ?? ""
            }
        };

        var vm = new ReportBatch()
        {
            Id = Guid.NewGuid(),
            CreatedBy = @User.Identity?.Name ?? string.Empty,
            Owners = owners,
            Name = NewNameGenerator.GenerateRandomName(),
            Description = $"A list of {memberCount} items of type {target}",
            BatchTarget = target,
            IsVisibleWithLink = true
        };
        _context.Add(vm);
        _context.SaveChanges();
        switch (target)
        {
            case ReportBatchTarget.EmployeeId:
                _context.Database.ExecuteSql($"""
                                              ;with cte as (SELECT
                                              distinct {vm.Id.ToString()} as ReportBatchId, EmployeeId,  CURRENT_TIMESTAMP as ts from DaasEntitlements 
                                              order by CURRENT_TIMESTAMP
                                              offset {memberCount} rows
                                              fetch next {memberCount} rows only
                                              )
                                              insert into ReportBatchMembers (ReportBatchId, EmployeeId)
                                              select ReportBatchId, EmployeeId from cte
                                              """);
                break;
            case ReportBatchTarget.LanId:
                _context.Database.ExecuteSql($"""
                                              ;with cte as (SELECT
                                              distinct {vm.Id.ToString()} as ReportBatchId, UserName,  CURRENT_TIMESTAMP as ts from DaasEntitlements
                                              order by CURRENT_TIMESTAMP
                                              offset {memberCount} rows
                                              fetch next {memberCount} rows only
                                              )
                                              insert into ReportBatchMembers (ReportBatchId, LanId)
                                              select ReportBatchId, UserName from cte
                                              """);
                break;
            default:
                break;
        }

        return RedirectToAction(nameof(Index));
    }
}