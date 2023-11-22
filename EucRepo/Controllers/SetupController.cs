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
                    .RuleFor(e => e.Os, "Win10")
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


            return RedirectToAction(controllerName: "Home", actionName: "Index");
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

            return RedirectToAction(controllerName: "Home", actionName: "Index");
        }
    }
}