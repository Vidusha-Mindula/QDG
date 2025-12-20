namespace QDG_DB_Migrator.Models
{
    public class FieldMappingViewModel
    {
        public Guid SessionId { get; set; }
        public List<TableFieldMappingViewModel> TableMappings { get; set; } = new();
    }

    public class TableFieldMappingViewModel
    {
        public string TableName { get; set; } = string.Empty;
        public List<ColumnMappingItem> ColumnMappings { get; set; } = new();
    }

    public class ColumnMappingItem
    {
        public string SourceColumn { get; set; } = string.Empty;
        public string DestinationColumn { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
    }

    public class FieldMappingSubmit
    {
        public string TableName { get; set; } = string.Empty;
        public string SourceColumn { get; set; } = string.Empty;
        public string DestinationColumn { get; set; } = string.Empty;
    }
}
