using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Options;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static DatabaseService DatabaseService { get; private set; }
        public static ApiSettings ApiSettings { get; private set; }
        public static IYouTubeUploadService YouTubeUploadService { get; private set; }
        public static ITenantContext TenantContext { get; private set; }
        public static ITenantService TenantService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inizializza il contesto tenant
            TenantContext = new TenantContext();

            // Inizializza il servizio del database
            DatabaseService = new DatabaseService(TenantContext);
            DatabaseService.InitializeDatabase();

            // Inizializza il servizio tenant
            TenantService = new TenantService(DatabaseService);

            // Inizializza le impostazioni API
            ApiSettings = new ApiSettings();

            // Lettura endpoint API dalle impostazioni
            using (var context = DatabaseService.CreateContext())
            {
                var settings = context.Settings.FirstOrDefault();
                if (settings != null && !string.IsNullOrEmpty(settings.ApiEndpoint))
                {
                    ApiSettings.ApiEndpoint = settings.ApiEndpoint;
                }
            }

            // Inizializza il servizio di upload YouTube
            string credentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "credentials.json");
            //YouTubeUploadService = new YouTubeUploadService(credentialsPath);
        }
    }
}
