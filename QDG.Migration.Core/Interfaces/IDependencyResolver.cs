using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Interfaces;

public interface IDependencyResolver
{
    Task<List<string>> ResolveDependencyOrderAsync(List<string> tables, List<TableRelationship> relationships);
    Task<bool> ValidateDependenciesAsync(List<string> tables, List<TableRelationship> relationships);
    Task<Dictionary<string, List<string>>> GetDependenciesAsync(string tableName, List<TableRelationship> relationships);
}
