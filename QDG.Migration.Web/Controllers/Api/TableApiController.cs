using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using QDG.Migration.Core.Interfaces;

namespace QDG_DB_Migrator.Controllers.Api;

[ApiController]
[Route("api/table")]
public class TableApiController : ControllerBase
{
    private readonly ITableService _tableService;
    private readonly IConfigurationService _configService;

    public TableApiController(ITableService tableService, IConfigurationService configService)
    {
        _tableService = tableService;
        _configService = configService;
    }

    [HttpPost("test-filter")]
    public async Task<IActionResult> TestFilter([FromBody] TestFilterRequest request)
    {
        try
        {
            var config = await _configService.GetActiveConfigurationAsync();
            if (config == null)
            {
                return BadRequest(new { error = "No active configuration found" });
            }
            
            using var connection = new MySqlConnection(config.SourceConnectionString);
            await connection.OpenAsync();

            var count = await _tableService.GetRowCountAsync(
                connection,
                request.TableName,
                request.Filter);

            return Ok(new { success = true, count, originalCount = request.OriginalCount });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("info/{tableName}")]
    public async Task<IActionResult> GetTableInfo(string tableName)
    {
        try
        {
            var config = await _configService.GetActiveConfigurationAsync();
            if (config == null)
            {
                return BadRequest(new { error = "No active configuration found" });
            }

            using var connection = new MySqlConnection(config.SourceConnectionString);
            await connection.OpenAsync();
            
            var tableInfo = await _tableService.GetTableInfoAsync(connection, tableName);
            
            return Ok(tableInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class TestFilterRequest
{
    public string TableName { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;
    public long OriginalCount { get; set; }
}
