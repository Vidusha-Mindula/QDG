using System.Data;
using Dapper;
using MySqlConnector;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Services;

public class TableService : ITableService
{
    public async Task<List<TableInfo>> GetTablesAsync(string connectionString)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var tableNames = (await connection.QueryAsync<string>(
                "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'"))
            .ToList();

        if (!tableNames.Any())
            return new List<TableInfo>();

        var tableNamesList = tableNames.ToList();
        var allColumns = await GetAllColumnsAsync(connection, tableNamesList);
        var allForeignKeys = await GetAllForeignKeysAsync(connection, tableNamesList);
        var allPrimaryKeys = await GetAllPrimaryKeysAsync(connection, tableNamesList);
        var rowCounts = await GetAllRowCountsAsync(connection);

        var tables = new List<TableInfo>();

        foreach (var tableName in tableNamesList)
        {
            var columns = allColumns.ContainsKey(tableName) ? allColumns[tableName] : new List<ColumnInfo>();
            var foreignKeys = allForeignKeys.ContainsKey(tableName)
                ? allForeignKeys[tableName]
                : new List<ForeignKeyInfo>();
            var primaryKey = allPrimaryKeys.ContainsKey(tableName) ? allPrimaryKeys[tableName] : null;

            var tableInfo = new TableInfo
            {
                TableName = tableName,
                RowCount = rowCounts.ContainsKey(tableName) ? rowCounts[tableName] : 0,
                Columns = columns,
                ForeignKeys = foreignKeys,
                PrimaryKey = primaryKey
            };

            tables.Add(tableInfo);
            Console.WriteLine($"Table {tableName} has {tableInfo.Columns.Count} columns");
        }

        return tables;
    }

    public async Task<TableInfo> GetTableInfoAsync(MySqlConnection connection, string tableName)
    {
        var tableInfo = new TableInfo
        {
            TableName = tableName,
            RowCount = await GetRowCountAsync(connection, tableName),
            Columns = await GetColumnsAsync(connection, tableName),
            ForeignKeys = await GetForeignKeysAsync(connection, tableName),
            PrimaryKey = await GetPrimaryKeyAsync(connection, tableName)
        };

        return tableInfo;
    }

    public async Task<long> GetRowCountAsync(MySqlConnection connection, string tableName, string? filter = null)
    {
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
        await connection.OpenAsync();

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @TableName",
            new { TableName = tableName });

        return count > 0;
    }

    public async Task CreateTableAsync(string connectionString, TableInfo tableInfo)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var columnDefinitions = new List<string>();
        foreach (var column in tableInfo.Columns)
        {
            var def = $"`{column.ColumnName}` {column.DataType}";
            if (!column.IsNullable)
                def += " NOT NULL";
            if (column.IsAutoIncrement)
                def += " AUTO_INCREMENT";
            if (!string.IsNullOrWhiteSpace(column.DefaultValue))
                def += $" DEFAULT {column.DefaultValue}";

            columnDefinitions.Add(def);
        }

        if (tableInfo.PrimaryKey != null && tableInfo.PrimaryKey.Columns.Any())
        {
            var pkCols = string.Join(", ", tableInfo.PrimaryKey.Columns.Select(c => $"`{c}`"));
            columnDefinitions.Add($"PRIMARY KEY ({pkCols})");
        }

        var sql = $"CREATE TABLE IF NOT EXISTS `{tableInfo.TableName}` ({string.Join(", ", columnDefinitions)})";
        await connection.ExecuteAsync(sql);
    }

    public async Task<List<Dictionary<string, object>>> GetTableDataAsync(
        string connectionString,
        string tableName,
        List<string> columns,
        string? filter = null,
        int limit = 0)
    {
        if (columns == null || !columns.Any())
        {
            throw new ArgumentException(
                $"Columns list cannot be empty for table '{tableName}'. " +
                "Ensure field mappings are configured before migration.",
                nameof(columns));
        }

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var columnList = string.Join(", ", columns.Select(c => $"`{c}`"));
        var sql = $"SELECT {columnList} FROM `{tableName}`";

        if (!string.IsNullOrWhiteSpace(filter))
            sql += $" WHERE {filter}";

        if (limit > 0)
            sql += $" LIMIT {limit}";

        var result = await connection.QueryAsync(sql);
        return result.Select(r => (IDictionary<string, object>)r)
            .Select(d => d.ToDictionary(k => k.Key, k => k.Value))
            .ToList();
    }

    private async Task<Dictionary<string, long>> GetAllRowCountsAsync(MySqlConnection connection)
    {
        var rowCounts = await connection.QueryAsync<dynamic>(
            @"SELECT TABLE_NAME, TABLE_ROWS
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'");

        var result = new Dictionary<string, long>();
        foreach (var row in rowCounts)
        {
            result[(string)row.TABLE_NAME] = Convert.ToInt64(row.TABLE_ROWS);
        }

        return result;
    }

    private async Task<Dictionary<string, List<ColumnInfo>>> GetAllColumnsAsync(MySqlConnection connection,
        List<string> tableNames)
    {
        var pkColumns = await connection.QueryAsync<dynamic>(
            @"SELECT TABLE_NAME, COLUMN_NAME
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE() 
                AND CONSTRAINT_NAME = 'PRIMARY'");

        var pkLookup = pkColumns
            .GroupBy(x => (string)x.TABLE_NAME)
            .ToDictionary(
                g => g.Key,
                g => new HashSet<string>(g.Select(x => (string)x.COLUMN_NAME))
            );

        var columns = await connection.QueryAsync<dynamic>(
            @"SELECT 
                TABLE_NAME,
                COLUMN_NAME,
                COLUMN_TYPE,
                IS_NULLABLE,
                EXTRA,
                COLUMN_DEFAULT,
                ORDINAL_POSITION
            FROM information_schema.COLUMNS 
            WHERE TABLE_SCHEMA = DATABASE()
            ORDER BY TABLE_NAME, ORDINAL_POSITION");

        return columns
            .GroupBy(c => (string)c.TABLE_NAME)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => new ColumnInfo
                {
                    ColumnName = c.COLUMN_NAME,
                    DataType = c.COLUMN_TYPE,
                    IsNullable = c.IS_NULLABLE == "YES",
                    IsAutoIncrement = c.EXTRA != null &&
                                      c.EXTRA.ToString().Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                    IsPrimaryKey = pkLookup.ContainsKey((string)c.TABLE_NAME) &&
                                   pkLookup[(string)c.TABLE_NAME].Contains(c.COLUMN_NAME),
                    DefaultValue = c.COLUMN_DEFAULT
                }).ToList()
            );
    }

    private async Task<Dictionary<string, List<ForeignKeyInfo>>> GetAllForeignKeysAsync(MySqlConnection connection,
        List<string> tableNames)
    {
        var foreignKeys = await connection.QueryAsync<dynamic>(
            @"SELECT 
                TABLE_NAME,
                CONSTRAINT_NAME as ConstraintName,
                COLUMN_NAME as ColumnName,
                REFERENCED_TABLE_NAME as ReferencedTable,
                REFERENCED_COLUMN_NAME as ReferencedColumn
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE() 
                AND REFERENCED_TABLE_NAME IS NOT NULL");

        return foreignKeys
            .GroupBy(fk => (string)fk.TABLE_NAME)
            .ToDictionary(
                g => g.Key,
                g => g.Select(fk => new ForeignKeyInfo
                {
                    ConstraintName = fk.ConstraintName,
                    ColumnName = fk.ColumnName,
                    ReferencedTable = fk.ReferencedTable,
                    ReferencedColumn = fk.ReferencedColumn
                }).ToList()
            );
    }

    private async Task<Dictionary<string, PrimaryKeyInfo>> GetAllPrimaryKeysAsync(MySqlConnection connection,
        List<string> tableNames)
    {
        var pkColumns = await connection.QueryAsync<dynamic>(
            @"SELECT TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE() 
                AND CONSTRAINT_NAME = 'PRIMARY'
            ORDER BY TABLE_NAME, ORDINAL_POSITION");

        return pkColumns
            .GroupBy(pk => (string)pk.TABLE_NAME)
            .ToDictionary(
                g => g.Key,
                g => new PrimaryKeyInfo
                {
                    ConstraintName = "PRIMARY",
                    Columns = g.Select(pk => (string)pk.COLUMN_NAME).ToList()
                }
            );
    }

    private async Task<List<ColumnInfo>> GetColumnsAsync(MySqlConnection connection, string tableName)
    {
        var pkColumns = await connection.QueryAsync<string>(
            @"SELECT COLUMN_NAME
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = @TableName 
                AND CONSTRAINT_NAME = 'PRIMARY'",
            new { TableName = tableName });

        var pkColumnSet = new HashSet<string>(pkColumns);

        var columns = await connection.QueryAsync<dynamic>(
            @"SELECT 
                COLUMN_NAME,
                COLUMN_TYPE,
                IS_NULLABLE,
                EXTRA,
                COLUMN_DEFAULT
            FROM information_schema.COLUMNS 
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION",
            new { TableName = tableName });

        return columns.Select(c => new ColumnInfo
        {
            ColumnName = c.COLUMN_NAME,
            DataType = c.COLUMN_TYPE,
            IsNullable = c.IS_NULLABLE == "YES",
            IsAutoIncrement = c.EXTRA != null &&
                              c.EXTRA.ToString().Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
            IsPrimaryKey = pkColumnSet.Contains(c.COLUMN_NAME),
            DefaultValue = c.COLUMN_DEFAULT
        }).ToList();
    }

    private async Task<List<ForeignKeyInfo>> GetForeignKeysAsync(MySqlConnection connection, string tableName)
    {
        var foreignKeys = await connection.QueryAsync<ForeignKeyInfo>(
            @"SELECT 
                CONSTRAINT_NAME as ConstraintName,
                COLUMN_NAME as ColumnName,
                REFERENCED_TABLE_NAME as ReferencedTable,
                REFERENCED_COLUMN_NAME as ReferencedColumn
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = @TableName 
                AND REFERENCED_TABLE_NAME IS NOT NULL",
            new { TableName = tableName });

        return foreignKeys.ToList();
    }

    private async Task<PrimaryKeyInfo?> GetPrimaryKeyAsync(MySqlConnection connection, string tableName)
    {
        var pkColumns = await connection.QueryAsync<string>(
            @"SELECT COLUMN_NAME
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = @TableName 
                AND CONSTRAINT_NAME = 'PRIMARY'
            ORDER BY ORDINAL_POSITION",
            new { TableName = tableName });

        var columnList = pkColumns.ToList();
        if (!columnList.Any())
            return null;

        return new PrimaryKeyInfo
        {
            ConstraintName = "PRIMARY",
            Columns = columnList
        };
    }
}