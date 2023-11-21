using EucRepo.Models;
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

        modelBuilder.Entity<UtilityCalendarDay>().HasKey(p => new { p.Date });
        modelBuilder.Entity<UtilityCalendarDay>().Property(p => p.Date).HasColumnType("date");

        modelBuilder.Entity<DaasEntitlement>().Property(p => p.Provisioned).HasColumnType("date");
        modelBuilder.Entity<DaasEntitlement>().Property(p => p.LastSeen).HasColumnType("date");
        modelBuilder.Entity<DaasEntitlementLog>().Property(p => p.Provisioned).HasColumnType("date");
      

        //Views
        modelBuilder.Entity<ModelsView.EFMigrationsHistory>(e => e.ToView("__EFMigrationsHistory").HasNoKey());
        //Tables with triggers need to be declared so an older, slower access method can be used. .Net7 breaking change.
        modelBuilder.Entity<DaasEntitlement>().ToTable(tb => tb.HasTrigger($"trg_{tb.Name}_Log"));

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