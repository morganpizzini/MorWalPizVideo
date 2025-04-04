using System.Configuration;
using System.Data;
using System.Windows;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static DatabaseService DatabaseService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inizializza il servizio del database
            DatabaseService = new DatabaseService();
            DatabaseService.InitializeDatabase();
        }
    }
}
