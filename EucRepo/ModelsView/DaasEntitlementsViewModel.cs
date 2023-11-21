using EucRepo.Models;
using EucRepo.ModelsFilter;

namespace EucRepo.ModelsView;

public class DaasEntitlementsViewModel
{
    public List<DaasEntitlement>? DaasEntitlements { get; set; }
    public DaasEntitlementsFilterModel FilterModel { get; set; } = new();
    public int TotalRecords { get; set; }
    public int FilteredRecords { get; set; }
    public int StartRecord { get; set; }
    public int EndRecord { get; set; }
    public bool FirstPage { get; set; } = false;
    public bool LastPage { get; set; } = false;
    public int TotalPages { get; set; }
    public List<ReportBatch> Batches { get; set; } = new();
    public List<string> BatchMissingEntries { get; set; } = new();
    public ReportBatch? ThisBatch { get; set; }
    public Dictionary<string, string?> SearchParams { get; set; } = new();
    public Dictionary<string, string[]> SearchOptions { get; set; } = new();
    public DateTime DataRefreshTime { get; set; }
}