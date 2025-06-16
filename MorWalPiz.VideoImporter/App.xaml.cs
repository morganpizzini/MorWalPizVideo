using System;
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
            string credentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"credentials-{TenantContext.CurrentTenantName.ToLower()}.json");
            YouTubeUploadService = new YouTubeUploadService(credentialsPath);

            // Sottoscrivi al cambio di tenant per reinizializzare YouTube service
            TenantContext.TenantChanged += OnTenantChanged;
        }

        /// <summary>
        /// Gestisce il cambio di tenant reinizializzando il servizio YouTube con le nuove credenziali
        /// </summary>
        private void OnTenantChanged(object sender, TenantChangedEventArgs e)
        {
            try
            {
                // Costruisci il nuovo percorso delle credenziali basato sul nome del tenant
                string newCredentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"credentials-{e.TenantName.ToLower()}.json");
                
                // Reinizializza il servizio YouTube con le nuove credenziali
                YouTubeUploadService.ReinitializeWithNewCredentials(newCredentialsPath);
            }
            catch (Exception ex)
            {
                // Log dell'errore ma non interrompere l'applicazione
                System.Diagnostics.Debug.WriteLine($"Errore nella reinizializzazione del servizio YouTube per il tenant {e.TenantName}: {ex.Message}");
            }
        }
    }
}
