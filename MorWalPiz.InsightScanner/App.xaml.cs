using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<App>()
                .AddEnvironmentVariables()
                .Build();

            Settings = new ScannerAppSettings();
            configuration.GetSection("BackOffice").Bind(Settings);
            configuration.GetSection("Scanner").Bind(Settings);

            BackOfficeClient = new BackOfficeInsightClient(Settings.ApiEndpoint, Settings.ApiKey);
            Scanner = new HybridInsightScanner([new LightFetchSourceScanStrategy()]);
        }
    }
}
