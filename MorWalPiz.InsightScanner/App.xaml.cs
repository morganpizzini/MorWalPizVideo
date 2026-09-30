using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorWalPiz.InsightScanner.Models;
using MorWalPiz.InsightScanner.Services;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MorWalPiz.InsightScanner.Tests")]

namespace MorWalPiz.InsightScanner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static ScannerAppSettings Settings { get; private set; } = new();
        public static IBackOfficeInsightClient BackOfficeClient { get; private set; } = null!;
        public static HybridInsightScanner Scanner { get; private set; } = null!;
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<App>()
                .AddEnvironmentVariables()
                .Build();
            _host = CreateHost(configuration);
            await _host.StartAsync();
            Settings = _host.Services.GetRequiredService<ScannerAppSettings>();
            BackOfficeClient = _host.Services.GetRequiredService<IBackOfficeInsightClient>();
            Scanner = _host.Services.GetRequiredService<HybridInsightScanner>();

            base.OnStartup(e);
            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow.Show();
        }

        internal static IHost CreateHost(IConfiguration configuration)
            => new HostBuilder()
                .ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration))
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<MainWindow>();
                    var settings = new ScannerAppSettings();
                    context.Configuration.GetSection("BackOffice").Bind(settings);
                    context.Configuration.GetSection("Scanner").Bind(settings);
                    if (settings.UseFake && !IsTestOrDevelopment(context.Configuration))
                        throw new InvalidOperationException("InsightScanner fake mode is allowed only in Development or Test.");
                    services.AddSingleton(settings);
                    if (settings.UseFake)
                    {
                        services.AddSingleton<IBackOfficeInsightClient, FakeBackOfficeInsightClient>();
                        services.AddSingleton<HybridInsightScanner>(provider =>
                            new HybridInsightScanner([new FakeSourceScanStrategy(provider.GetRequiredService<ScannerAppSettings>())]));
                    }
                    else
                    {
                        services.AddHttpClient<IBackOfficeInsightClient, BackOfficeInsightClient>(client =>
                        {
                            client.BaseAddress = new Uri(settings.ApiEndpoint);
                            client.Timeout = TimeSpan.FromSeconds(100);
                            if (!string.IsNullOrEmpty(settings.ApiKey))
                                client.DefaultRequestHeaders.Add("X-API-Key", settings.ApiKey);
                            if (!string.IsNullOrWhiteSpace(settings.ChannelId))
                                client.DefaultRequestHeaders.Add("X-Channel-Id", settings.ChannelId);
                        });
                        services.AddSingleton<HybridInsightScanner>(_ =>
                            new HybridInsightScanner([new LightFetchSourceScanStrategy()]));
                    }
                })
                .Build();

        private static bool IsTestOrDevelopment(IConfiguration configuration)
            => configuration["DOTNET_ENVIRONMENT"] is "Development" or "Test" ||
               configuration["ASPNETCORE_ENVIRONMENT"] is "Development" or "Test";

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host is not null)
            {
                try
                {
                    await _host.StopAsync();
                }
                finally
                {
                    _host.Dispose();
                }
            }

            base.OnExit(e);
        }
    }
}
