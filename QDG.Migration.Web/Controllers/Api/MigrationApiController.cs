using Microsoft.AspNetCore.Mvc;
using QDG.Migration.Core.Interfaces;

namespace QDG_DB_Migrator.Controllers.Api;

[ApiController]
[Route("api/migration")]
public class MigrationApiController : ControllerBase
{
    private readonly IMigrationService _migrationService;
    private readonly ISessionRepository _sessionRepository;

    public MigrationApiController(IMigrationService migrationService, ISessionRepository sessionRepository)
    {
        _migrationService = migrationService;
        _sessionRepository = sessionRepository;
    }

    [HttpGet("progress/{sessionId}")]
    public async Task<IActionResult> GetProgress(Guid sessionId)
    {
        try
        {
            var session = await _migrationService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound();
            }

            var stats = await _sessionRepository.GetMigrationStatsAsync(session.Id);
            var logs = await _sessionRepository.GetRecentLogsAsync(session.Id, 20);

            var elapsedSeconds = session.StartedAt.HasValue 
                ? (DateTime.UtcNow - session.StartedAt.Value).TotalSeconds 
                : 0;

            var tableProgressList = stats.Select(s => new
            {
                tableName = s.TableName,
                status = s.CompletedAt.HasValue ? "Completed" : (s.StartedAt != default ? "Running" : "Pending"),
                rowsCopied = s.RowsCopied,
                totalRows = s.SourceRowCount,
                errorCount = s.ErrorCount
            }).OrderBy(t => t.status != "Completed" ? 0 : 1).ThenBy(t => t.tableName).ToList();

            var response = new
            {
                status = session.Status.ToString(),
                totalTables = stats.Count,
                completedTables = stats.Count(s => s.CompletedAt.HasValue),
                currentTable = stats.LastOrDefault(s => !s.CompletedAt.HasValue)?.TableName ?? "",
                totalRowsCopied = stats.Sum(s => s.RowsCopied),
                errorCount = stats.Sum(s => s.ErrorCount),
                elapsed = elapsedSeconds,
                isComplete = session.Status == QDG.Migration.Core.Models.MigrationStatus.Completed || 
                            session.Status == QDG.Migration.Core.Models.MigrationStatus.Failed,
                logs = logs.Select(l => new
                {
                    level = l.LogLevel,
                    message = l.Message,
                    timestamp = l.CreatedAt
                }),
                tableProgress = tableProgressList
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("cancel/{sessionId}")]
    public async Task<IActionResult> CancelMigration(Guid sessionId)
    {
        try
        {
            await _migrationService.UpdateSessionStatusAsync(sessionId, QDG.Migration.Core.Models.MigrationStatus.Cancelled);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("logs/{sessionId}")]
    public async Task<IActionResult> GetLogs(Guid sessionId, [FromQuery] int count = 50)
    {
        try
        {
            var session = await _migrationService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound();
            }

            var logs = await _sessionRepository.GetRecentLogsAsync(session.Id, count);
            
            return Ok(logs.Select(l => new
            {
                level = l.LogLevel,
                message = l.Message,
                tableName = l.TableName,
                timestamp = l.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
