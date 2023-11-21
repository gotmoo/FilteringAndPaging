namespace EucRepo.ModelsExport;

public class DaasEntitlementExportModel
{
    public int EmployeeId { get; set; }
    public string? EmployeeStatus { get; set; }
    public bool AdEnabled { get; set; }
    public string? UserName { get; set; }
    public string? AdGroup { get; set; }
    public string? DaasName { get; set; }
    public string? MachineType { get; set; }
    public string? Os { get; set; }
    public string? DcPair { get; set; }
    public string? Provisioned { get; set; }
    public int DaysActive { get; set; }
    public string? LastSeen { get; set; }
    public int PriAssigned { get; set; }
}