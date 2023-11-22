using EucRepo.Helpers;
using EucRepo.Models;
using EucRepo.ModelsExport;

namespace EucRepo.Persistence.Repositories;

public class DaasEntitlementsDto
{
    public int TotalRecords { get; set; }
    public int FilteredRecords { get; set; }
    public ReportBatch? ThisBatch { get; set; }
    public string? ThisBatchAccess { get; set; } = null;
    public List<string> ThisBatchMissingEntries { get; set; } = new();

    public List<ReportBatch> ReportBatches { get; set; } = new();
    public Dictionary<string, string[]> SearchOptions { get; set; } = new();
    public PaginatedList<DaasEntitlement>? PaginatedList { get; set; }
    public List<DaasEntitlementExportModel> ExportDaasEntitlements { get; set; } = new();

}