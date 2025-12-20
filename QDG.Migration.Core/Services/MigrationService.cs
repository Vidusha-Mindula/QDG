using MySqlConnector;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Services;

public class MigrationService : IMigrationService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ITableService _tableService;
    private readonly IDependencyResolver _dependencyResolver;

    public MigrationService(ISessionRepository sessionRepository, ITableService tableService, IDependencyResolver dependencyResolver)
    {
        _sessionRepository = sessionRepository;
        _tableService = tableService;
        _dependencyResolver = dependencyResolver;
    }

    public async Task<MigrationSession> CreateSessionAsync(int configurationId)
    {
        var session = new MigrationSession
        {
            SessionGuid = Guid.NewGuid(),
            ConfigurationId = configurationId,
            Status = MigrationStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        return await _sessionRepository.CreateAsync(session);
    }

    public async Task<MigrationSession?> GetSessionAsync(Guid sessionGuid)
    {
        var session = await _sessionRepository.GetByGuidAsync(sessionGuid);
        if (session == null) return null;

        session.SelectedTables = await _sessionRepository.GetSelectedTablesAsync(session.Id);
        session.Relationships = await _sessionRepository.GetRelationshipsAsync(session.Id);
        session.FieldMappings = await _sessionRepository.GetFieldMappingsAsync(session.Id);

        return session;
    }

    public async Task UpdateSessionStatusAsync(Guid sessionGuid, MigrationStatus status)
    {
        var session = await _sessionRepository.GetByGuidAsync(sessionGuid);
        if (session == null) return;

        session.Status = status;
        if (status == MigrationStatus.Running && session.StartedAt == null)
            session.StartedAt = DateTime.UtcNow;
        if (status is MigrationStatus.Completed or MigrationStatus.Failed or MigrationStatus.Cancelled)
            session.CompletedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session);
    }

    public async Task<MigrationResult> ExecuteMigrationAsync(Guid sessionGuid, IProgress<MigrationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionGuid);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionGuid} not found");

        var startTime = DateTime.UtcNow;
        var result = new MigrationResult { SessionId = sessionGuid };

        try
        {
            await UpdateSessionStatusAsync(sessionGuid, MigrationStatus.Running);

            var migrationOrder = await _dependencyResolver.ResolveDependencyOrderAsync(
                session.SelectedTables.Select(t => t.TableName).ToList(),
                session.Relationships);

            result.TableResults = new List<TableMigrationResult>();
            var totalTables = migrationOrder.Count;
            var completedTables = 0;

            foreach (var tableName in migrationOrder)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Status = MigrationStatus.Cancelled;
                    break;
                }

                progress?.Report(new MigrationProgress
                {
                    TotalTables = totalTables,
                    CompletedTables = completedTables,
                    CurrentTable = tableName,
                    TotalRowsCopied = result.TotalRowsCopied,
                    ErrorCount = result.TotalErrors
                });

                var tableResult = await MigrateTableAsync(session, tableName);
                result.TableResults.Add(tableResult);

                if (tableResult.Success)
                {
                    completedTables++;
                    result.TablesCompleted++;
                }
                else
                {
                    result.TablesFailed++;
                }

                result.TotalRowsCopied += tableResult.RowsCopied;
                result.TotalRowsSkipped += tableResult.RowsSkipped;
                result.TotalErrors += tableResult.ErrorCount;
            }

            result.Status = result.TablesFailed == 0 ? MigrationStatus.Completed : MigrationStatus.Failed;
            await UpdateSessionStatusAsync(sessionGuid, result.Status);
        }
        catch (Exception ex)
        {
            result.Status = MigrationStatus.Failed;
            await UpdateSessionStatusAsync(sessionGuid, MigrationStatus.Failed);
            await LogErrorAsync(sessionGuid, null, -1, ex.Message);
            throw;
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<TableMigrationResult> MigrateTableAsync(MigrationSession session, string tableName, IProgress<TableProgress>? progress = null)
    {
        var startTime = DateTime.UtcNow;
        var result = new TableMigrationResult { TableName = tableName };

        try
        {
            var selectedTable = session.SelectedTables.First(t => t.TableName == tableName);
            var fieldMappings = session.FieldMappings.Where(f => f.TableName == tableName).ToList();
            var columnConfigs = await _sessionRepository.GetColumnConfigurationsAsync(session.Id);
            var tableColumnConfigs = columnConfigs.Where(c => c.TableName == tableName).ToList();

            using var connection = new MySqlConnection(session.Configuration!.SourceConnectionString);
            await connection.OpenAsync();

            // Helper function to check if column is dropped
            bool IsColumnDropped(string columnName)
            {
                var config = tableColumnConfigs.FirstOrDefault(cfg => cfg.ColumnName == columnName);
                return config?.DropColumn == true;
            }

            // If no field mappings exist, auto-generate 1:1 mapping (excluding dropped columns)
            if (!fieldMappings.Any())
            {
                var tableInfo = await _tableService.GetTableInfoAsync(connection, tableName);

                fieldMappings = tableInfo.Columns
                    .Where(c => !IsColumnDropped(c.ColumnName))
                    .Select(c => new FieldMapping
                    {
                        SessionId = session.Id,
                        TableName = tableName,
                        SourceColumn = c.ColumnName,
                        DestinationColumn = c.ColumnName,
                        Include = true
                    })
                    .ToList();
            }
            else
            {
                // Filter out dropped columns from existing mappings
                fieldMappings = fieldMappings
                    .Where(fm => !IsColumnDropped(fm.SourceColumn))
                    .ToList();
            }

            var destExists = await _tableService.TableExistsAsync(session.Configuration!.DestinationConnectionString, tableName);
            if (!destExists)
            {
                await CreateDestinationTableAsync(session, tableName, fieldMappings, tableColumnConfigs);
            }

            // Only select columns that aren't dropped
            var sourceColumns = fieldMappings
                .Where(f => f.Include && !IsColumnDropped(f.SourceColumn))
                .Select(f => f.SourceColumn)
                .ToList();

            var sourceData = await _tableService.GetTableDataAsync(
                session.Configuration.SourceConnectionString,
                tableName,
                sourceColumns,
                selectedTable.FilterCondition);

            result.SourceRowCount = sourceData.Count;

            var batchSize = 1000;
            for (int i = 0; i < sourceData.Count; i += batchSize)
            {
                var batch = sourceData.Skip(i).Take(batchSize).ToList();

                foreach (var row in batch)
                {
                    try
                    {
                        await MigrateRowAsync(session, tableName, row, fieldMappings, tableColumnConfigs);
                        result.RowsCopied++;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        await LogErrorAsync(session.SessionGuid, tableName, i, ex.Message);
                    }

                    progress?.Report(new TableProgress
                    {
                        TableName = tableName,
                        TotalRows = sourceData.Count,
                        ProcessedRows = i + batch.IndexOf(row) + 1,
                        ErrorCount = result.ErrorCount
                    });
                }
            }

            result.Success = result.ErrorCount == 0;

            await _sessionRepository.SaveStatsAsync(new MigrationStats
            {
                SessionId = session.Id,
                TableName = tableName,
                SourceRowCount = result.SourceRowCount,
                DestinationRowCount = result.RowsCopied,
                RowsCopied = result.RowsCopied,
                RowsSkipped = result.RowsSkipped,
                ErrorCount = result.ErrorCount,
                Duration = (int)(DateTime.UtcNow - startTime).TotalSeconds,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            result.Success = false;
            await LogErrorAsync(session.SessionGuid, tableName, -1, ex.Message);
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<List<MigrationSession>> GetRecentSessionsAsync(int count = 10)
    {
        return await _sessionRepository.GetRecentAsync(count);
    }

    public async Task<List<MigrationSession>> GetSessionsAsync(MigrationStatus? status = null, string sortBy = "created", bool descending = true)
    {
        return await _sessionRepository.GetFilteredAsync(status, sortBy, descending);
    }

    public async Task<Dictionary<MigrationStatus, int>> GetSessionStatisticsAsync()
    {
        return await _sessionRepository.GetStatisticsAsync();
    }

    public async Task<MigrationResult> GetMigrationResultAsync(Guid sessionGuid)
    {
        var session = await GetSessionAsync(sessionGuid);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionGuid} not found");

        var stats = await _sessionRepository.GetStatsAsync(sessionGuid);

        return new MigrationResult
        {
            SessionId = sessionGuid,
            Status = session.Status,
            Duration = session.CompletedAt.HasValue
                ? session.CompletedAt.Value - session.StartedAt!.Value
                : TimeSpan.Zero,
            TablesCompleted = stats.Count(s => s.ErrorCount == 0),
            TablesFailed = stats.Count(s => s.ErrorCount > 0),
            TotalRowsCopied = stats.Sum(s => s.RowsCopied),
            TotalRowsSkipped = stats.Sum(s => s.RowsSkipped),
            TotalErrors = stats.Sum(s => s.ErrorCount),
            TableResults = stats.Select(s => new TableMigrationResult
            {
                TableName = s.TableName,
                SourceRowCount = s.SourceRowCount,
                RowsCopied = s.RowsCopied,
                RowsSkipped = s.RowsSkipped,
                ErrorCount = s.ErrorCount,
                Duration = TimeSpan.FromSeconds(s.Duration),
                Success = s.ErrorCount == 0
            }).ToList(),
            Errors = new List<MigrationError>()
        };
    }

    public async Task DeleteSessionAsync(Guid sessionGuid)
    {
        var session = await _sessionRepository.GetByGuidAsync(sessionGuid);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionGuid} not found");

        if (session.Status != MigrationStatus.Draft)
            throw new InvalidOperationException("Only draft sessions can be deleted");

        await _sessionRepository.DeleteAsync(session.Id);
    }

    private async Task CreateDestinationTableAsync(MigrationSession session, string tableName, List<FieldMapping> fieldMappings, List<ColumnConfiguration> columnConfigs)
    {
        using var connection = new MySqlConnection(session.Configuration!.SourceConnectionString);
        await connection.OpenAsync();

        var sourceTableInfo = await _tableService.GetTableInfoAsync(connection, tableName);
        var destinationTableInfo = MapTableInfoToDestination(sourceTableInfo, fieldMappings, columnConfigs);
        await _tableService.CreateTableAsync(session.Configuration.DestinationConnectionString, destinationTableInfo);
    }

    private TableInfo MapTableInfoToDestination(TableInfo sourceTableInfo, List<FieldMapping> fieldMappings, List<ColumnConfiguration> columnConfigs)
    {
        var destinationTableInfo = new TableInfo
        {
            TableName = sourceTableInfo.TableName,
            RowCount = 0,
            Columns = new List<ColumnInfo>(),
            ForeignKeys = new List<ForeignKeyInfo>(),
            PrimaryKey = null
        };

        foreach (var sourceColumn in sourceTableInfo.Columns)
        {
            // Check if column should be dropped
            var columnConfig = columnConfigs.FirstOrDefault(c =>
                c.TableName == sourceTableInfo.TableName &&
                c.ColumnName == sourceColumn.ColumnName);

            if (columnConfig?.DropColumn == true)
            {
                // Skip this column entirely - it won't exist in destination
                continue;
            }

            var mapping = fieldMappings.FirstOrDefault(m => m.SourceColumn == sourceColumn.ColumnName);
            if (mapping != null && !mapping.Include)
                continue;

            var destinationColumnName = mapping?.DestinationColumn ?? sourceColumn.ColumnName;

            destinationTableInfo.Columns.Add(new ColumnInfo
            {
                ColumnName = destinationColumnName,
                DataType = sourceColumn.DataType,
                IsNullable = sourceColumn.IsNullable,
                IsPrimaryKey = sourceColumn.IsPrimaryKey,
                IsAutoIncrement = sourceColumn.IsAutoIncrement,
                DefaultValue = sourceColumn.DefaultValue
            });
        }

        // Handle foreign keys - exclude dropped columns
        foreach (var sourceFk in sourceTableInfo.ForeignKeys)
        {
            var columnConfig = columnConfigs.FirstOrDefault(c =>
                c.TableName == sourceTableInfo.TableName &&
                c.ColumnName == sourceFk.ColumnName);

            if (columnConfig?.DropColumn == true)
            {
                // Skip foreign keys for dropped columns
                continue;
            }

            var columnMapping = fieldMappings.FirstOrDefault(m => m.SourceColumn == sourceFk.ColumnName);

            if (columnMapping != null && !columnMapping.Include)
                continue;

            destinationTableInfo.ForeignKeys.Add(new ForeignKeyInfo
            {
                ConstraintName = sourceFk.ConstraintName,
                ColumnName = columnMapping?.DestinationColumn ?? sourceFk.ColumnName,
                ReferencedTable = sourceFk.ReferencedTable,
                ReferencedColumn = sourceFk.ReferencedColumn
            });
        }

        // Handle primary keys - exclude dropped columns
        if (sourceTableInfo.PrimaryKey != null && sourceTableInfo.PrimaryKey.Columns.Any())
        {
            var mappedPkColumns = new List<string>();

            foreach (var pkColumn in sourceTableInfo.PrimaryKey.Columns)
            {
                var columnConfig = columnConfigs.FirstOrDefault(c =>
                    c.TableName == sourceTableInfo.TableName &&
                    c.ColumnName == pkColumn);

                if (columnConfig?.DropColumn == true)
                {
                    // Skip primary key columns that are dropped
                    continue;
                }

                var pkMapping = fieldMappings.FirstOrDefault(m => m.SourceColumn == pkColumn);

                if (pkMapping == null || pkMapping.Include)
                {
                    var destinationPkColumn = pkMapping?.DestinationColumn ?? pkColumn;
                    mappedPkColumns.Add(destinationPkColumn);
                }
            }

            if (mappedPkColumns.Any())
            {
                destinationTableInfo.PrimaryKey = new PrimaryKeyInfo
                {
                    ConstraintName = sourceTableInfo.PrimaryKey.ConstraintName,
                    Columns = mappedPkColumns
                };
            }
        }

        return destinationTableInfo;
    }

    private async Task MigrateRowAsync(MigrationSession session, string tableName, Dictionary<string, object> sourceRow, List<FieldMapping> fieldMappings, List<ColumnConfiguration> columnConfigs)
    {
        var destRow = new Dictionary<string, object>();

        foreach (var mapping in fieldMappings.Where(m => m.Include))
        {
            var isDropped = columnConfigs.Any(c =>
                c.TableName == tableName &&
                c.ColumnName == mapping.SourceColumn &&
                c.DropColumn);

            if (isDropped)
                continue;

            var sourceValue = sourceRow.ContainsKey(mapping.SourceColumn) ? sourceRow[mapping.SourceColumn] : null;
            if (sourceValue != null)
                destRow[mapping.DestinationColumn] = sourceValue;
        }

        await ResolveForeignKeysAsync(session, tableName, destRow);
        await InsertRowAsync(session.Configuration!.DestinationConnectionString, tableName, destRow);

        if (sourceRow.ContainsKey("id"))
        {
            var insertedId = await GetLastInsertIdAsync(session.Configuration.DestinationConnectionString);
            await _sessionRepository.SaveIdMappingAsync(new IdMapping
            {
                SessionId = session.Id,
                TableName = tableName,
                SourceId = sourceRow["id"].ToString()!,
                DestinationId = insertedId.ToString()
            });
        }
    }

    private async Task ResolveForeignKeysAsync(MigrationSession session, string tableName, Dictionary<string, object> row)
    {
        var relationships = session.Relationships.Where(r => r.ChildTable.Equals(tableName, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var rel in relationships)
        {
            if (row.ContainsKey(rel.ChildColumn) && row[rel.ChildColumn] != null)
            {
                var sourceId = row[rel.ChildColumn].ToString()!;
                var newId = await _sessionRepository.GetMappedIdAsync(session.SessionGuid, rel.ParentTable, sourceId);
                if (newId != null)
                {
                    row[rel.ChildColumn] = newId;
                }
            }
        }
    }

    private async Task InsertRowAsync(string connectionString, string tableName, Dictionary<string, object> row)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var columns = string.Join(", ", row.Keys.Select(k => $"`{k}`"));
        var values = string.Join(", ", row.Keys.Select(k => $"@{k}"));
        var sql = $"INSERT INTO `{tableName}` ({columns}) VALUES ({values})";

        await using var command = new MySqlCommand(sql, connection);
        foreach (var kvp in row)
        {
            command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
        }

        await command.ExecuteNonQueryAsync();
    }

    private async Task<long> GetLastInsertIdAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private async Task LogErrorAsync(Guid sessionGuid, string? tableName, int rowIndex, string message)
    {
        var session = await _sessionRepository.GetByGuidAsync(sessionGuid);
        if (session == null) return;

        await _sessionRepository.SaveLogAsync(new MigrationLog
        {
            SessionId = session.Id,
            TableName = tableName,
            LogLevel = "Error",
            Message = $"Row {rowIndex}: {message}",
            CreatedAt = DateTime.UtcNow
        });
    }
}