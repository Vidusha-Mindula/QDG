using Microsoft.EntityFrameworkCore;
using QDG.Migration.Core.Models;

namespace QDG.Migration.Data;

public class AppDbContext : DbContext
{
    public DbSet<DatabaseConfig> DatabaseConfigurations { get; set; }
    public DbSet<MigrationSession> MigrationSessions { get; set; }
    public DbSet<SelectedTable> SelectedTables { get; set; }
    public DbSet<TableRelationship> TableRelationships { get; set; }
    public DbSet<ColumnConfiguration> ColumnConfigurations { get; set; }
    public DbSet<FieldMapping> FieldMappings { get; set; }
    public DbSet<MigrationStats> MigrationStats { get; set; }
    public DbSet<IdMapping> IdMappings { get; set; }
    public DbSet<MigrationLog> MigrationLogs { get; set; }
    public DbSet<User> Users { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DatabaseConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.SourceHost).IsRequired();
            entity.Property(e => e.SourceDatabase).IsRequired();
            entity.Property(e => e.DestinationHost).IsRequired();
            entity.Property(e => e.DestinationDatabase).IsRequired();
        });

        modelBuilder.Entity<MigrationSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionGuid).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.SessionGuid).IsRequired();
        });

        modelBuilder.Entity<SelectedTable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<TableRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<ColumnConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<FieldMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<MigrationStats>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<IdMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SessionId, e.TableName, e.SourceId }).IsUnique();
        });

        modelBuilder.Entity<MigrationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SessionId, e.CreatedAt });
        });
    }
}
