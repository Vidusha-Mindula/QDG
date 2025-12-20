using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QDG.Migration.Core.Interfaces;

namespace QDG.Migration.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IConfigurationService _configService;

    public HomeController(IConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var activeConfig = await _configService.GetActiveConfigurationAsync();
            ViewBag.HasActiveConfig = true;
        }
        catch (InvalidOperationException)
        {
            ViewBag.HasActiveConfig = false;
        }

        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
