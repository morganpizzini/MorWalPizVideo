using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Identity;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static DatabaseService DatabaseService { get; private set; } = null!;
        public static ApiSettings ApiSettings { get; private set; } = null!;
        public static IYouTubeUploadService YouTubeUploadService { get; private set; } = null!;
        public static ITenantContext TenantContext { get; private set; } = null!;
        public static ITenantService TenantService { get; private set; } = null!;
        public static IApiServiceFactory ApiServiceFactory { get; private set; } = null!;
        public static SocialPublishingService SocialPublishingService { get; private set; } = null!;
        public static IHashtagHistoryService HashtagHistoryService { get; private set; } = null!;
        public static IVideoThumbnailService VideoThumbnailService { get; private set; } = null!;
        public static ImageGenerationOptions ImageGenerationOptions { get; private set; } = null!;
        public static IImageGenerationService ImageGenerationService { get; private set; } = null!;
        public static IImageOutputService ImageOutputService { get; private set; } = null!;
        public static IPromptTemplateStore PromptTemplateStore { get; private set; } = null!;
        public static IConfiguration Configuration { get; private set; } = null!;

        public static string GetCurrentChannelId()
        {
            using var context = DatabaseService.CreateContext();
            return context.Tenants.Find(TenantContext.CurrentTenantId)?.ChannelId
                ?? ApiSettings.ChannelId;
        }
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = new HostBuilder()
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddUserSecrets<App>()
                        .AddEnvironmentVariables();

                    var initialConfiguration = configuration.Build();
                    var keyVaultUrl = initialConfiguration["KeyVaultUrl"];
                    if (!string.IsNullOrEmpty(keyVaultUrl))
                    {
                        configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    var useFake = context.Configuration.GetValue<bool>("UseFake");
                    var fakeScenario = context.Configuration["FakeScenario"] ?? "success";
                    var environment = context.Configuration["DOTNET_ENVIRONMENT"] ?? context.Configuration["ASPNETCORE_ENVIRONMENT"];
                    if (useFake && environment is not ("Development" or "Test"))
                        throw new InvalidOperationException("VideoImporter fake mode is allowed only in Development or Test.");
                    services.AddSingleton<ITenantContext, TenantContext>();
                    services.AddSingleton<DatabaseService>();
                    services.AddSingleton<ITenantService, TenantService>();
                    services.AddSingleton(provider => CreateApiSettings(context.Configuration, provider.GetRequiredService<DatabaseService>()));
                    services.AddSingleton(provider => ImageGenerationOptions.FromConfiguration(context.Configuration));
                    services.AddSingleton<IImageApiKeyProvider, ConfigurationImageApiKeyProvider>();
                    services.AddSingleton<IImageGenerationService, ImageGenerationService>();
                    services.AddSingleton<IImageOutputService, ImageOutputService>();
                    services.AddSingleton<IPromptTemplateStore, PromptTemplateStore>();
                    services.AddHttpClient("ImageGeneration", client => client.Timeout = TimeSpan.FromMinutes(10));
                    services.AddHttpClient("BackOffice", client => client.Timeout = TimeSpan.FromSeconds(300))
                        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                        {
                            ConnectTimeout = TimeSpan.FromSeconds(30),
                            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                            KeepAlivePingTimeout = TimeSpan.FromSeconds(20)
                        });
                    if (useFake)
                        services.AddSingleton<IApiServiceFactory>(_ => new FakeApiServiceFactory(fakeScenario));
                    else
                        services.AddSingleton<IApiServiceFactory, ApiServiceFactory>();
                    services.AddHttpClient("SocialGraph", client =>
                    {
                        client.BaseAddress = new Uri("https://graph.facebook.com/v23.0/");
                        client.Timeout = TimeSpan.FromMinutes(5);
                    });
                    services.AddSingleton<IHashtagHistoryService, HashtagHistoryService>();
                    services.AddSingleton<IVideoThumbnailService, WpfVideoThumbnailService>();
                    services.AddSingleton<ISocialProvider>(provider => new OfficialGraphSocialProvider(provider.GetRequiredService<IHttpClientFactory>()) { Provider = SocialProviderKind.FacebookPage });
                    services.AddSingleton<ISocialProvider>(provider => new OfficialGraphSocialProvider(provider.GetRequiredService<IHttpClientFactory>()) { Provider = SocialProviderKind.InstagramBusiness });
                    services.AddSingleton<ISocialProvider>(provider => new OfficialGraphSocialProvider(provider.GetRequiredService<IHttpClientFactory>()) { Provider = SocialProviderKind.Threads });
                    services.AddSingleton<ISocialProvider>(new UnavailableSocialProvider(SocialProviderKind.FacebookPersonalProfile));
                    services.AddSingleton<SocialPublishingService>();
                })
                .Build();

            await _host.StartAsync();
            Configuration = _host.Services.GetRequiredService<IConfiguration>();
            TenantContext = _host.Services.GetRequiredService<ITenantContext>();
            DatabaseService = _host.Services.GetRequiredService<DatabaseService>();
            DatabaseService.InitializeDatabase();
            TenantService = _host.Services.GetRequiredService<ITenantService>();
            ApiSettings = _host.Services.GetRequiredService<ApiSettings>();
            ApiServiceFactory = _host.Services.GetRequiredService<IApiServiceFactory>();
            SocialPublishingService = _host.Services.GetRequiredService<SocialPublishingService>();
            HashtagHistoryService = _host.Services.GetRequiredService<IHashtagHistoryService>();
            VideoThumbnailService = _host.Services.GetRequiredService<IVideoThumbnailService>();
            ImageGenerationOptions = _host.Services.GetRequiredService<ImageGenerationOptions>();
            ImageGenerationService = _host.Services.GetRequiredService<IImageGenerationService>();
            ImageOutputService = _host.Services.GetRequiredService<IImageOutputService>();
            PromptTemplateStore = _host.Services.GetRequiredService<IPromptTemplateStore>();

            // Inizializza il servizio di upload YouTube con Key Vault
            //var credentials = Configuration[$"credentials-{TenantContext.CurrentTenantName.ToLower()}"];
            var credentials = Configuration["credentials-morwalpiz"];
            if (string.IsNullOrEmpty(credentials))
            {
                throw new InvalidOperationException($"YouTube credentials for tenant '{TenantContext.CurrentTenantName}' are not configured in Key Vault.");
            }

            YouTubeUploadService = new YouTubeUploadService(credentials, TenantContext.CurrentTenantName);

            // Sottoscrivi al cambio di tenant per reinizializzare YouTube service
            TenantContext.TenantChanged += OnTenantChanged;
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host is not null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            base.OnExit(e);
        }

        private static ApiSettings CreateApiSettings(IConfiguration configuration, DatabaseService databaseService)
        {
            var configuredApiEndpoint = configuration["ApiEndpoint"] ?? configuration["BackOffice:ApiEndpoint"];
            var configuredApiKey = configuration["ApiKey"] ?? configuration["BackOffice:ApiKey"];
            var configuredChannelId = configuration["ChannelId"] ?? configuration["BackOffice:ChannelId"];
            using var context = databaseService.CreateContext();
            var settings = context.Settings.FirstOrDefault();

            return new ApiSettings
            {
                ApiEndpoint = !string.IsNullOrWhiteSpace(configuredApiEndpoint)
                    ? configuredApiEndpoint
                    : settings?.ApiEndpoint ?? string.Empty,
                ApiKey = !string.IsNullOrWhiteSpace(configuredApiKey)
                    ? configuredApiKey
                    : settings?.ApiKey ?? string.Empty,
                ChannelId = !string.IsNullOrWhiteSpace(configuredChannelId)
                    ? configuredChannelId
                    : settings?.ChannelId ?? string.Empty
            };
        }

        /// <summary>
        /// Gestisce il cambio di tenant reinizializzando il servizio YouTube con le nuove credenziali da Key Vault
        /// </summary>
        private async void OnTenantChanged(object sender, TenantChangedEventArgs e)
        {
            try
            {
                // Inizializza il servizio di upload YouTube con Key Vault
                //var credentials = Configuration[$"credentials-{TenantContext.CurrentTenantName.ToLower()}"];
                var credentials = Configuration["credentials-morwalpiz"];
                if (string.IsNullOrEmpty(credentials))
                {
                    throw new InvalidOperationException($"YouTube credentials for tenant '{TenantContext.CurrentTenantName}' are not configured in Key Vault.");
                }
                // Reinizializza il servizio YouTube con le nuove credenziali dal Key Vault
                await YouTubeUploadService.ReinitializeWithNewCredentialsAsync(credentials, e.TenantName);
            }
            catch (Exception ex)
            {
                // Log dell'errore ma non interrompere l'applicazione
                System.Diagnostics.Debug.WriteLine($"Errore nella reinizializzazione del servizio YouTube per il tenant {e.TenantName}: {ex.Message}");
            }
        }
    }
}
