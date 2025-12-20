using Microsoft.EntityFrameworkCore;
using QDG.Migration.Core.Helpers;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Data.Repositories;

public class ConfigRepository : IConfigRepository
{
    private readonly AppDbContext _context;

    public ConfigRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DatabaseConfig>> GetAllAsync()
    {
        var configs = await _context.DatabaseConfigurations.ToListAsync();
        // foreach (var config in configs)
        // {
        //     config.SourcePassword = EncryptionHelper.Decrypt(config.SourcePassword);
        //     config.DestinationPassword = EncryptionHelper.Decrypt(config.DestinationPassword);
        // }
        return configs;
    }

    public async Task<DatabaseConfig?> GetByIdAsync(int id)
    {
        var config = await _context.DatabaseConfigurations.FindAsync(id);
        // if (config != null)
        // {
        //     config.SourcePassword = EncryptionHelper.Decrypt(config.SourcePassword);
        //     config.DestinationPassword = EncryptionHelper.Decrypt(config.DestinationPassword);
        // }
        return config;
    }

    public async Task<DatabaseConfig?> GetActiveAsync()
    {
        var config = await _context.DatabaseConfigurations.FirstOrDefaultAsync(c => c.IsActive);
        // if (config != null)
        // {
        //     config.SourcePassword = EncryptionHelper.Decrypt(config.SourcePassword);
        //     config.DestinationPassword = EncryptionHelper.Decrypt(config.DestinationPassword);
        //  }
        return config;
    }

    public async Task<int> AddAsync(DatabaseConfig config)
    {
        config.CreatedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
        
        _context.DatabaseConfigurations.Add(config);
        await _context.SaveChangesAsync();
        return config.Id;
    }

    public async Task UpdateAsync(DatabaseConfig config)
    {
        config.SourcePassword = EncryptionHelper.Encrypt(config.SourcePassword);
        config.DestinationPassword = EncryptionHelper.Encrypt(config.DestinationPassword);
        config.UpdatedAt = DateTime.UtcNow;
        
        _context.DatabaseConfigurations.Update(config);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var config = await _context.DatabaseConfigurations.FindAsync(id);
        if (config != null)
        {
            _context.DatabaseConfigurations.Remove(config);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetActiveAsync(int id)
    {
        var configs = await _context.DatabaseConfigurations.ToListAsync();
        foreach (var config in configs)
        {
            config.IsActive = config.Id == id;
        }
        await _context.SaveChangesAsync();
    }
}
