using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Interfaces;

public interface IRelationshipService
{
    Task<List<TableRelationship>> GetDatabaseRelationshipsAsync(string connectionString, List<string> tableNames);
    Task<List<TableRelationship>> GetAllRelationshipsAsync(int sessionId);
    Task SaveRelationshipAsync(TableRelationship relationship);
    Task DeleteRelationshipAsync(int id);
    Task<DependencyGraph> BuildDependencyGraphAsync(List<string> tables, List<TableRelationship> relationships);
    Task<List<string>> GetMigrationOrderAsync(DependencyGraph graph);
    Task<bool> HasCircularDependencyAsync(List<string> tables, List<TableRelationship> relationships);
}
