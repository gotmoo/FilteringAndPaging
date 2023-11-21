using System.ComponentModel.DataAnnotations;

namespace EucRepo.Models;

public class DaasEntitlement : DaasEntitlementBase
{
}

public class DaasEntitlementLog : DaasEntitlementBase
{
    public int LogId { get; set; }
    [StringLength(15)] public string LogAction { get; set; } = null!;
    public DateTime LogTime { get; set; }
}

public class DaasEntitlementBase
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    [StringLength(15)] public string? EmployeeStatus { get; set; }
    public bool AdEnabled { get; set; }
    [StringLength(75)] public string? UserName { get; set; }
    [StringLength(120)] public string? AdGroup { get; set; }
    [StringLength(100)] public string? DaasName { get; set; }
    [StringLength(50)] public string? MachineType { get; set; }
    [StringLength(15)] public string? Os { get; set; }
    [StringLength(15)] public string? DcPair { get; set; }
    public DateTime Provisioned { get; set; }
    public int DaysActive { get; set; }
    public DateTime? LastSeen { get; set; }
    public int? PriAssigned { get; set; }
}