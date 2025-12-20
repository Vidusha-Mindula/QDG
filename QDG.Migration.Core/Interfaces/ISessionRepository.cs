using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Interfaces;

public interface ISessionRepository
{
    Task<MigrationSession> CreateAsync(MigrationSession session);
    Task<MigrationSession?> GetByGuidAsync(Guid sessionGuid);
    Task UpdateAsync(MigrationSession session);
    Task<List<MigrationSession>> GetRecentAsync(int count);
    Task<List<MigrationSession>> GetFilteredAsync(MigrationStatus? status = null, string sortBy = "created", bool descending = true);
    Task<Dictionary<MigrationStatus, int>> GetStatisticsAsync();
    Task SaveSelectedTablesAsync(int sessionId, List<SelectedTable> tables);
    Task AddSelectedTableAsync(SelectedTable table);
    Task UpdateTableFilterAsync(int sessionId, string tableName, string filter);
    Task SaveRelationshipsAsync(int sessionId, List<TableRelationship> relationships);
    Task AddRelationshipAsync(TableRelationship relationship);
    Task DeleteRelationshipAsync(int id);
    Task SaveFieldMappingsAsync(int sessionId, List<FieldMapping> mappings);
    Task SaveColumnConfigurationsAsync(int sessionId, List<ColumnConfiguration> configurations);
    Task<List<SelectedTable>> GetSelectedTablesAsync(int sessionId);
    Task<List<TableRelationship>> GetRelationshipsAsync(int sessionId);
    Task<List<FieldMapping>> GetFieldMappingsAsync(int sessionId);
    Task<List<ColumnConfiguration>> GetColumnConfigurationsAsync(int sessionId);
    Task UpdateColumnConfigurationAsync(ColumnConfiguration configuration);
    Task SaveIdMappingAsync(IdMapping mapping);
    Task SaveOrUpdateFieldMappingAsync(FieldMapping mapping);
    Task<string?> GetMappedIdAsync(Guid sessionGuid, string tableName, string sourceId);
    Task SaveLogAsync(MigrationLog log);
    Task SaveStatsAsync(MigrationStats stats);
    Task<List<MigrationStats>> GetStatsAsync(Guid sessionGuid);
    Task<List<MigrationStats>> GetMigrationStatsAsync(int sessionId);
    Task<List<MigrationLog>> GetRecentLogsAsync(int sessionId, int count);
    Task DeleteAsync(int sessionId);
}
