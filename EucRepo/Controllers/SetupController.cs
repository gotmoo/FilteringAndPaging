using Bogus;
using ChoETL;
using EucRepo.Helpers;
using EucRepo.Models;
using EucRepo.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EucRepo.Controllers
{
    public class SetupController : Controller
    {
        private readonly SqlDbContext _context;

        public SetupController(SqlDbContext context)
        {
            _context = context;
        }

        public ActionResult ResetDatabase()
        {
//Set the randomizer seed if you wish to generate repeatable data sets.
            Randomizer.Seed = new Random(80945);

            var adGroups = new[] { "GroupA", "GroupB", "GroupC", "GroupD", "GroupE" };
            var daasNames = new[] { "Persistent", "Non-persistent" };
            var machineTypes = new[] { "PRI", "NPP" };
            var osVersion = new[] { "Win10", "Win11" };
            var DcPair = new[] { "EastUS", "EMEA", "APAC" };
            var userNamePrefix = new[] { "e", "a", "p" };
            var entitlementIds = 1;
            var testUsers = new Faker<DaasEntitlement>()
                    // .StrictMode(true)
                    // .RuleFor(e => e.Id, f => entitlementIds++)
                    .RuleFor(e => e.EmployeeId, f => f.Random.Int(200001, 400000))
                    .RuleFor(e => e.EmployeeStatus, "Active")
                    .RuleFor(e => e.AdEnabled, true)
                    .RuleFor(e => e.UserName,
                        (f, e) => $"ANYMOO\\{f.PickRandom(userNamePrefix)}{e.EmployeeId.ToString()}")
                    .RuleFor(e => e.AdGroup, f => f.PickRandom(adGroups))
                    .RuleFor(e => e.DaasName, f => f.PickRandom(daasNames))
                    .RuleFor(e => e.MachineType, f => f.PickRandom(machineTypes))
                    .RuleFor(e => e.Os, f => f.PickRandom(osVersion))
                    .RuleFor(e => e.DcPair, f => f.PickRandom(DcPair))
                    .RuleFor(e => e.Provisioned, f => DateTime.UtcNow.AddDays(-f.Random.Int(0, 90)).Date)
                    .RuleFor(e => e.DaysActive, f => f.Random.Int(0, 40))
                    .RuleFor(e => e.LastSeen, f => DateTime.UtcNow.AddDays(-f.Random.Int(0, 90)).Date)
                    .RuleFor(e => e.PriAssigned, f => f.Random.Int(0, 2))
                ;


            var newEntitlements = testUsers.Generate(150000);
// DIRTY HACK, we WILL come back to this to fix this

            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            using (var bulkInsert = new SqlBulkCopy(_context.Database.GetConnectionString()))
            {
                bulkInsert.DestinationTableName =
                    _context.Model.FindEntityType(typeof(DaasEntitlement))!.GetTableName();
                bulkInsert.BulkCopyTimeout = 90;
                bulkInsert.WriteToServer(newEntitlements.AsDataTable());
            }

            //Create a few batches
            CreateBatch(ReportBatchTarget.EmployeeId, 5000, new Guid("213380BD-C7E8-47EB-8895-35CCB43D33C0"), "5000 Employees");
            CreateBatch(ReportBatchTarget.EmployeeId, 50, new Guid("7903193A-7F34-4145-981E-5BA20125A2E2"), "50 Employees");
            CreateBatch(ReportBatchTarget.LanId, 100, new Guid("0DA13CB0-97E1-43F0-A6FD-1F5A650309D7"), "100 Users");
            CreateBatch(ReportBatchTarget.LanId, 50, new Guid("453E24A1-6D99-4F98-854A-6A240E708469"), "50 Users");
            CreateBatch(ReportBatchTarget.EmployeeId, 50, new Guid("9AEEDE2D-5DBD-4A8E-898F-021756B83297"), "50 Employees Read-only");
            CreateBatch(ReportBatchTarget.LanId, 50, new Guid("830EA165-A451-4A99-AB8F-57129593E5CC"), "50 Users View-only");
            CreateBatch(ReportBatchTarget.LanId, 50, new Guid("1DC35B30-AF0F-4BBA-AFA7-EE799217DFD0"), "50 Users No-Access");
            CreateBatch(ReportBatchTarget.EmployeeId, 15000, null, "15000 Employees");
            CreateBatch(ReportBatchTarget.LanId, 15000, null, "15000 Users");

            //No Access
            var modifyBatch1 =
                _context.ReportBatches.Include(r => r.Owners).FirstOrDefault(r => r.Id == new Guid("1DC35B30-AF0F-4BBA-AFA7-EE799217DFD0"));
            modifyBatch1.IsVisibleWithLink = false;
            _context.ReportBatchOwners.Where(b => b.ReportBatch == modifyBatch1).ExecuteDelete();
            //View-only
            var modifyBatch2 =
                _context.ReportBatches.Include(r => r.Owners).FirstOrDefault(r => r.Id == new Guid("830EA165-A451-4A99-AB8F-57129593E5CC"));
            _context.ReportBatchOwners.Where(b => b.ReportBatch == modifyBatch2).ExecuteDelete();
            //Read-only
            var modifyBatch3 =
                _context.ReportBatches.Include(r => r.Owners).FirstOrDefault(r => r.Id == new Guid("9AEEDE2D-5DBD-4A8E-898F-021756B83297"));

            modifyBatch3.Viewers.Add(new ReportBatchViewer
            {
                ReportBatch = modifyBatch3,
                UserName = modifyBatch3.CreatedBy
            });
            _context.ReportBatchOwners.Where(b => b.ReportBatch == modifyBatch3).ExecuteDelete();
            _context.SaveChanges();
            return RedirectToAction(controllerName: "Home", actionName: "Index");
        }

        [Authorize]
        public IActionResult CreateNewBatch(ReportBatchTarget target, Guid? batchId, int memberCount = 1000)
        {
            CreateBatch(target, memberCount, batchId, null);

            return RedirectToAction(controllerName: "Home", actionName: "Index");
        }

        private void CreateBatch(ReportBatchTarget target, int memberCount, Guid? batchId, string? batchName)
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
                Id = batchId ?? Guid.NewGuid(),
                CreatedBy = @User.Identity?.Name ?? string.Empty,
                Owners = owners,
                Name = batchName ?? NewNameGenerator.GenerateRandomName(),
                Description = $"A list of {memberCount} items of type {target}",
                BatchTarget = target,
                IsVisibleWithLink = true,
                Created = DateTime.UtcNow
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
        }
    }
}