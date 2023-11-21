namespace EucRepo.ModelsFilter;

public class SortByModel
{
    public SortByModel(string order, string column)
    {
        Order = order;
        Column = column;
    }

    public string Column { get; set; }
    public string Order { get; set; }
}