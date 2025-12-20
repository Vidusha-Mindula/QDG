using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;
using QDG_DB_Migrator.Models;

namespace QDG_DB_Migrator.Controllers;

public class MigrationController : Controller
{
    private readonly IConfigurationService _configService;
    private readonly ITableService _tableService;
    private readonly IMigrationService _migrationService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IRelationshipService _relationshipService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MigrationController(
        IConfigurationService configService,
        ITableService tableService,
        IMigrationService migrationService,
        ISessionRepository sessionRepository,
        IRelationshipService relationshipService,
        IServiceScopeFactory serviceScopeFactory)
    {
        _configService = configService;
        _tableService = tableService;
        _migrationService = migrationService;
        _sessionRepository = sessionRepository;
        _relationshipService = relationshipService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    [HttpGet]
    public async Task<IActionResult> TableSelection()
    {
        try
        {
            var config = await _configService.GetActiveConfigurationAsync();
            var session = await _migrationService.CreateSessionAsync(config.Id);
            
            var tables = await _tableService.GetTablesAsync(config.SourceConnectionString);
            
            var model = new TableSelectionViewModel
            {
                SessionId = session.SessionGuid,
                Tables = tables
            };

            return View(model);
        }
        catch (InvalidOperationException)
        {
            TempData["ErrorMessage"] = "No active configuration found. Please configure database connections first.";
            return RedirectToAction("Index", "Configuration");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error loading tables: {ex.Message}";
            return RedirectToAction("Index", "Configuration");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TableSelection(Guid sessionId, string selectedTables)
    {
        if (string.IsNullOrWhiteSpace(selectedTables))
        {
            TempData["ErrorMessage"] = "Please select at least one table.";
            return RedirectToAction(nameof(TableSelection));
        }

        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        var tableList = selectedTables.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        
        for (int i = 0; i < tableList.Count; i++)
        {
            await _sessionRepository.AddSelectedTableAsync(new SelectedTable
            {
                SessionId = session.Id,
                TableName = tableList[i].Trim(),
                RowCount = 0,
                SortOrder = i
            });
        }

        return RedirectToAction(nameof(FilterConfig), new { sessionId });
    }

    [HttpGet]
    public async Task<IActionResult> FilterConfig(Guid sessionId)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        var selectedTables = await _sessionRepository.GetSelectedTablesAsync(session.Id);
        
        var model = new FilterConfigViewModel
        {
            SessionId = sessionId,
            TableFilters = selectedTables.Select(t => new TableFilterItem
            {
                TableName = t.TableName,
                RowCount = t.RowCount,
                FilterCondition = t.FilterCondition ?? string.Empty
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FilterConfig(Guid sessionId, List<TableFilterItem> filters)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        foreach (var filter in filters)
        {
            await _sessionRepository.UpdateTableFilterAsync(session.Id, filter.TableName, filter.FilterCondition);
        }

        return RedirectToAction(nameof(RelationshipConfig), new { sessionId });
    }

    [HttpGet]
    public async Task<IActionResult> RelationshipConfig(Guid sessionId)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        var config = await _configService.GetActiveConfigurationAsync();
        var selectedTables = await _sessionRepository.GetSelectedTablesAsync(session.Id);
        var tableNames = selectedTables.Select(t => t.TableName).ToList();

        var detectedRelationships = await _relationshipService.GetDatabaseRelationshipsAsync(
            config.SourceConnectionString, 
            tableNames);

        var customRelationships = (await _sessionRepository.GetRelationshipsAsync(session.Id))
            .Where(r => r.IsCustom)
            .ToList();

        using var connection = new MySqlConnection(config.SourceConnectionString);
        await connection.OpenAsync();
        
        var tableColumns = new Dictionary<string, List<ColumnInfo>>();
        foreach (var tableName in tableNames)
        {
            var tableInfo = await _tableService.GetTableInfoAsync(connection, tableName);
            tableColumns[tableName] = tableInfo.Columns;
        }

        var allRelationships = detectedRelationships.Concat(customRelationships).ToList();
        var dependencyGraph = await _relationshipService.BuildDependencyGraphAsync(tableNames, allRelationships);

        var columnConfigs = await _sessionRepository.GetColumnConfigurationsAsync(session.Id);

        var model = new RelationshipConfigViewModel
        {
            SessionId = sessionId,
            DetectedRelationships = detectedRelationships,
            CustomRelationships = customRelationships,
            AvailableTables = tableNames,
            TableColumns = tableColumns,
            ColumnConfigurations = columnConfigs,
            HasCircularDependency = dependencyGraph.HasCircularDependency,
            DependencyVisualization = BuildDependencyVisualization(dependencyGraph),
            MigrationOrder = dependencyGraph.HasCircularDependency 
                ? new List<string>() 
                : await _relationshipService.GetMigrationOrderAsync(dependencyGraph)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RelationshipConfig(Guid sessionId, List<ColumnConfiguration> columnConfigs)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (columnConfigs != null && columnConfigs.Any())
        {
            foreach (var config in columnConfigs)
            {
                config.SessionId = session.Id;
                await _sessionRepository.UpdateColumnConfigurationAsync(config);
            }
        }

        return RedirectToAction(nameof(FieldMapping), new { sessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCustomRelationship(Guid sessionId, string parentTable, string parentColumn, 
        string childTable, string childColumn)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return Json(new { success = false, message = "Session not found" });
        }

        var relationship = new TableRelationship
        {
            SessionId = session.Id,
            ParentTable = parentTable,
            ParentColumn = parentColumn,
            ChildTable = childTable,
            ChildColumn = childColumn,
            IsCustom = true
        };

        await _sessionRepository.AddRelationshipAsync(relationship);

        return Json(new { success = true, message = "Relationship added successfully" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRelationship(int relationshipId)
    {
        await _sessionRepository.DeleteRelationshipAsync(relationshipId);
        return Json(new { success = true, message = "Relationship deleted successfully" });
    }

    private string BuildDependencyVisualization(DependencyGraph graph)
    {
        var lines = new List<string>();
        
        if (graph.IndependentTables.Any())
        {
            lines.Add("Independent Tables (no dependencies):");
            foreach (var table in graph.IndependentTables)
            {
                lines.Add($"  • {table}");
            }
            lines.Add("");
        }

        lines.Add("Dependencies:");
        foreach (var kvp in graph.Dependencies.Where(d => d.Value.Any()))
        {
            lines.Add($"  {kvp.Key} depends on:");
            foreach (var dep in kvp.Value)
            {
                lines.Add($"    → {dep}");
            }
        }

        return string.Join("\n", lines);
    }

    [HttpGet]
    public async Task<IActionResult> FieldMapping(Guid sessionId)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        var config = await _configService.GetActiveConfigurationAsync();
        var selectedTables = await _sessionRepository.GetSelectedTablesAsync(session.Id);
        var columnConfigs = await _sessionRepository.GetColumnConfigurationsAsync(session.Id);

        using var connection = new MySqlConnection(config.SourceConnectionString);
        await connection.OpenAsync();

        var tableMappings = new List<TableFieldMappingViewModel>();

        foreach (var table in selectedTables)
        {
            var tableInfo = await _tableService.GetTableInfoAsync(connection, table.TableName);
            var existingMappings = await _sessionRepository.GetFieldMappingsAsync(session.Id);

            var droppedColumns = columnConfigs
                .Where(c => c.TableName == table.TableName && c.DropColumn)
                .Select(c => c.ColumnName)
                .ToList();

            var columnMappings = tableInfo.Columns
                .Where(col => !droppedColumns.Contains(col.ColumnName))
                .Select(col =>
                {
                    var existing = existingMappings.FirstOrDefault(m =>
                        m.TableName == table.TableName &&
                        m.SourceColumn == col.ColumnName);

                    return new ColumnMappingItem
                    {
                        SourceColumn = col.ColumnName,
                        DestinationColumn = existing?.DestinationColumn ?? col.ColumnName,
                        DataType = col.DataType,
                        IsNullable = col.IsNullable,
                        IsPrimaryKey = col.IsPrimaryKey
                    };
                }).ToList();

            tableMappings.Add(new TableFieldMappingViewModel
            {
                TableName = table.TableName,
                ColumnMappings = columnMappings
            });
        }

        var model = new FieldMappingViewModel
        {
            SessionId = sessionId,
            TableMappings = tableMappings
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FieldMappingPost(Guid sessionId, List<FieldMappingSubmit> mappings)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (mappings != null && mappings.Any())
        {
            foreach (var mapping in mappings)
            {
                await _sessionRepository.SaveOrUpdateFieldMappingAsync(new FieldMapping
                {
                    SessionId = session.Id,
                    TableName = mapping.TableName,
                    SourceColumn = mapping.SourceColumn,
                    DestinationColumn = string.IsNullOrWhiteSpace(mapping.DestinationColumn)
                        ? mapping.SourceColumn
                        : mapping.DestinationColumn
                });
            }
        }

        return RedirectToAction(nameof(Summary), new { sessionId });
    }

    [HttpGet]
    public async Task<IActionResult> Summary(Guid sessionId)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        return View(session);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartMigration(Guid sessionId)
    {
        await _migrationService.UpdateSessionStatusAsync(sessionId, MigrationStatus.Running);
        
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
                await migrationService.ExecuteMigrationAsync(sessionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        });

        return RedirectToAction(nameof(Progress), new { sessionId });
    }

    [HttpGet]
    public async Task<IActionResult> Progress(Guid sessionId)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        ViewBag.SessionId = sessionId;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Results(Guid sessionId)
    {
        var session = await _migrationService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        var result = await _migrationService.GetMigrationResultAsync(sessionId);
        return View(result);
    }
}
