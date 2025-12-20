namespace QDG_DB_Migrator.Models;

public class MigrationProgressViewModel
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalTables { get; set; }
    public int CompletedTables { get; set; }
    public string CurrentTable { get; set; } = string.Empty;
    public long TotalRowsCopied { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan Elapsed { get; set; }
    public bool IsComplete { get; set; }
}
