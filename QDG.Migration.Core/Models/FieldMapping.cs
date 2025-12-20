namespace QDG.Migration.Core.Models;

public class FieldMapping
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string SourceColumn { get; set; } = string.Empty;
    public string DestinationColumn { get; set; } = string.Empty;
    public bool Include { get; set; } = true;
}
