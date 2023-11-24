using EucRepo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EucRepo.Persistence.EntityMapping;

public class DaasEntitlementMapping :IEntityTypeConfiguration<DaasEntitlement>
{
    public void Configure(EntityTypeBuilder<DaasEntitlement> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.AdEnabled).HasDefaultValue(false);
        builder.Property(p => p.Provisioned).HasColumnType("date");
        builder.Property(p => p.DaysActive).HasDefaultValue(0);
        builder.Property(p => p.LastSeen).HasColumnType("date");
        
        //Tables with triggers need to be declared so an older, slower access method can be used. .Net7 breaking change.
        //builder.ToTable(tb => tb.HasTrigger($"trg_{tb.Name}_Log"));
        builder.HasIndex(p => p.UserName)
            .IsClustered(false);
        builder.HasIndex(p => new{p.UserName, p.Os})
            .IsClustered(false);

    }
}

public class DaasEntitlementLogMapping :IEntityTypeConfiguration<DaasEntitlementLog>
{
    public void Configure(EntityTypeBuilder<DaasEntitlementLog> builder)
    {
        builder.HasKey(p => p.LogId);
        
        // builder.Property(p => p.LogAction).IsUnicode(false);
        // builder.Property(p => p.EmployeeStatus).IsUnicode(false);
        builder.Property(p => p.AdEnabled).HasDefaultValue(false);
        builder.Property(p => p.UserName).IsUnicode(false);
        // builder.Property(p => p.AdGroup).IsUnicode(false);
        // builder.Property(p => p.DaasName).IsUnicode(false);
        // builder.Property(p => p.MachineType).IsUnicode(false);
        // builder.Property(p => p.Os).IsUnicode(false);
        // builder.Property(p => p.DcPair).IsUnicode(false);
        builder.Property(p => p.Provisioned).HasColumnType("date");
        builder.Property(p => p.DaysActive).HasDefaultValue(0);
        builder.Property(p => p.LastSeen).HasColumnType("date");

    }
}