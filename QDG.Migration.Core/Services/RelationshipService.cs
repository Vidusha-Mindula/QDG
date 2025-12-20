using Dapper;
using MySqlConnector;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Services;

public class RelationshipService : IRelationshipService
{
    private readonly IDependencyResolver _dependencyResolver;

    public RelationshipService(IDependencyResolver dependencyResolver)
    {
        _dependencyResolver = dependencyResolver;
    }

    public async Task<List<TableRelationship>> GetDatabaseRelationshipsAsync(string connectionString, List<string> tableNames)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var relationships = new List<TableRelationship>();

        foreach (var tableName in tableNames)
        {
            var fks = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    CONSTRAINT_NAME,
                    COLUMN_NAME,
                    REFERENCED_TABLE_NAME,
                    REFERENCED_COLUMN_NAME
                FROM information_schema.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = @TableName 
                    AND REFERENCED_TABLE_NAME IS NOT NULL",
                new { TableName = tableName });

            foreach (var fk in fks)
            {
                relationships.Add(new TableRelationship
                {
                    ParentTable = fk.REFERENCED_TABLE_NAME,
                    ParentColumn = fk.REFERENCED_COLUMN_NAME,
                    ChildTable = tableName,
                    ChildColumn = fk.COLUMN_NAME,
                    IsCustom = false
                });
            }
        }

        return relationships;
    }

    public async Task<List<TableRelationship>> GetAllRelationshipsAsync(int sessionId)
    {
        using var connection = new MySqlConnection(await GetConnectionStringForSessionAsync(sessionId));
        await connection.OpenAsync();

        var relationships = await connection.QueryAsync<TableRelationship>(
            "SELECT * FROM TableRelationships WHERE SessionId = @SessionId",
            new { SessionId = sessionId });

        return relationships.ToList();
    }

    public async Task SaveRelationshipAsync(TableRelationship relationship)
    {
        using var connection = new MySqlConnection(await GetConnectionStringForSessionAsync(relationship.SessionId));
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            @"INSERT INTO TableRelationships (SessionId, ParentTable, ParentColumn, ChildTable, ChildColumn, IsCustom)
              VALUES (@SessionId, @ParentTable, @ParentColumn, @ChildTable, @ChildColumn, @IsCustom)",
            relationship);
    }

    public async Task DeleteRelationshipAsync(int id)
    {
        using var connection = new MySqlConnection("Data Source=app.db");
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            "DELETE FROM TableRelationships WHERE Id = @Id",
            new { Id = id });
    }

    private async Task<string> GetConnectionStringForSessionAsync(int sessionId)
    {
        return "Data Source=app.db";
    }

    public async Task<DependencyGraph> BuildDependencyGraphAsync(List<string> tables, List<TableRelationship> relationships)
    {
        var dependencies = new Dictionary<string, List<string>>();
        
        foreach (var table in tables)
        {
            var tableDeps = relationships
                .Where(r => r.ChildTable.Equals(table, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.ParentTable)
                .Distinct()
                .ToList();
            
            dependencies[table] = tableDeps;
        }

        var independentTables = tables
            .Where(t => !dependencies[t].Any())
            .ToList();

        var hasCircular = await HasCircularDependencyAsync(tables, relationships);

        return new DependencyGraph
        {
            Dependencies = dependencies,
            IndependentTables = independentTables,
            HasCircularDependency = hasCircular
        };
    }

    public async Task<List<string>> GetMigrationOrderAsync(DependencyGraph graph)
    {
        if (graph.HasCircularDependency)
            throw new InvalidOperationException("Cannot determine migration order with circular dependencies");

        return await _dependencyResolver.ResolveDependencyOrderAsync(
            graph.Dependencies.Keys.ToList(),
            graph.Dependencies.SelectMany(kvp => 
                kvp.Value.Select(parent => new TableRelationship
                {
                    ChildTable = kvp.Key,
                    ParentTable = parent
                })).ToList());
    }

    public async Task<bool> HasCircularDependencyAsync(List<string> tables, List<TableRelationship> relationships)
    {
        return !await _dependencyResolver.ValidateDependenciesAsync(tables, relationships);
    }
}
