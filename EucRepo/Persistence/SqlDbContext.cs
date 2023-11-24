using EucRepo.Models;
using EucRepo.Persistence.EntityMapping;
using Microsoft.EntityFrameworkCore;

namespace EucRepo.Persistence;

public class SqlDbContext : DbContext
{

    public DbSet<DaasEntitlement> DaasEntitlements => Set<DaasEntitlement>();
    public DbSet<DaasEntitlementLog> LogDaasEntitlements => Set<DaasEntitlementLog>();
   
    public DbSet<UtilityCalendarDay> UtilityCalendar => Set<UtilityCalendarDay>();
    
    public DbSet<ReportBatch> ReportBatches => Set<ReportBatch>();
    public DbSet<ReportBatchOwner> ReportBatchOwners => Set<ReportBatchOwner>();
    public DbSet<ReportBatchMember> ReportBatchMembers => Set<ReportBatchMember>();
    public DbSet<ReportBatchViewer> ReportBatchViewers => Set<ReportBatchViewer>();
    public DbSet<ReportBatchRequest> ReportBatchRequests => Set<ReportBatchRequest>();
    

    //Views
    public DbSet<ModelsView.EFMigrationsHistory> __EFMigrationsHistory => Set<ModelsView.EFMigrationsHistory>();
    public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Set the default text type to VarChar, away from NVarChar
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetProperties()
                         .Where(p => p.ClrType == typeof(string))))
        {
            property.SetIsUnicode(false);
        }

        // Apply specific entity mapping configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SqlDbContext).Assembly);
        
        modelBuilder.Entity<UtilityCalendarDay>().HasKey(p => new { p.Date });
        modelBuilder.Entity<UtilityCalendarDay>().Property(p => p.Date).HasColumnType("date");
     
      

        //Views
        modelBuilder.Entity<ModelsView.EFMigrationsHistory>(e => e.ToView("__EFMigrationsHistory").HasNoKey());

    }

    private static byte[] StringToByteArray(string hex)
    {
        int numberChars = hex.Length;
        byte[] bytes = new byte[numberChars / 2];
        for (int i = 0; i < numberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}