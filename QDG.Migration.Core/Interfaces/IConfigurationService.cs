using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Interfaces;

public interface IConfigurationService
{
    Task<List<DatabaseConfig>> GetAllConfigurationsAsync();
    Task<DatabaseConfig> GetConfigurationAsync(int id);
    Task<DatabaseConfig> GetActiveConfigurationAsync();
    Task<int> SaveConfigurationAsync(DatabaseConfig config);
    Task UpdateConfigurationAsync(DatabaseConfig config);
    Task DeleteConfigurationAsync(int id);
    Task<bool> TestConnectionAsync(string connectionString);
    Task SetActiveConfigurationAsync(int id);
}
