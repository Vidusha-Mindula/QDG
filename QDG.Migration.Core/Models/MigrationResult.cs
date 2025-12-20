namespace QDG.Migration.Core.Models;

public class MigrationResult
{
    public Guid SessionId { get; set; }
    public MigrationStatus Status { get; set; }
    public TimeSpan Duration { get; set; }
    public int TablesCompleted { get; set; }
    public int TablesFailed { get; set; }
    public long TotalRowsCopied { get; set; }
    public long TotalRowsSkipped { get; set; }
    public int TotalErrors { get; set; }
    public List<TableMigrationResult> TableResults { get; set; } = new();
    public List<MigrationError> Errors { get; set; } = new();
}

public class TableMigrationResult
{
    public string TableName { get; set; } = string.Empty;
    public long SourceRowCount { get; set; }
    public long RowsCopied { get; set; }
    public long RowsSkipped { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}

public class MigrationError
{
    public string TableName { get; set; } = string.Empty;
    public int RowIndex { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
