namespace QDG.Migration.Core.Models;

public class MigrationLog
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string? TableName { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
