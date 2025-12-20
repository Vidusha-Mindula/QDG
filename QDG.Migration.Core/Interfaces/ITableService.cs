using MySqlConnector;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Interfaces;

public interface ITableService
{
    Task<List<TableInfo>> GetTablesAsync(string connectionString);
    Task<TableInfo> GetTableInfoAsync(MySqlConnection  connection,string tableName);
    Task<long> GetRowCountAsync(MySqlConnection connection, string tableName, string? filter = null);
    Task<bool> TableExistsAsync(string connectionString, string tableName);
    Task CreateTableAsync(string connectionString, TableInfo tableInfo);
    Task<List<Dictionary<string, object>>> GetTableDataAsync(
        string connectionString,
        string tableName,
        List<string> columns,
        string? filter = null,
        int limit = 0);
}
