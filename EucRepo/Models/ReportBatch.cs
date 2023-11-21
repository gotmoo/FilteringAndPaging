using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EucRepo.Models;

public class ReportBatch
{
    public Guid Id { get; set; }
    [StringLength(60)] public string Name { get; set; } = null!;
    [StringLength(400)] public string Description { get; set; } = null!;
    public bool IsManaged { get; set; }
    public bool IsVisibleWithLink { get; set; }
    public ReportBatchTarget BatchTarget { get; set; }
    public DateTime Created { get; set; }
    [StringLength(20)] public string CreatedBy { get; set; } = null!;
    public DateTime? LastRequested { get; set; }
    [StringLength(20)] public string? LastRequestedBy { get; set; }
    public List<ReportBatchOwner> Owners { get; set; } = new();
    public List<ReportBatchViewer> Viewers { get; set; } = new();
    public List<ReportBatchMember> Members { get; set; } = new();
    public List<ReportBatchRequest> Requests { get; set; } = new();

    //Compound value for display
    public string DisplayName => $"{Name} ({BatchTarget})";

    public bool CanView(string? userName = "") =>
        Owners.Select(h => h.UserName).Contains(userName, StringComparer.InvariantCultureIgnoreCase) ||
        Viewers.Select(h => h.UserName).Contains(userName, StringComparer.InvariantCultureIgnoreCase);

    public bool CanEdit(string? userName = "") =>
        Owners.Select(h => h.UserName).Contains(userName, StringComparer.InvariantCultureIgnoreCase);
}

public class ReportBatchOwner
{
    public int Id { get; set; }
    public ReportBatch ReportBatch { get; set; } = new();
    [StringLength(20)] public string UserName { get; set; } = null!;
}

public class ReportBatchViewer
{
    public int Id { get; set; }
    public ReportBatch ReportBatch { get; set; } = new();
    [StringLength(20)] public string UserName { get; set; } = null!;
}

public class ReportBatchMember
{
    public int Id { get; set; }
    public ReportBatch ReportBatch { get; set; } = null!;
    public int? EmployeeId { get; set; }
    [StringLength(20)] public string? LanId { get; set; }
    public int? ApplicationId { get; set; }
}

public class ReportBatchRequest
{
    public int Id { get; set; }
    public ReportBatch ReportBatch { get; set; } = null!;
    [StringLength(20)] public string RequestedBy { get; set; } = null!;
    public DateTime Requested { get; set; }
    [StringLength(300)] public string? Page { get; set; }
}

public class ReportBatchForm
{
    public Guid Id { get; set; }
    public bool NewBatch { get; set; } = false;
    [StringLength(60)] public string? Name { get; set; }
    [StringLength(300)] public string? Description { get; set; } = string.Empty;
    public bool IsManaged { get; set; }
    public bool IsVisibleWithLink { get; set; }
    public DateTime Created { get; set; }
    public ReportBatchTarget BatchTarget { get; set; }
    [StringLength(20)] public string CreatedBy { get; set; } = string.Empty;


    [StringLength(Int32.MaxValue)] public string? Owners { get; set; }
    public string? Viewers { get; set; }


    [StringLength(Int32.MaxValue)] public string? Members { get; set; }

    public string? Message { get; set; }
}

public enum ReportBatchTarget
{
    [Description("Employee ID")] EmployeeId = 1,
    [Description("Lan ID (No Domain)")] LanId = 2
}