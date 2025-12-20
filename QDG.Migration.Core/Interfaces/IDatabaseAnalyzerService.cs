using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Interfaces;

public interface IDatabaseAnalyzerService
{
    Task<List<string>> GetTableNamesAsync(int dbConfigId);
}