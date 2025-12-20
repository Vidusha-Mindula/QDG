using QDG.Migration.Core.Models;

namespace QDG.Migration.Core.Interfaces;

public interface IMigrationService
{
    Task<MigrationSession> CreateSessionAsync(int configurationId);
    Task<MigrationSession?> GetSessionAsync(Guid sessionGuid);
    Task UpdateSessionStatusAsync(Guid sessionGuid, MigrationStatus status);
    Task<MigrationResult> ExecuteMigrationAsync(
        Guid sessionGuid,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default);
    Task<TableMigrationResult> MigrateTableAsync(
        MigrationSession session,
        string tableName,
        IProgress<TableProgress>? progress = null);
    Task<List<MigrationSession>> GetRecentSessionsAsync(int count = 10);
    Task<MigrationResult> GetMigrationResultAsync(Guid sessionGuid);
    Task<List<MigrationSession>> GetSessionsAsync(MigrationStatus? status = null, string sortBy = "created", bool descending = true);
    Task<Dictionary<MigrationStatus, int>> GetSessionStatisticsAsync();
    Task DeleteSessionAsync(Guid sessionGuid);
}
