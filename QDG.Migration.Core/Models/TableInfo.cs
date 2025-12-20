namespace QDG.Migration.Core.Models;

public class TableInfo
{
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();
    public PrimaryKeyInfo? PrimaryKey { get; set; }
}

public class ColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsAutoIncrement { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string? DefaultValue { get; set; }
}

public class ForeignKeyInfo
{
    public string ConstraintName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public string ReferencedColumn { get; set; } = string.Empty;
}

public class PrimaryKeyInfo
{
    public string ConstraintName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
}
