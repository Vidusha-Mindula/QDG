namespace QDG.Migration.Core.Models;

public class MigrationSession
{
    public int Id { get; set; }
    public Guid SessionGuid { get; set; }
    public int ConfigurationId { get; set; }
    public string? Name { get; set; }
    public MigrationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DatabaseConfig? Configuration { get; set; }
    public List<SelectedTable> SelectedTables { get; set; } = new();
    public List<TableRelationship> Relationships { get; set; } = new();
    public List<FieldMapping> FieldMappings { get; set; } = new();
}

public enum MigrationStatus
{
    Draft,
    Ready,
    Running,
    Completed,
    Failed,
    Cancelled
}
