using Microsoft.AspNetCore.Mvc;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;
using QDG_DB_Migrator.Models;
using QDG.Migration.Core.Helpers;

namespace QDG_DB_Migrator.Controllers;

public class ConfigurationController : Controller
{
    private readonly IConfigurationService _configService;

    public ConfigurationController(IConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task<IActionResult> Index()
    {
        var configs = await _configService.GetAllConfigurationsAsync();
        return View(configs);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ConfigurationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ConfigurationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var config = new DatabaseConfig
            {
                Name = model.Name,
                SourceHost = model.SourceHost,
                SourcePort = model.SourcePort,
                SourceDatabase = model.SourceDatabase,
                SourceUsername = model.SourceUsername,
                SourcePassword = EncryptionHelper.Encrypt(model.SourcePassword),
                DestinationHost = model.DestinationHost,
                DestinationPort = model.DestinationPort,
                DestinationDatabase = model.DestinationDatabase,
                DestinationUsername = model.DestinationUsername,
                DestinationPassword = EncryptionHelper.Encrypt(model.DestinationPassword),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _configService.SaveConfigurationAsync(config);
            TempData["SuccessMessage"] = "Configuration saved successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error saving configuration: {ex.Message}");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var config = await _configService.GetConfigurationAsync(id);
        if (config == null)
        {
            return NotFound();
        }

        var model = new ConfigurationViewModel
        {
            Id = config.Id,
            Name = config.Name,
            SourceHost = config.SourceHost,
            SourcePort = config.SourcePort,
            SourceDatabase = config.SourceDatabase,
            SourceUsername = config.SourceUsername,
            SourcePassword = config.SourcePassword,
            DestinationHost = config.DestinationHost,
            DestinationPort = config.DestinationPort,
            DestinationDatabase = config.DestinationDatabase,
            DestinationUsername = config.DestinationUsername,
            DestinationPassword = config.DestinationPassword
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ConfigurationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var config = await _configService.GetConfigurationAsync(model.Id);
            if (config == null)
            {
                return NotFound();
            }

            config.Name = model.Name;
            config.SourceHost = model.SourceHost;
            config.SourcePort = model.SourcePort;
            config.SourceDatabase = model.SourceDatabase;
            config.SourceUsername = model.SourceUsername;
            config.SourcePassword = model.SourcePassword;
            config.DestinationHost = model.DestinationHost;
            config.DestinationPort = model.DestinationPort;
            config.DestinationDatabase = model.DestinationDatabase;
            config.DestinationUsername = model.DestinationUsername;
            config.DestinationPassword = model.DestinationPassword;
            config.UpdatedAt = DateTime.UtcNow;

            await _configService.UpdateConfigurationAsync(config);
            TempData["SuccessMessage"] = "Configuration updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error updating configuration: {ex.Message}");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _configService.DeleteConfigurationAsync(id);
            TempData["SuccessMessage"] = "Configuration deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting configuration: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id)
    {
        try
        {
            await _configService.SetActiveConfigurationAsync(id);
            TempData["SuccessMessage"] = "Active configuration set successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error setting active configuration: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
