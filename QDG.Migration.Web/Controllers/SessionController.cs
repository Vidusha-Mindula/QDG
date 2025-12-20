using Microsoft.AspNetCore.Mvc;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;

namespace QDG_DB_Migrator.Controllers;

public class SessionController : Controller
{
    private readonly IMigrationService _migrationService;

    public SessionController(IMigrationService migrationService)
    {
        _migrationService = migrationService;
    }

    public async Task<IActionResult> Index(MigrationStatus? status = null, string sortBy = "created", bool descending = true)
    {
        var sessions = await _migrationService.GetSessionsAsync(status, sortBy, descending);
        var statistics = await _migrationService.GetSessionStatisticsAsync();

        ViewBag.CurrentStatus = status;
        ViewBag.CurrentSortBy = sortBy;
        ViewBag.CurrentDescending = descending;
        ViewBag.Statistics = statistics;
        ViewBag.TotalSessions = sessions.Count;

        return View(sessions);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid sessionId)
    {
        await _migrationService.DeleteSessionAsync(sessionId);
        return RedirectToAction(nameof(Index));
    }
}