namespace QDG.Migration.Core.Models;

public class TableRelationship
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string ParentTable { get; set; } = string.Empty;
    public string ParentColumn { get; set; } = string.Empty;
    public string ChildTable { get; set; } = string.Empty;
    public string ChildColumn { get; set; } = string.Empty;
    public bool IsCustom { get; set; }
}
