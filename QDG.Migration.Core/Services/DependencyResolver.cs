using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Services;

public class DependencyResolver : IDependencyResolver
{
    public Task<List<string>> ResolveDependencyOrderAsync(List<string> tables, List<TableRelationship> relationships)
    {
        var graph = BuildGraph(tables, relationships);
        var sorted = TopologicalSort(graph);
        
        if (sorted == null)
            throw new InvalidOperationException("Circular dependency detected");
        
        return Task.FromResult(sorted);
    }

    public Task<bool> ValidateDependenciesAsync(List<string> tables, List<TableRelationship> relationships)
    {
        try
        {
            var graph = BuildGraph(tables, relationships);
            var sorted = TopologicalSort(graph);
            return Task.FromResult(sorted != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<Dictionary<string, List<string>>> GetDependenciesAsync(string tableName, List<TableRelationship> relationships)
    {
        var dependencies = new Dictionary<string, List<string>>();
        
        var tableDeps = relationships
            .Where(r => r.ChildTable.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.ParentTable)
            .Distinct()
            .ToList();
        
        dependencies[tableName] = tableDeps;
        return Task.FromResult(dependencies);
    }

    private Dictionary<string, List<string>> BuildGraph(List<string> tables, List<TableRelationship> relationships)
    {
        var graph = tables.ToDictionary(t => t, t => new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var rel in relationships)
        {
            if (graph.ContainsKey(rel.ChildTable) && graph.ContainsKey(rel.ParentTable))
            {
                if (!graph[rel.ChildTable].Contains(rel.ParentTable, StringComparer.OrdinalIgnoreCase))
                {
                    graph[rel.ChildTable].Add(rel.ParentTable);
                }
            }
        }

        return graph;
    }

    private List<string>? TopologicalSort(Dictionary<string, List<string>> graph)
    {
        var inDegree = graph.Keys.ToDictionary(k => k, k => 0, StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in graph)
        {
            var childTable = kvp.Key;
            var parentTables = kvp.Value;

            inDegree[childTable] = parentTables.Count;
        }

        var queue = new Queue<string>(inDegree.Where(x => x.Value == 0).Select(x => x.Key));
        var result = new List<string>();

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            result.Add(node);

            foreach (var kvp in graph)
            {
                if (kvp.Value.Contains(node, StringComparer.OrdinalIgnoreCase))
                {
                    var dependentTable = kvp.Key;
                    inDegree[dependentTable]--;

                    if (inDegree[dependentTable] == 0)
                    {
                        queue.Enqueue(dependentTable);
                    }
                }
            }
        }

        return result.Count == graph.Count ? result : null;
    }
}
