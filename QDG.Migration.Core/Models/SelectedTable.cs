namespace QDG.Migration.Core.Models;

public class SelectedTable
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public string? FilterCondition { get; set; }
    public int SortOrder { get; set; }
}
