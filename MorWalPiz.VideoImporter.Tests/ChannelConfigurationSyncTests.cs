using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using Xunit;

namespace MorWalPiz.VideoImporter.Tests;

[Collection("Desktop startup")]
public sealed class ChannelConfigurationSyncTests
{
    [Fact]
    public async Task First_use_seeds_only_after_no_content_and_projects_acknowledged_values()
    {
        await RunWithDatabaseAsync(async (database, tenantContext) =>
        {
            using (var context = database.CreateContext())
            {
                var settings = await context.Settings.SingleAsync();
                settings.ApplicationName = "Local name";
                settings.DefaultHashtags = "local, tags";
                settings.ApiEndpoint = "https://local-backoffice.test";
                settings.ApiKey = "local-secret";
                settings.ChannelId = "local-channel";
                await context.SaveChangesAsync();
            }

            var factory = new FakeApiServiceFactory("settings-missing");
            var sync = CreateService(database, tenantContext, factory);
            var status = await sync.SynchronizeAsync("channel-1");

            Assert.Equal(ChannelConfigurationSyncState.Synchronized, status.State);
            Assert.Equal(1, factory.GetSettingsRequestCount);
            Assert.Equal(1, factory.PutSettingsRequestCount);
            var saved = Assert.IsType<MorWalPiz.Contracts.Contracts.VideoImporterChannelSettingsResponse>(
                factory.GetSavedSettings("channel-1"));
            Assert.DoesNotContain("apiEndpoint", saved.Payload.GetRawText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("apiKey", saved.Payload.GetRawText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("channelId", saved.Payload.GetRawText(), StringComparison.OrdinalIgnoreCase);

            using var projected = database.CreateContext();
            var projectedSettings = await projected.Settings.SingleAsync();
            Assert.Equal("Local name", projectedSettings.ApplicationName);
            Assert.Equal("local, tags", projectedSettings.DefaultHashtags);
            Assert.Equal("https://local-backoffice.test", projectedSettings.ApiEndpoint);
            Assert.Equal("local-secret", projectedSettings.ApiKey);
            Assert.Equal("local-channel", projectedSettings.ChannelId);
        });
    }

    [Theory]
    [InlineData("route-missing")]
    [InlineData("settings-auth")]
    [InlineData("settings-transient")]
    [InlineData("settings-malformed")]
    public async Task Failed_or_malformed_get_never_seeds_or_changes_local_cache(string scenario)
    {
        await RunWithDatabaseAsync(async (database, tenantContext) =>
        {
            var factory = new FakeApiServiceFactory(scenario);
            var sync = CreateService(database, tenantContext, factory);
            var status = await sync.SynchronizeAsync("channel-1");

            Assert.Equal(ChannelConfigurationSyncState.LocalOnly, status.State);
            Assert.Equal(0, factory.PutSettingsRequestCount);
            using var context = database.CreateContext();
            Assert.Equal("MorWalPiz Site", (await context.Settings.SingleAsync()).ApplicationName);
            Assert.False(sync.CanAttemptSaveFor("channel-1"));
        });
    }

    [Fact]
    public async Task Cancelled_sync_does_not_contact_backoffice_or_seed_local_values()
    {
        await RunWithDatabaseAsync(async (database, tenantContext) =>
        {
            var factory = new FakeApiServiceFactory("settings-missing");
            var sync = CreateService(database, tenantContext, factory);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                sync.SynchronizeAsync("channel-1", cancellation.Token));

            Assert.Equal(0, factory.GetSettingsRequestCount);
            Assert.Equal(0, factory.PutSettingsRequestCount);
            using var context = database.CreateContext();
            Assert.Equal("MorWalPiz Site", (await context.Settings.SingleAsync()).ApplicationName);
        });
    }

    [Fact]
    public async Task Remote_snapshot_wins_and_rebuilds_local_rows_without_overwriting_bootstrap_fields()
    {
        await RunWithDatabaseAsync(async (database, tenantContext) =>
        {
            var factory = new FakeApiServiceFactory("settings-existing");
            factory.SetRemoteSettings("channel-1", new ChannelConfigurationSnapshot
            {
                ApplicationName = "Remote application",
                DefaultHashtags = "remote, tags",
                Languages =
                [
                    new ChannelLanguageSnapshot("EN", "English", true, true, "Remote disclaimer"),
                    new ChannelLanguageSnapshot("it", "Italiano", false, false, null)
                ],
                PublishSchedules =
                [
                    new ChannelPublishScheduleSnapshot("Remote schedule", 31, new TimeSpan(18, 30, 0), true)
                ]
            });
            using (var context = database.CreateContext())
            {
                var settings = await context.Settings.SingleAsync();
                settings.ApiEndpoint = "https://bootstrap.test";
                settings.ApiKey = "do-not-sync";
                settings.ChannelId = "bootstrap-channel";
                await context.SaveChangesAsync();
            }

            var sync = CreateService(database, tenantContext, factory);
            var status = await sync.SynchronizeAsync("channel-1");

            Assert.Equal(ChannelConfigurationSyncState.Synchronized, status.State);
            Assert.Equal(0, factory.PutSettingsRequestCount);
            using var projected = database.CreateContext();
            var projectedSettings = await projected.Settings.SingleAsync();
            Assert.Equal("Remote application", projectedSettings.ApplicationName);
            Assert.Equal("remote, tags", projectedSettings.DefaultHashtags);
            Assert.Equal("https://bootstrap.test", projectedSettings.ApiEndpoint);
            Assert.Equal("do-not-sync", projectedSettings.ApiKey);
            Assert.Equal("bootstrap-channel", projectedSettings.ChannelId);
            var languages = await projected.Languages.OrderBy(language => language.Code).ToListAsync();
            Assert.Equal(["en", "it"], languages.Select(language => language.Code));
            Assert.Single(await projected.Disclaimers.ToListAsync());
            Assert.Equal("Remote disclaimer", (await projected.Disclaimers.SingleAsync()).Text);
            var schedule = await projected.PublishSchedules.SingleAsync();
            Assert.Equal("Remote schedule", schedule.Name);
            Assert.Equal(new TimeSpan(18, 30, 0), schedule.PublishTime);
        });
    }

    [Fact]
    public async Task Remote_projection_changes_only_the_selected_tenant()
    {
        await RunWithDatabaseAsync(async (database, tenantContext) =>
        {
            using (var context = database.CreateContext())
            {
                var existingSettings = await context.Settings.SingleAsync();
                existingSettings.ApplicationName = "Tenant one local value";
                await context.SaveChangesAsync();
            }

            tenantContext.SetCurrentTenant(2, "ShootingIta");
            using (var context = database.CreateContext())
            {
                var existingSettings = await context.Settings.SingleAsync();
                existingSettings.ApplicationName = "Tenant two local value";
                (await context.Tenants.SingleAsync(tenant => tenant.Id == 2)).ChannelId = "channel-2";
                context.Languages.Add(new Language
                {
                    Code = "it",
                    Name = "Italiano",
                    IsDefault = true,
                    IsSelected = true
                });
                await context.SaveChangesAsync();
            }

            var factory = new FakeApiServiceFactory("settings-existing");
            factory.SetRemoteSettings("channel-2", new ChannelConfigurationSnapshot
            {
                ApplicationName = "Tenant two remote value",
                DefaultHashtags = "tenant two",
                Languages = [new ChannelLanguageSnapshot("en", "English", true, true, null)],
                PublishSchedules = []
            });
            var sync = CreateService(database, tenantContext, factory);
            Assert.Equal(
                ChannelConfigurationSyncState.Synchronized,
                (await sync.SynchronizeAsync("channel-2")).State);

            tenantContext.SetCurrentTenant(1, "MorWalPiz");
            using var tenantOne = database.CreateContext();
            Assert.Equal("Tenant one local value", (await tenantOne.Settings.SingleAsync()).ApplicationName);
            Assert.Equal(["de", "en", "es", "fr", "it"],
                await tenantOne.Languages.OrderBy(language => language.Code)
                    .Select(language => language.Code).ToListAsync());
            tenantContext.SetCurrentTenant(2, "ShootingIta");
            using var tenantTwo = database.CreateContext();
            Assert.Equal("Tenant two remote value", (await tenantTwo.Settings.SingleAsync()).ApplicationName);
            Assert.Equal("en", (await tenantTwo.Languages.SingleAsync()).Code);
        });
    }

    [Fact]
    public async Task Save_is_remote_first_and_failed_remote_write_leaves_sqlite_unchanged()
    {
        await RunWithDatabaseAsync(async (database, tenantContext) =>
        {
            var factory = new FakeApiServiceFactory("settings-missing");
            var sync = CreateService(database, tenantContext, factory);
            await sync.SynchronizeAsync("channel-1");
            var snapshot = await sync.GetLocalSnapshotAsync();
            var updated = snapshot with { ApplicationName = "Saved remotely" };
            await sync.SaveAsync("channel-1", updated);
            Assert.Equal(2, factory.PutSettingsRequestCount);
            using (var context = database.CreateContext())
                Assert.Equal("Saved remotely", (await context.Settings.SingleAsync()).ApplicationName);

            factory.SetScenario("settings-transient");
            var before = (await sync.GetLocalSnapshotAsync()).ApplicationName;
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                sync.SaveAsync("channel-1", updated with { ApplicationName = "Not committed" }));
            using var unchanged = database.CreateContext();
            Assert.Equal(before, (await unchanged.Settings.SingleAsync()).ApplicationName);
            Assert.Equal(3, factory.PutSettingsRequestCount);
        });
    }

    [Fact]
    public async Task Snapshot_contract_excludes_machine_and_secret_fields()
    {
        await RunWithDatabaseAsync(async (database, tenantContext) =>
        {
            var factory = new FakeApiServiceFactory("settings-missing");
            var sync = CreateService(database, tenantContext, factory);
            var snapshot = await sync.GetLocalSnapshotAsync();
            var serialized = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            using var document = JsonDocument.Parse(serialized);
            var propertyNames = GetPropertyNames(document.RootElement).ToArray();
            var forbiddenNames = new[]
            {
                "apiEndpoint", "apiKey", "channelId", "tenantId", "id",
                "token", "path", "credential", "history"
            };
            foreach (var forbiddenName in forbiddenNames)
                Assert.DoesNotContain(propertyNames, name =>
                    string.Equals(name, forbiddenName, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static IEnumerable<string> GetPropertyNames(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                yield return property.Name;
                foreach (var childName in GetPropertyNames(property.Value))
                    yield return childName;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            foreach (var childName in GetPropertyNames(item))
                yield return childName;
        }
    }

    private static ChannelConfigurationSyncService CreateService(
        DatabaseService database,
        TestTenantContext tenantContext,
        FakeApiServiceFactory factory) =>
        new(
            database,
            tenantContext,
            new ApiSettings
            {
                ApiEndpoint = "https://backoffice.test/",
                ApiKey = "test-key",
                ChannelId = "channel-1"
            },
            factory,
            NullLogger<ChannelConfigurationSyncService>.Instance);

    private static async Task RunWithDatabaseAsync(
        Func<DatabaseService, TestTenantContext, Task> action)
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var directory = Path.Combine(Path.GetTempPath(), $"importer-channel-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var tenantContext = new TestTenantContext();
        try
        {
            Directory.SetCurrentDirectory(directory);
            var database = new DatabaseService(tenantContext);
            using (var context = database.CreateContext())
            {
                await context.Database.EnsureCreatedAsync();
                var tenant = await context.Tenants.SingleAsync(item => item.Id == 1);
                tenant.ChannelId = "channel-1";
                await context.SaveChangesAsync();
            }

            await action(database, tenantContext);
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
        public int CurrentTenantId { get; private set; } = 1;
        public string CurrentTenantName { get; private set; } = "MorWalPiz";
        public event EventHandler<TenantChangedEventArgs>? TenantChanged;

        public void SetCurrentTenant(int tenantId, string tenantName)
        {
            if (CurrentTenantId == tenantId)
                return;
            CurrentTenantId = tenantId;
            CurrentTenantName = tenantName;
            TenantChanged?.Invoke(this, new TenantChangedEventArgs(tenantId, tenantName));
        }
    }
}
