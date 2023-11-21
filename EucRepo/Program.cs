using System.Data.Common;
using Bogus;
using ChoETL;
using EucRepo.Endpoints.Internal;
using EucRepo.Interfaces;
using EucRepo.Models;
using EucRepo.Persistence;
using EucRepo.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);

var builder = WebApplication.CreateBuilder(args);

// Load the database connection string from the configuration
var connectionString = builder.Configuration.GetConnectionString("default");
Environment.SetEnvironmentVariable("ConnectionString",connectionString);


// Add services to the container.
builder.Services.AddDbContext<SqlDbContext>(options =>
{
    if (connectionString != null) options.UseSqlServer(connectionString);
});
builder.Services.Configure<ApiCallSettings>(builder.Configuration.GetSection(ApiCallSettings.Key));
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IDaasEntitlementRepository, DaasEntitlementRepository>();

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

//Load role definitions from appsettings.json under RepoAccess
var roleConfig = builder.Configuration.GetSection("RepoAccess").Get<EucRepoRoles>();
var configuredRoles = roleConfig!.Roles;
builder.Services.AddAuthorizationCore(options =>
{
    //Dynamically register all authorization roles configured in appsettings.json 
    foreach (var key in configuredRoles.Keys)
    {
        Console.WriteLine($"Adding Authorization policy for {key}");
        options.AddPolicy(key, policy=> policy.RequireRole(configuredRoles[key]));
    }
    
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages();

var app = builder.Build();




//Set the randomizer seed if you wish to generate repeatable data sets.
Randomizer.Seed = new Random(80945);

var adGroups = new[] { "GroupA", "GroupB", "GroupC", "GroupD", "GroupE" };
var daasNames = new[] { "Persistent", "Non-persistent"};
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
    .RuleFor(e => e.UserName, ( f,e) => $"ANYMOO\\{f.PickRandom(userNamePrefix)}{e.EmployeeId.ToString()}")
    .RuleFor(e => e.AdGroup, f => f.PickRandom(adGroups))
    .RuleFor(e => e.DaasName, f => f.PickRandom(daasNames))
    .RuleFor(e => e.MachineType, f => f.PickRandom(machineTypes))
    .RuleFor(e => e.Os, "Win10")
    .RuleFor(e => e.DcPair, f => f.PickRandom(DcPair))
    .RuleFor(e => e.Provisioned, f => DateTime.UtcNow.AddDays(-f.Random.Int(0,90)).Date)
    .RuleFor(e => e.DaysActive, f => f.Random.Int(0,40))
    .RuleFor(e => e.LastSeen, f =>  DateTime.UtcNow.AddDays(-f.Random.Int(0,90)).Date)
    .RuleFor(e => e.PriAssigned, f => f.Random.Int(0,2))
;


var newEntitlements = testUsers.Generate(150000);
// DIRTY HACK, we WILL come back to this to fix this

using (
    var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SqlDbContext>();
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
    using (var bulkInsert = new SqlBulkCopy(connectionString))
    {
        bulkInsert.DestinationTableName = context.Model.FindEntityType(typeof(DaasEntitlement))!.GetTableName();
        bulkInsert.BulkCopyTimeout = 90;
        bulkInsert.WriteToServer(newEntitlements.AsDataTable());
    }
}






// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map Minimal API endpoints
app.UseEndpoints<Program>(); 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();