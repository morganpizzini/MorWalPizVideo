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
        private readonly IChannelConfigurationSyncService _channelSettingsSyncService;

        public SettingsPage()
        {
            InitializeComponent();
            _databaseService = App.DatabaseService;
            _channelSettingsSyncService = App.ChannelSettingsSyncService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            using var context = _databaseService.CreateContext();
            var settings = context.Settings.FirstOrDefault() ?? new Settings();
            HashtagsTextBox.Text = settings.DefaultHashtags;
            ApiEndpointTextBox.Text = settings.ApiEndpoint;
            ApiKeyTextBox.Text = settings.ApiKey;
            ApplicationNameTextBox.Text = settings.ApplicationName;

            var channelId = App.GetCurrentChannelId();
            var syncStatus = _channelSettingsSyncService.CurrentStatus;
            ChannelSettingsStatusTextBlock.Text = syncStatus.Message;
            SaveButton.IsEnabled = _channelSettingsSyncService.CanAttemptSaveFor(channelId);
            if (!SaveButton.IsEnabled)
            {
                ChannelSettingsStatusTextBlock.Text =
                    "Le impostazioni del canale sono in sola lettura finché la sincronizzazione non è disponibile.";
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SetEditingEnabled(false);
            ChannelSettingsStatusTextBlock.Text = "Salvataggio delle impostazioni del canale...";
            try
            {
                var channelId = App.GetCurrentChannelId();
                if (!_channelSettingsSyncService.CanAttemptSaveFor(channelId))
                    throw new InvalidOperationException("Sincronizza il canale prima di modificare le impostazioni.");

                var snapshot = await _channelSettingsSyncService.GetLocalSnapshotAsync(App.ApplicationStoppingToken);
                snapshot = snapshot with
                {
                    DefaultHashtags = HashtagsTextBox.Text.Trim(),
                    ApplicationName = ApplicationNameTextBox.Text.Trim()
                };
                var result = await _channelSettingsSyncService.SaveAsync(
                    channelId,
                    snapshot,
                    App.ApplicationStoppingToken);
                ChannelSettingsStatusTextBlock.Text = result.Message;
                if (!result.LocalCacheSynchronized)
                {
                    System.Windows.MessageBox.Show(
                        result.Message,
                        "Cache locale non sincronizzata",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                System.Windows.MessageBox.Show("Impostazioni salvate con successo!", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (OperationCanceledException) when (App.ApplicationStoppingToken.IsCancellationRequested)
            {
                ChannelSettingsStatusTextBlock.Text = "Salvataggio annullato durante la chiusura dell'applicazione.";
            }
            catch (Exception ex)
            {
                ChannelSettingsStatusTextBlock.Text =
                    "Salvataggio non riuscito. I valori modificati sono ancora disponibili per un nuovo tentativo.";
                System.Diagnostics.Trace.TraceError("Channel settings UI save failed: {0}", ex);
                System.Windows.MessageBox.Show(
                    "Il server non ha confermato il salvataggio. La cache locale non è stata modificata; riprova quando la connessione è disponibile.",
                    "Salvataggio non riuscito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                SaveButton.Focus();
            }
            finally
            {
                SetEditingEnabled(true);
                SaveButton.IsEnabled = _channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId());
            }
        }

        private void SaveConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ApiEndpointTextBox.Text) ||
                !Uri.TryCreate(ApiEndpointTextBox.Text.Trim(), UriKind.Absolute, out var endpoint) ||
                endpoint.Scheme is not ("http" or "https"))
            {
                LocalConnectionStatusTextBlock.Text = "Inserisci un endpoint API HTTP o HTTPS valido.";
                ApiEndpointTextBox.Focus();
                return;
            }

            try
            {
                using var context = _databaseService.CreateContext();
                var settings = context.Settings.FirstOrDefault() ?? new Settings();
                settings.ApiEndpoint = endpoint.ToString();
                settings.ApiKey = ApiKeyTextBox.Text.Trim();
                if (settings.Id == 0)
                    context.Settings.Add(settings);
                context.SaveChanges();

                App.ApiSettings.ApiEndpoint = endpoint.ToString();
                App.ApiSettings.ApiKey = settings.ApiKey ?? string.Empty;
                LocalConnectionStatusTextBlock.Text = "Configurazione locale della connessione salvata.";
            }
            catch (Exception ex)
            {
                LocalConnectionStatusTextBlock.Text = "Configurazione locale non salvata. Correggi il problema e riprova.";
                System.Diagnostics.Trace.TraceError("Local BackOffice connection settings save failed: {0}", ex);
                System.Windows.MessageBox.Show(
                    "Impossibile salvare la configurazione locale della connessione. Verifica i valori e riprova.",
                    "Salvataggio non riuscito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SetEditingEnabled(bool enabled)
        {
            HashtagsTextBox.IsEnabled = enabled;
            ApplicationNameTextBox.IsEnabled = enabled;
            SaveButton.IsEnabled = enabled;
            CancelButton.IsEnabled = enabled;
            ApiEndpointTextBox.IsEnabled = enabled;
            ApiKeyTextBox.IsEnabled = enabled;
            SaveConnectionButton.IsEnabled = enabled;
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
