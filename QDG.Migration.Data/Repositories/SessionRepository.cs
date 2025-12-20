using Microsoft.EntityFrameworkCore;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Data.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _context;

    public SessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MigrationSession> CreateAsync(MigrationSession session)
    {
        _context.MigrationSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<MigrationSession?> GetByGuidAsync(Guid sessionGuid)
    {
        return await _context.MigrationSessions
            .Include(s => s.Configuration)
            .Include(s => s.SelectedTables)
            .Include(s => s.Relationships)
            .Include(s => s.FieldMappings)
            .FirstOrDefaultAsync(s => s.SessionGuid == sessionGuid);
    }

    public async Task UpdateAsync(MigrationSession session)
    {
        _context.MigrationSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task<List<MigrationSession>> GetRecentAsync(int count)
    {
        return await _context.MigrationSessions
            .Include(s => s.Configuration)
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<MigrationSession>> GetFilteredAsync(MigrationStatus? status = null, string sortBy = "created", bool descending = true)
    {
        var query = _context.MigrationSessions
            .Include(s => s.Configuration)
            .Include(s => s.SelectedTables)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var sessions = await query.ToListAsync();

        var sortedSessions = sortBy.ToLower() switch
        {
            "status" => descending
                ? sessions.OrderByDescending(s => s.Status).ThenByDescending(s => s.CreatedAt)
                : sessions.OrderBy(s => s.Status).ThenByDescending(s => s.CreatedAt),

            "completed" => descending
                ? sessions.OrderByDescending(s => s.CompletedAt.HasValue)
                          .ThenByDescending(s => s.CompletedAt)
                          .ThenByDescending(s => s.Status == MigrationStatus.Completed ? 1 : 0)
                          .ThenByDescending(s => s.CreatedAt)
                : sessions.OrderBy(s => s.CompletedAt.HasValue)
                          .ThenBy(s => s.CompletedAt)
                          .ThenBy(s => s.Status == MigrationStatus.Completed ? 1 : 0)
                          .ThenByDescending(s => s.CreatedAt),

            "started" => descending
                ? sessions.OrderByDescending(s => s.StartedAt.HasValue)
                          .ThenByDescending(s => s.StartedAt)
                          .ThenByDescending(s => s.CreatedAt)
                : sessions.OrderBy(s => s.StartedAt.HasValue)
                          .ThenBy(s => s.StartedAt)
                          .ThenByDescending(s => s.CreatedAt),

            _ => descending
                ? sessions.OrderByDescending(s => s.CreatedAt)
                : sessions.OrderBy(s => s.CreatedAt)
        };

        return sortedSessions.ToList();
    }

    public async Task DeleteAsync(int sessionId)
    {
        var session = await _context.MigrationSessions
            .Include(s => s.SelectedTables)
            .Include(s => s.Relationships)
            .Include(s => s.FieldMappings)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session != null)
        {
            _context.SelectedTables.RemoveRange(session.SelectedTables);
            _context.TableRelationships.RemoveRange(session.Relationships);
            _context.FieldMappings.RemoveRange(session.FieldMappings);

            var columnConfigs = await _context.ColumnConfigurations
                .Where(c => c.SessionId == sessionId)
                .ToListAsync();
            _context.ColumnConfigurations.RemoveRange(columnConfigs);

            _context.MigrationSessions.Remove(session);

            await _context.SaveChangesAsync();
        }
    }

    public async Task<Dictionary<MigrationStatus, int>> GetStatisticsAsync()
    {
        var stats = await _context.MigrationSessions
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = new Dictionary<MigrationStatus, int>();
        foreach (MigrationStatus status in Enum.GetValues(typeof(MigrationStatus)))
        {
            result[status] = stats.FirstOrDefault(s => s.Status == status)?.Count ?? 0;
        }

        return result;
    }

    public async Task SaveSelectedTablesAsync(int sessionId, List<SelectedTable> tables)
    {
        foreach (var table in tables)
        {
            table.SessionId = sessionId;
        }
        _context.SelectedTables.AddRange(tables);
        await _context.SaveChangesAsync();
    }

    public async Task SaveRelationshipsAsync(int sessionId, List<TableRelationship> relationships)
    {
        foreach (var rel in relationships)
        {
            rel.SessionId = sessionId;
        }
        _context.TableRelationships.AddRange(relationships);
        await _context.SaveChangesAsync();
    }

    public async Task AddRelationshipAsync(TableRelationship relationship)
    {
        _context.TableRelationships.Add(relationship);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRelationshipAsync(int id)
    {
        var relationship = await _context.TableRelationships.FindAsync(id);
        if (relationship != null)
        {
            _context.TableRelationships.Remove(relationship);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<ColumnConfiguration>> GetColumnConfigurationsAsync(int sessionId)
    {
        return await _context.ColumnConfigurations
            .Where(c => c.SessionId == sessionId)
            .ToListAsync();
    }

    public async Task UpdateColumnConfigurationAsync(ColumnConfiguration configuration)
    {
        var existing = await _context.ColumnConfigurations
            .FirstOrDefaultAsync(c =>
                c.SessionId == configuration.SessionId &&
                c.TableName == configuration.TableName &&
                c.ColumnName == configuration.ColumnName);

        if (existing != null)
        {
            existing.SkipCopy = configuration.SkipCopy;
            existing.DropColumn = configuration.DropColumn;
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ColumnConfigurations.Add(configuration);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveFieldMappingsAsync(int sessionId, List<FieldMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            mapping.SessionId = sessionId;
        }
        _context.FieldMappings.AddRange(mappings);
        await _context.SaveChangesAsync();
    }

    public async Task SaveOrUpdateFieldMappingAsync(FieldMapping mapping)
    {
        var existing = await _context.FieldMappings
            .FirstOrDefaultAsync(f =>
                f.SessionId == mapping.SessionId &&
                f.TableName == mapping.TableName &&
                f.SourceColumn == mapping.SourceColumn);

        if (existing != null)
        {
            existing.DestinationColumn = mapping.DestinationColumn;
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.FieldMappings.Add(mapping);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveColumnConfigurationsAsync(int sessionId, List<ColumnConfiguration> configurations)
    {
        foreach (var config in configurations)
        {
            config.SessionId = sessionId;
        }
        _context.ColumnConfigurations.AddRange(configurations);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SelectedTable>> GetSelectedTablesAsync(int sessionId)
    {
        return await _context.SelectedTables
            .Where(t => t.SessionId == sessionId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task<List<TableRelationship>> GetRelationshipsAsync(int sessionId)
    {
        return await _context.TableRelationships
            .Where(r => r.SessionId == sessionId)
            .ToListAsync();
    }

    public async Task<List<FieldMapping>> GetFieldMappingsAsync(int sessionId)
    {
        return await _context.FieldMappings
            .Where(f => f.SessionId == sessionId)
            .ToListAsync();
    }

    public async Task SaveIdMappingAsync(IdMapping mapping)
    {
        _context.IdMappings.Add(mapping);
        await _context.SaveChangesAsync();
    }

    public async Task<string?> GetMappedIdAsync(Guid sessionGuid, string tableName, string sourceId)
    {
        var session = await _context.MigrationSessions
            .FirstOrDefaultAsync(s => s.SessionGuid == sessionGuid);

        if (session == null)
            return null;

        var mapping = await _context.IdMappings
            .FirstOrDefaultAsync(m =>
                m.SessionId == session.Id &&
                m.TableName == tableName &&
                m.SourceId == sourceId);

        return mapping?.DestinationId;
    }

    public async Task SaveLogAsync(MigrationLog log)
    {
        _context.MigrationLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task SaveStatsAsync(MigrationStats stats)
    {
        _context.MigrationStats.Add(stats);
        await _context.SaveChangesAsync();
    }

    public async Task<List<MigrationStats>> GetStatsAsync(Guid sessionGuid)
    {
        var session = await _context.MigrationSessions
            .FirstOrDefaultAsync(s => s.SessionGuid == sessionGuid);

        if (session == null)
            return new List<MigrationStats>();

        return await _context.MigrationStats
            .Where(s => s.SessionId == session.Id)
            .ToListAsync();
    }

    public async Task AddSelectedTableAsync(SelectedTable table)
    {
        _context.SelectedTables.Add(table);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTableFilterAsync(int sessionId, string tableName, string filter)
    {
        var table = await _context.SelectedTables
            .FirstOrDefaultAsync(t => t.SessionId == sessionId && t.TableName == tableName);

        if (table != null)
        {
            table.FilterCondition = filter;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<MigrationStats>> GetMigrationStatsAsync(int sessionId)
    {
        return await _context.MigrationStats
            .Where(s => s.SessionId == sessionId)
            .ToListAsync();
    }

    public async Task<List<MigrationLog>> GetRecentLogsAsync(int sessionId, int count)
    {
        return await _context.MigrationLogs
            .Where(l => l.SessionId == sessionId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}