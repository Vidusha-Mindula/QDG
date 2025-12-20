using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Interfaces;

public interface IConfigRepository
{
    Task<List<DatabaseConfig>> GetAllAsync();
    Task<DatabaseConfig?> GetByIdAsync(int id);
    Task<DatabaseConfig?> GetActiveAsync();
    Task<int> AddAsync(DatabaseConfig config);
    Task UpdateAsync(DatabaseConfig config);
    Task DeleteAsync(int id);
    Task SetActiveAsync(int id);
}
