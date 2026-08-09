using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorWalPiz.InsightScanner.Models;
using MorWalPiz.InsightScanner.Services;

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
            _host = new HostBuilder()
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddUserSecrets<App>()
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    var settings = new ScannerAppSettings();
                    context.Configuration.GetSection("BackOffice").Bind(settings);
                    context.Configuration.GetSection("Scanner").Bind(settings);
                    services.AddSingleton(settings);
                    services.AddHttpClient<IBackOfficeInsightClient, BackOfficeInsightClient>(client =>
                    {
                        client.BaseAddress = new Uri(settings.ApiEndpoint);
                        client.Timeout = TimeSpan.FromSeconds(100);
                        if (!string.IsNullOrEmpty(settings.ApiKey))
                        {
                            client.DefaultRequestHeaders.Add("X-API-Key", settings.ApiKey);
                        }
                        if (!string.IsNullOrWhiteSpace(settings.ChannelId))
                        {
                            client.DefaultRequestHeaders.Add("X-Channel-Id", settings.ChannelId);
                        }
                    });
                    services.AddSingleton<HybridInsightScanner>(_ =>
                        new HybridInsightScanner([new LightFetchSourceScanStrategy()]));
                })
                .Build();

            await _host.StartAsync();
            Settings = _host.Services.GetRequiredService<ScannerAppSettings>();
            BackOfficeClient = _host.Services.GetRequiredService<IBackOfficeInsightClient>();
            Scanner = _host.Services.GetRequiredService<HybridInsightScanner>();

            base.OnStartup(e);
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
    }
}
