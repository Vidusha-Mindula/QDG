namespace QDG.Migration.Core.Models;

public class IdMapping
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string DestinationId { get; set; } = string.Empty;
}
