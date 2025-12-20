namespace QDG_DB_Migrator.Models;

public class FilterConfigViewModel
{
    public Guid SessionId { get; set; }
    public List<TableFilterItem> TableFilters { get; set; } = new();
}

public class TableFilterItem
{
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public string FilterCondition { get; set; } = string.Empty;
    public long? PreviewCount { get; set; }
}
