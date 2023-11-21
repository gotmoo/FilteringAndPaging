using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EucRepo.Models;

public class DaasEntitlement : DaasEntitlementBase
{
    public int Id { get; set; }
}

public class DaasEntitlementLog : DaasEntitlementBase
{
    [Key] public int LogId { get; set; }
    public int Id { get; set; }
    [Unicode(false)] [StringLength(15)] public string LogAction { get; set; } = null!;
    public DateTime LogTime { get; set; }
}

public class DaasEntitlementBase
{
    public int EmployeeId { get; set; }
    [Unicode(false)] [StringLength(15)]public string? EmployeeStatus { get; set; }
    public bool AdEnabled { get; set; }
    [Unicode(false)] [StringLength(75)]public string? UserName { get; set; }
    [Unicode(false)] [StringLength(120)]public string? AdGroup { get; set; }
    [Unicode(false)] [StringLength(100)]public string? DaasName { get; set; }
    [Unicode(false)] [StringLength(50)]public string? MachineType { get; set; }
    [Unicode(false)] [StringLength(15)]public string? Os { get; set; }
    [Unicode(false)] [StringLength(15)]public string? DcPair { get; set; }
    public DateTime Provisioned { get; set; }
    public int DaysActive { get; set; }
    public DateTime? LastSeen { get; set; }
    public int? PriAssigned { get; set; }
}