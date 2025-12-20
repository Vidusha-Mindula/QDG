namespace QDG.Migration.Core.Models;

public class MigrationProgress
{
    public int TotalTables { get; set; }
    public int CompletedTables { get; set; }
    public string CurrentTable { get; set; } = string.Empty;
    public long TotalRowsCopied { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan Elapsed { get; set; }
}

public class TableProgress
{
    public string TableName { get; set; } = string.Empty;
    public long TotalRows { get; set; }
    public long ProcessedRows { get; set; }
    public int ErrorCount { get; set; }
}
