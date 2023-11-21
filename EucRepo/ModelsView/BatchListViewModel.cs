using EucRepo.Models;

namespace EucRepo.ModelsView;

public class BatchListViewModel
{
    public List<ReportBatch>? ReportBatches { get; set; }   
    public List<ReportBatch>? OwnerBatches { get; set; }   

}