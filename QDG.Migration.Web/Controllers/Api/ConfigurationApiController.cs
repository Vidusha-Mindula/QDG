using Microsoft.AspNetCore.Mvc;
using QDG.Migration.Core.Interfaces;

namespace QDG_DB_Migrator.Controllers.Api;

[ApiController]
[Route("api/configuration")]
public class ConfigurationApiController : ControllerBase
{
    private readonly IConfigurationService _configService;

    public ConfigurationApiController(IConfigurationService configService)
    {
        _configService = configService;
    }

    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request)
    {
        try
        {
            var connectionString = $"Server={request.Host};Port={request.Port};Database={request.Database};Uid={request.Username};Pwd={request.Password};";
            var result = await _configService.TestConnectionAsync(connectionString);
            
            return Ok(new { success = result, message = result ? "Connection successful" : "Connection failed" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class TestConnectionRequest
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
