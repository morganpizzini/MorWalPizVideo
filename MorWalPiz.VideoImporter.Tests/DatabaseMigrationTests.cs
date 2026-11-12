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
