using System.Data;
using Dapper;
using MySqlConnector;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Services;

public class DatabaseAnalyzerService : IDatabaseAnalyzerService
{
    private readonly IConfigRepository _configRepository;

    public DatabaseAnalyzerService(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    public async Task<List<string>> GetTableNamesAsync(int dbConfigId)
    {
        var config = await _configRepository.GetByIdAsync(dbConfigId);
        if (config == null)
            throw new InvalidOperationException($"Database configuration {dbConfigId} not found");

        using var connection = new MySqlConnection(config.SourceConnectionString);
        
        var tableNames = await connection.QueryAsync<string>(@"
            SELECT TABLE_NAME 
            FROM information_schema.TABLES 
            WHERE TABLE_SCHEMA = DATABASE() 
            AND TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME");

        return tableNames.ToList();
    }

    public async Task<List<TableInfo>> GetTablesAsync(string connectionString)
    {
        using var connection = new MySqlConnection(connectionString);
        
        var tableNames = await connection.QueryAsync<string>(@"
            SELECT TABLE_NAME 
            FROM information_schema.TABLES 
            WHERE TABLE_SCHEMA = DATABASE() 
            AND TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME");

        var tables = new List<TableInfo>();
        foreach (var tableName in tableNames)
        {
            var table = await GetTableInfoAsync(connectionString, tableName);
            tables.Add(table);
        }

        return tables;
    }

    public async Task<TableInfo> GetTableInfoAsync(string connectionString, string tableName)
    {
        using var connection = new MySqlConnection(connectionString);
        
        var columns = await connection.QueryAsync<dynamic>(@"
            SELECT 
                COLUMN_NAME as ColumnName,
                DATA_TYPE as DataType,
                IS_NULLABLE as IsNullable,
                COLUMN_DEFAULT as DefaultValue,
                EXTRA as Extra
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION", new { TableName = tableName });

        var columnList = columns.Select(c => new ColumnInfo
        {
            ColumnName = c.ColumnName,
            DataType = c.DataType,
            IsNullable = c.IsNullable == "YES",
            IsAutoIncrement = c.Extra?.Contains("auto_increment") == true,
            DefaultValue = c.DefaultValue
        }).ToList();

        var foreignKeys = await connection.QueryAsync<dynamic>(@"
            SELECT 
                CONSTRAINT_NAME as ConstraintName,
                COLUMN_NAME as ColumnName,
                REFERENCED_TABLE_NAME as ReferencedTable,
                REFERENCED_COLUMN_NAME as ReferencedColumn
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @TableName
            AND REFERENCED_TABLE_NAME IS NOT NULL", new { TableName = tableName });

        var fkList = foreignKeys.Select(fk => new ForeignKeyInfo
        {
            ConstraintName = fk.ConstraintName,
            ColumnName = fk.ColumnName,
            ReferencedTable = fk.ReferencedTable,
            ReferencedColumn = fk.ReferencedColumn
        }).ToList();

        var primaryKey = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT CONSTRAINT_NAME as ConstraintName
            FROM information_schema.TABLE_CONSTRAINTS
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @TableName
            AND CONSTRAINT_TYPE = 'PRIMARY KEY'", new { TableName = tableName });

        var pkColumns = new List<string>();
        if (primaryKey != null)
        {
            var pkCols = await connection.QueryAsync<string>(@"
                SELECT COLUMN_NAME
                FROM information_schema.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @TableName
                AND CONSTRAINT_NAME = @ConstraintName
                ORDER BY ORDINAL_POSITION", 
                new { TableName = tableName, ConstraintName = (string)primaryKey.ConstraintName });
            
            pkColumns = pkCols.ToList();
        }

        var rowCount = await connection.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM `{tableName}`");

        return new TableInfo
        {
            TableName = tableName,
            RowCount = rowCount,
            Columns = columnList,
            ForeignKeys = fkList,
            PrimaryKey = primaryKey != null ? new PrimaryKeyInfo
            {
                ConstraintName = primaryKey.ConstraintName,
                Columns = pkColumns
            } : new PrimaryKeyInfo { Columns = new List<string>() }
        };
    }

    public async Task<long> GetRowCountAsync(string connectionString, string tableName, string? filter = null)
    {
        using var connection = new MySqlConnection(connectionString);
        
        var sql = $"SELECT COUNT(*) FROM `{tableName}`";
        if (!string.IsNullOrWhiteSpace(filter))
        {
            sql += $" WHERE {filter}";
        }

        return await connection.ExecuteScalarAsync<long>(sql);
    }

    public async Task<bool> TableExistsAsync(string connectionString, string tableName)
    {
        using var connection = new MySqlConnection(connectionString);
        
        var count = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @TableName", new { TableName = tableName });

        return count > 0;
    }
}
