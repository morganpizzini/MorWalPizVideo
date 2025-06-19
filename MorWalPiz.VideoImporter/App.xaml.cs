using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using System.Threading.Tasks;

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
        public static IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configura la configurazione per leggere da user secrets
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<App>()
                .AddEnvironmentVariables();

            // Build an initial configuration to get the KeyVaultUrl
            var initialConfig = builder.Build();
            
            // Add Azure Key Vault configuration provider if KeyVaultUrl is available
            // This allows Key Vault secrets to be accessed via Configuration["secret-name"]
            // alongside other configuration sources (appsettings.json, user secrets, environment variables)
            string? keyVaultUrl = initialConfig["KeyVaultUrl"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                var credential = new DefaultAzureCredential();
                builder.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
            }

            Configuration = builder.Build();

            // Inizializza il contesto tenant
            TenantContext = new TenantContext();

            // Inizializza il servizio del database
            DatabaseService = new DatabaseService(TenantContext);
            DatabaseService.InitializeDatabase();

            // Inizializza il servizio tenant
            TenantService = new TenantService(DatabaseService);

            // Inizializza le impostazioni API
            ApiSettings = new ApiSettings();

            using (var context = DatabaseService.CreateContext())
            {
                var settings = context.Settings.FirstOrDefault();
                if (settings != null && !string.IsNullOrEmpty(settings.ApiEndpoint))
                {
                    ApiSettings.ApiEndpoint = settings.ApiEndpoint;
                }
            }

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
                await YouTubeUploadService.ReinitializeWithNewCredentialsAsync(credentials,e.TenantName);
            }
            catch (Exception ex)
            {
                // Log dell'errore ma non interrompere l'applicazione
                System.Diagnostics.Debug.WriteLine($"Errore nella reinizializzazione del servizio YouTube per il tenant {e.TenantName}: {ex.Message}");
            }
        }
    }
}
