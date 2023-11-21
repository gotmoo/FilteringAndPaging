namespace EucRepo.ModelsFilter;

public class DaasEntitlementsFilterModel
{
    public int PageSize { get; set; }
    public int? Page { get; set; }
    public string? Order
    {
        get => SortByModel.Order;
        set => SortByModel.Order = value??"asc";
    }
    public string? OrderBy
    {
        get => SortByModel.Column;
        set => SortByModel.Column = value??string.Empty;
    }

    public string? UserName { get; set; }
    public string? DcPair { get; set; }
    public string? DaasName { get; set; }
    public string? Os { get; set; }
    public string? MachineType { get; set; }
    public string? AdGroup { get; set; }
    public string? DaysActive { get; set; }
    public string? LastSeen { get; set; }
    public string? Provisioned { get; set; }
    public Guid? Batch { get; set; }
    public SortByModel SortByModel { get; set; }
    public DaasEntitlementsFilterModel()
    {
        Page = 1;
        PageSize = 50;
        SortByModel = new SortByModel(order: "asc", column: string.Empty);
    }
}
