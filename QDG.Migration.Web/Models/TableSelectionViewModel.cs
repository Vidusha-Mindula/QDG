using QDG.Migration.Core.Models;

namespace QDG_DB_Migrator.Models;

public class TableSelectionViewModel
{
    public Guid SessionId { get; set; }
    public List<TableInfo> Tables { get; set; } = new();
    public HashSet<string> SelectedTables { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
}
