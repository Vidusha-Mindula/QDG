using QDG.Migration.Core.Models;

namespace QDG_DB_Migrator.Models;

public class RelationshipConfigViewModel
{
    public Guid SessionId { get; set; }
    public List<TableRelationship> DetectedRelationships { get; set; } = new();
    public List<TableRelationship> CustomRelationships { get; set; } = new();
    public List<string> AvailableTables { get; set; } = new();
    public Dictionary<string, List<ColumnInfo>> TableColumns { get; set; } = new();
    public List<ColumnConfiguration> ColumnConfigurations { get; set; } = new();
    public string DependencyVisualization { get; set; } = string.Empty;
    public bool HasCircularDependency { get; set; }
    public List<string> MigrationOrder { get; set; } = new();
}
