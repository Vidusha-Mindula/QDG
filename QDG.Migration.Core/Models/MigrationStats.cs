namespace QDG.Migration.Core.Models;

public class MigrationStats
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public long SourceRowCount { get; set; }
    public long DestinationRowCount { get; set; }
    public long RowsCopied { get; set; }
    public long RowsSkipped { get; set; }
    public int ErrorCount { get; set; }
    public int Duration { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
