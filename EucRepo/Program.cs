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