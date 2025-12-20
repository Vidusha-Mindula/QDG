using System.ComponentModel.DataAnnotations.Schema;

namespace QDG.Migration.Core.Models;

public class ColumnConfiguration
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public bool SkipCopy { get; set; }
    public bool DropColumn { get; set; }
}
