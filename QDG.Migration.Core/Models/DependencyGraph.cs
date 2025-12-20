namespace QDG.Migration.Core.Models;

public class DependencyGraph
{
    public Dictionary<string, List<string>> Dependencies { get; set; } = new();
    public List<string> IndependentTables { get; set; } = new();
    public bool HasCircularDependency { get; set; }
}
