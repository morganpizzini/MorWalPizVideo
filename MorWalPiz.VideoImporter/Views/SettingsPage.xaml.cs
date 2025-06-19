using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Window
    {
        private readonly DatabaseService _databaseService;
        private Settings _currentSettings;
        public static ITenantContext TenantContext { get; private set; }

        public SettingsPage()
        {
            InitializeComponent();
            // Inizializza il contesto tenant
            TenantContext = new TenantContext();
            _databaseService = new DatabaseService(TenantContext);
            LoadSettings();
        }

        private void LoadSettings()
        {
            using (var context = _databaseService.CreateContext())
            {
                _currentSettings = context.Settings.FirstOrDefault() ?? new Settings();

                // Popola la TextBox degli hashtag
                HashtagsTextBox.Text = _currentSettings.DefaultHashtags;

                // Popola il campo API Endpoint
                ApiEndpointTextBox.Text = _currentSettings.ApiEndpoint;
                ApplicationNameTextBox.Text = _currentSettings.ApplicationName;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validazione dell'endpoint API
                if (string.IsNullOrWhiteSpace(ApiEndpointTextBox.Text))
                {
                    System.Windows.MessageBox.Show("L'endpoint API non può essere vuoto.", "Errore di validazione", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var apiEndpoint = ApiEndpointTextBox.Text.Trim();
                var applicationName = ApplicationNameTextBox.Text.Trim();
                // Salvataggio delle impostazioni
                using (var context = _databaseService.CreateContext())
                {
                    var settings = context.Settings.FirstOrDefault() ?? new Settings();

                    settings.DefaultHashtags = HashtagsTextBox.Text.Trim();
                    settings.ApiEndpoint = apiEndpoint;
                    settings.ApplicationName = applicationName;

                    if (settings.Id == 0)
                    {
                        context.Settings.Add(settings);
                    }
                    else
                    {
                        context.Settings.Update(settings);
                    }

                    context.SaveChanges();
                }

                //refresh app settings
                App.ApiSettings.ApiEndpoint = apiEndpoint;

                System.Windows.MessageBox.Show("Impostazioni salvate con successo!", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Errore durante il salvataggio: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Input validation per i campi numerici
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text);
        }

        private bool IsNumeric(string text)
        {
            return int.TryParse(text, out _);
        }
    }
}
