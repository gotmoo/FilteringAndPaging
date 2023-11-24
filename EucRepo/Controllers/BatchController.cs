using EucRepo.Helpers;
using EucRepo.Interfaces;
using EucRepo.Models;
using EucRepo.ModelsView;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EucRepo.Controllers;

[Authorize]
public class BatchController : Controller
{
    private readonly IBatchRepository _batchRepository;

    public BatchController( IBatchRepository batchRepository)
    {
        _batchRepository = batchRepository;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userName = User.Identity?.Name;

        var viewModel = new BatchListViewModel()
        {
            ReportBatches = await _batchRepository.GetAllBatchesForCurrentUserAsync(userName)
        };
        return View(viewModel);
    }

    [Authorize]
    public async Task<IActionResult> View(Guid id, string view = "full")
    {
        var userName = User.Identity?.Name;
        var batch = await _batchRepository.GetBatchByIdAsync(id);
        if (batch is null)
            return NotFound($"Batch with id {id} not found.");
        if (!batch.CanView(userName))
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
    public async Task<IActionResult> Manage([FromRoute] Guid? id)
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


        var batch = await _batchRepository.GetBatchByIdAsync(id.Value);
        if (batch is null)
            return RedirectToAction("Index");

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

        return View(batchVm);
    }

    [HttpPost]
    [Route("/[controller]/Manage/{id:guid}")]
    public async Task<IActionResult> Manage([FromForm] ReportBatchForm reportBatch, [FromRoute] Guid id)
    {
        if (!ModelState.IsValid)
        {
            var query = from state in ModelState.Values
                from error in state.Errors
                select error.ErrorMessage;
            var errorMessages = query.ToList();
            return BadRequest($"Model not valid:\n{string.Join("\n", errorMessages)}");
        }

        var result = await _batchRepository.UpdateBatchByIdFromReportBatchFormAsync(id, reportBatch);

        return View(result);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var thisUser = User.Identity?.Name ?? "";
        var batch = await _batchRepository.GetBatchByIdAsync(id);
        if (batch is null)
            return RedirectToAction("Index");
        if (!batch.CanEdit(thisUser))
            return RedirectToAction("Index");

        return View(batch);
    }

    [Authorize]
    [HttpPost]
    [Route("[Controller]/Delete/{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var thisUser = User.Identity?.Name ?? "";
        await _batchRepository.DeleteBatchByIdAsync(id, thisUser);
        
        return RedirectToAction("Index");
    }
}