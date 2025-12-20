using MySqlConnector;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfigRepository _repository;

    public ConfigurationService(IConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DatabaseConfig>> GetAllConfigurationsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<DatabaseConfig> GetConfigurationAsync(int id)
    {
        var config = await _repository.GetByIdAsync(id);
        if (config == null)
            throw new KeyNotFoundException($"Configuration with ID {id} not found");
        return config;
    }

    public async Task<DatabaseConfig> GetActiveConfigurationAsync()
    {
        var config = await _repository.GetActiveAsync();
        if (config == null)
            throw new InvalidOperationException("No active configuration found");
        return config;
    }

    public async Task<int> SaveConfigurationAsync(DatabaseConfig config)
    {
        return await _repository.AddAsync(config);
    }

    public async Task UpdateConfigurationAsync(DatabaseConfig config)
    {
        await _repository.UpdateAsync(config);
    }

    public async Task DeleteConfigurationAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task SetActiveConfigurationAsync(int id)
    {
        await _repository.SetActiveAsync(id);
    }
}
