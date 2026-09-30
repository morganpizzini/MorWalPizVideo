using System.Net;
using System.Net.Http;
using System.IO;
using System.Text;
using FluentAssertions;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace MorWalPiz.VideoImporter.Tests;

[Collection("Desktop startup")]
public sealed class ImageGenerationTests
{
    [Fact]
    public void Desktop_host_preserves_database_switches_tenants_and_stops()
    {
        RunOnDesktopThread(async () =>
        {
            var originalDirectory = Directory.GetCurrentDirectory();
            var directory = Path.Combine(Path.GetTempPath(), $"importer-startup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            App? application = null;
            MainWindow? window = null;
            var upload = new TestUploadService();
            try
            {
                Directory.SetCurrentDirectory(directory);
                var tenantContext = new TestTenantContext();
                var database = new DatabaseService(tenantContext);
                string[] knownMigrations;
                using (var context = database.CreateContext())
                {
                    Assert.Equal(directory, Path.GetDirectoryName(Path.GetFullPath(context.Database.GetDbConnection().DataSource)));
                    context.Database.EnsureCreated();
                    knownMigrations = context.Database.GetMigrations().ToArray();
                    foreach (var tenant in context.Tenants)
                        tenant.ChannelId = $"channel-{tenant.Id}";
                    context.Settings.Single().ApplicationName = "existing local data";
                    context.SaveChanges();
                }

                var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DOTNET_ENVIRONMENT"] = "Test",
                    ["UseFake"] = "true",
                    ["ApiEndpoint"] = "https://backoffice.test/",
                    ["ApiKey"] = "test-api-key",
                    ["ChannelId"] = "configured-channel",
                    ["credentials-morwalpiz"] = "test-credentials"
                }).Build();
                DispatcherOperation? startupOperation = null;
                void CaptureStartup(object? sender, DispatcherHookEventArgs args) => startupOperation = args.Operation;
                var dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.Hooks.OperationPosted += CaptureStartup;
                try
                {
                    application = new App { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
                }
                finally
                {
                    dispatcher.Hooks.OperationPosted -= CaptureStartup;
                }
                Assert.NotNull(startupOperation);
                Assert.True(startupOperation.Abort());
                application.InitializeComponent();
                Assert.NotNull(application.Resources["BackgroundBrush"]);
                var host = App.CreateHost(configuration, services =>
                {
                    services.AddSingleton<ITenantContext>(_ => tenantContext);
                    services.AddSingleton<IYouTubeUploadService>(_ => upload);
                });
                window = await application.StartHostAsync(host, service =>
                {
                    using var context = service.CreateContext();
                    Assert.False(context.Database.EnsureCreated());
                    Assert.Equal("existing local data", context.Settings.Single().ApplicationName);
                });
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                Assert.Same(window, host.Services.GetRequiredService<MainWindow>());
                Assert.IsType<FakeApiServiceFactory>(host.Services.GetRequiredService<IApiServiceFactory>());
                Assert.Equal("https://backoffice.test/", App.ApiSettings.ApiEndpoint);
                Assert.Equal("test-api-key", App.ApiSettings.ApiKey);
                Assert.Equal("configured-channel", App.ApiSettings.ChannelId);
                Assert.Equal(0, upload.ValidationCount);
                Assert.Equal(1, tenantContext.SubscriberCount);
                Assert.False(window.IsVisible);
                Assert.Same(application.Resources["BackgroundBrush"], window.Background);
                Assert.False(window.WindowCancellationToken.IsCancellationRequested);
                using (var context = database.CreateContext())
                {
                    Assert.Equal(knownMigrations, context.Database.GetMigrations());
                    Assert.Equal("existing local data", context.Settings.Single().ApplicationName);
                }

                await window.InitializeAsync();
                await window.InitializeAsync();
                Assert.Equal(1, upload.ValidationCount);
                var selector = (ComboBox)window.FindName("TenantComboBox");
                window.VideoFiles.Add(new VideoFile());
                selector.SelectedItem = selector.Items.Cast<Tenant>().Single(tenant => tenant.Id == 2);
                Assert.Equal(2, tenantContext.CurrentTenantId);
                Assert.Equal("Video Importer - ShootingIta", window.Title);
                Assert.Empty(window.VideoFiles);
                Assert.Equal(["ShootingIta"], upload.RefreshedTenants);
                Assert.Equal("test-credentials", upload.LastCredentials);
                Assert.Equal("channel-2", App.GetCurrentChannelId());
                using (var context = database.CreateContext())
                {
                    Assert.Equal(2, context.Settings.Single().TenantId);
                    Assert.Empty(context.Languages);
                }

                upload.PendingRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                selector.SelectedItem = selector.Items.Cast<Tenant>().Single(tenant => tenant.Id == 1);
                var cancellationToken = window.WindowCancellationToken;
                var stopTask = application.StopHostAsync();
                Assert.True(cancellationToken.IsCancellationRequested);
                Assert.Equal(0, tenantContext.SubscriberCount);
                Assert.False(stopTask.IsCompleted);
                upload.PendingRefresh.SetResult();
                await stopTask;
                Assert.True(lifetime.ApplicationStopped.IsCancellationRequested);
                Assert.True(upload.IsDisposed);
                var refreshCount = upload.RefreshedTenants.Count;
                tenantContext.SetCurrentTenant(2, "ShootingIta");
                Assert.Equal(refreshCount, upload.RefreshedTenants.Count);
                window.Close();
                Assert.True(cancellationToken.IsCancellationRequested);
                window = null;

                using var reopened = database.CreateContext();
                Assert.Equal(2, reopened.Settings.Single().TenantId);
                Assert.Equal(knownMigrations, reopened.Database.GetMigrations());
            }
            finally
            {
                upload.PendingRefresh?.TrySetResult();
                window?.Close();
                if (application is not null)
                {
                    await application.StopHostAsync();
                    application.Shutdown();
                }
                Directory.SetCurrentDirectory(originalDirectory);
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        });
    }

    [Fact]
    public void Desktop_database_migration_compatibility()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var directory = Path.Combine(Path.GetTempPath(), $"importer-migrations-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            Directory.SetCurrentDirectory(directory);
            var database = new DatabaseService(new TestTenantContext());
            database.InitializeDatabase();
            using (var context = database.CreateContext())
            {
                context.Settings.Single().ApplicationName = "existing migrated data";
                context.SaveChanges();
            }
            database.InitializeDatabase();
            using var reopened = database.CreateContext();
            Assert.Empty(reopened.Database.GetPendingMigrations());
            Assert.Equal("existing migrated data", reopened.Settings.Single().ApplicationName);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Desktop_host_rejects_production_fake_mode()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DOTNET_ENVIRONMENT"] = "Production",
            ["UseFake"] = "true"
        }).Build();
        Assert.Throws<InvalidOperationException>(() => App.CreateHost(configuration));
    }

    [Fact]
    public async Task GenerateAsync_sends_configured_json_and_bearer_key()
    {
        var handler = new RecordingHandler(_ => JsonResponse("{\"data\":[{\"b64_json\":\"aGVsbG8=\"}] }"));
        var service = CreateService(handler);

        var result = await service.GenerateAsync(new("a red fox", "1024x1024", "low", "opaque", "", "png", 1), CancellationToken.None);

        result.Single().Content.Should().Equal(Encoding.UTF8.GetBytes("hello"));
        handler.Request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Request.Headers.Authorization.Parameter.Should().Be("secret");
        handler.Body.Should().Contain("\"prompt\":\"a red fox\"");
        handler.Body.Should().Contain("\"output_format\":\"png\"");
    }

    [Fact]
    public async Task EditAsync_sends_one_image_and_optional_mask_as_multipart()
    {
        var source = Path.GetTempFileName();
        var mask = Path.GetTempFileName();
        await File.WriteAllBytesAsync(source, [1, 2]);
        await File.WriteAllBytesAsync(mask, [3, 4]);
        try
        {
            var handler = new RecordingHandler(_ => JsonResponse("{\"data\":[{\"b64_json\":\"aA==\"}]}"));
            await CreateService(handler).EditAsync(new("edit", source, mask), CancellationToken.None);
            handler.ContentType.Should().StartWith("multipart/form-data");
            handler.Body.Should().Contain("name=prompt");
            handler.Body.Should().Contain("name=image");
            handler.Body.Should().Contain("name=mask");
        }
        finally { File.Delete(source); File.Delete(mask); }
    }

    [Fact]
    public void Size_mapping_rejects_unconfigured_dimensions()
    {
        var options = new ImageGenerationOptions { SupportedSizes = new(StringComparer.OrdinalIgnoreCase) { ["16:9"] = "1536x864" } };
        ImageGenerationValidation.ResolveProviderSize(options, "16:9").Should().Be("1536x864");
        var action = () => ImageGenerationValidation.ResolveProviderSize(options, "4:3");
        action.Should().Throw<ImageProviderException>().WithMessage("*non è supportato*");
    }

    [Fact]
    public async Task Output_names_are_collision_free()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var output = new ImageOutputService();
            var image = new GeneratedImage([1], "png", "image/png");
            var first = await output.SaveAsync([image], directory, "same name", CancellationToken.None);
            var second = await output.SaveAsync([image], directory, "same name", CancellationToken.None);
            first.Single().Should().NotBe(second.Single());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void Templates_persist_and_replace_by_name()
    {
        var path = Path.Combine(Path.GetTempPath(), $"templates-{Guid.NewGuid():N}.json");
        try
        {
            var store = new PromptTemplateStore(path);
            store.Save(new("Fox", "one"));
            store.Save(new("Fox", "two"));
            new PromptTemplateStore(path).GetAll().Should().ContainSingle().Which.Prompt.Should().Be("two");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Provider_errors_include_status_code()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad request") });
        var action = () => CreateService(handler).GenerateAsync(new("p", "1024x1024", "auto", "auto", "", "png", 1), CancellationToken.None);
        await action.Should().ThrowAsync<ImageProviderException>().Where(exception => exception.StatusCode == 400);
    }

    private static ImageGenerationService CreateService(RecordingHandler handler) => new(new TestHttpClientFactory(handler), new TestKeyProvider(), new ImageGenerationOptions { Endpoint = "https://provider.test/images", Model = "test", OutputFormat = "png" });
    private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static void RunOnDesktopThread(Func<Task> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.InvokeAsync(async () =>
            {
                try { await action(); }
                catch (Exception exception) { failure = exception; }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
            });
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "Desktop startup test did not complete.");
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public int CurrentTenantId { get; private set; } = 1;
        public string CurrentTenantName { get; private set; } = "MorWalPiz";
        public event EventHandler<TenantChangedEventArgs>? TenantChanged;
        public int SubscriberCount => TenantChanged?.GetInvocationList().Length ?? 0;

        public void SetCurrentTenant(int tenantId, string tenantName)
        {
            if (CurrentTenantId == tenantId) return;
            CurrentTenantId = tenantId;
            CurrentTenantName = tenantName;
            TenantChanged?.Invoke(this, new TenantChangedEventArgs(tenantId, tenantName));
        }
    }

    private sealed class TestUploadService : IYouTubeUploadService, IDisposable
    {
        public int ValidationCount { get; private set; }
        public List<string> RefreshedTenants { get; } = [];
        public string? LastCredentials { get; private set; }
        public TaskCompletionSource? PendingRefresh { get; set; }
        public bool IsDisposed { get; private set; }
        public Task<bool> ValidateCredentialsAsync() { ValidationCount++; return Task.FromResult(true); }
        public Task ReinitializeWithNewCredentialsAsync(string credentials, string tenantName)
        {
            LastCredentials = credentials;
            RefreshedTenants.Add(tenantName);
            return PendingRefresh?.Task ?? Task.CompletedTask;
        }
        public Task<bool> ReinitializeServiceAsync() => Task.FromResult(true);
        public Task<bool> ForceReauthenticationAsync() => Task.FromResult(true);
        public bool ClearStoredCredentials() => true;
        public Task<VideoLocalizationUpdateResult> AutoTranslateVideoAsync(string youtubeVideoId) => throw new NotSupportedException();
        public Task<IEnumerable<Services.UploadResult>> UploadVideosAsync(IEnumerable<VideoFile> videos, IList<string> tags,
            Action<UploadProgressInfo>? progressCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Dispose() => IsDisposed = true;
    }

    private sealed class TestKeyProvider : IImageApiKeyProvider
    {
        public Task<string> GetApiKeyAsync(CancellationToken cancellationToken) => Task.FromResult("secret");
    }

    private sealed class TestHttpClientFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage Request { get; private set; } = null!;
        public string Body { get; private set; } = string.Empty;
        public string ContentType { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            ContentType = request.Content?.Headers.ContentType?.ToString() ?? string.Empty;
            return responseFactory(request);
        }
    }
}

[CollectionDefinition("Desktop startup", DisableParallelization = true)]
public sealed class DesktopStartupCollection;