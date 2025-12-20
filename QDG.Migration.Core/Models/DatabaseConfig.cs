using QDG.Migration.Core.Helpers;

namespace QDG.Migration.Core.Models;

public class DatabaseConfig
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SourceHost { get; set; }
    public int SourcePort { get; set; }
    public string SourceDatabase { get; set; }
    public string SourceUsername { get; set; }
    public string SourcePassword { get; set; } // Should be encrypted
    public string DestinationHost { get; set; }
    public int DestinationPort { get; set; }
    public string DestinationDatabase { get; set; }
    public string DestinationUsername { get; set; }
    public string DestinationPassword { get; set; } // Should be encrypted
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string SourceConnectionString =>
        $"Server={SourceHost};Port={SourcePort};Database={SourceDatabase};" +
        $"Uid={SourceUsername};Pwd={EncryptionHelper.Decrypt(SourcePassword)};";

    public string DestinationConnectionString =>
        $"Server={DestinationHost};Port={DestinationPort};Database={DestinationDatabase};" +
        $"Uid={DestinationUsername};Pwd={EncryptionHelper.Decrypt(DestinationPassword)};";
}
