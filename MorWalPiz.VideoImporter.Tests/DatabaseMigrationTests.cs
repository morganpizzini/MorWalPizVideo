using System.IO;
using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Services;
using Xunit;

namespace MorWalPiz.VideoImporter.Tests;

[Collection("Desktop startup")]
public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task Initialize_database_applies_recent_migrations_before_loading_tenants()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var directory = Path.Combine(Path.GetTempPath(), $"importer-migrations-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var tenantContext = new TestTenantContext();

        try
        {
            Directory.SetCurrentDirectory(directory);

            using (var context = new AppDbContext(tenantContext))
                await context.Database.MigrateAsync("20260809223129_changes");

            var database = new DatabaseService(tenantContext);
            database.InitializeDatabase();

            var tenants = await new TenantService(database).GetActiveTenantsAsync();

            Assert.Equal(["MorWalPiz", "ShootingIta"], tenants.Select(tenant => tenant.Name));
            Assert.All(tenants, tenant => Assert.Equal(string.Empty, tenant.ChannelId));

            using var migratedContext = database.CreateContext();
            var appliedMigrations = await migratedContext.Database.GetAppliedMigrationsAsync();
            Assert.Contains("20260918120000_socialPublishing", appliedMigrations);
            Assert.Contains("20260918130000_tenantChannel", appliedMigrations);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task Initialize_database_rejects_missing_column_even_when_migration_is_recorded()
    {
        await RunWithDatabaseAsync(async database =>
        {
            database.InitializeDatabase();
            using (var context = database.CreateContext())
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Tenants\" DROP COLUMN \"ChannelId\"");

            var exception = Assert.Throws<DatabaseStartupException>(database.InitializeDatabase);

            Assert.Equal(DatabaseStartupStage.SchemaVerification, exception.Stage);
            Assert.Contains(exception.Details, detail => detail.Contains("Tenants.ChannelId", StringComparison.Ordinal));
            using var verification = database.CreateContext();
            Assert.Empty(await verification.Database.GetPendingMigrationsAsync());
        });
    }

    [Fact]
    public async Task Initialize_database_rejects_unknown_migration_history()
    {
        await RunWithDatabaseAsync(async database =>
        {
            database.InitializeDatabase();
            using (var context = database.CreateContext())
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20990101000000_future', '10.0.11')");

            var exception = Assert.Throws<DatabaseStartupException>(database.InitializeDatabase);

            Assert.Equal(DatabaseStartupStage.MigrationHistory, exception.Stage);
            Assert.Contains("20990101000000_future", exception.Details);
        });
    }

    [Fact]
    public async Task Database_service_keeps_using_its_resolved_path_after_working_directory_changes()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var firstDirectory = Path.Combine(Path.GetTempPath(), $"importer-db-path-{Guid.NewGuid():N}");
        var secondDirectory = Path.Combine(Path.GetTempPath(), $"importer-db-path-{Guid.NewGuid():N}");
        Directory.CreateDirectory(firstDirectory);
        Directory.CreateDirectory(secondDirectory);

        try
        {
            Directory.SetCurrentDirectory(firstDirectory);
            var database = new DatabaseService(new TestTenantContext());
            Directory.SetCurrentDirectory(secondDirectory);
            database.InitializeDatabase();

            using var context = database.CreateContext();
            Assert.Equal(database.DatabasePath, Path.GetFullPath(context.Database.GetDbConnection().DataSource));
            Assert.True(File.Exists(database.DatabasePath));
            Assert.False(File.Exists(Path.Combine(secondDirectory, "VideoImporter.db")));
            Assert.Equal(["MorWalPiz", "ShootingIta"], await context.Tenants
                .OrderBy(tenant => tenant.Id)
                .Select(tenant => tenant.Name)
                .ToListAsync());
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(firstDirectory, true);
            Directory.Delete(secondDirectory, true);
        }
    }

    private static async Task RunWithDatabaseAsync(Func<DatabaseService, Task> action)
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var directory = Path.Combine(Path.GetTempPath(), $"importer-migrations-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            Directory.SetCurrentDirectory(directory);
            await action(new DatabaseService(new TestTenantContext()));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public int CurrentTenantId => 1;
        public string CurrentTenantName => "MorWalPiz";

        public void SetCurrentTenant(int tenantId, string tenantName)
        {
        }

        public event EventHandler<TenantChangedEventArgs>? TenantChanged
        {
            add { }
            remove { }
        }
    }
}
