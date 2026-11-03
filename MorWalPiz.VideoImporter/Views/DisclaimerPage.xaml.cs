using System.Windows;
using System.Windows.Controls;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter.Views
{
    public partial class DisclaimerPage : Window
    {
        private readonly AppDbContext _context;
        private readonly IChannelConfigurationSyncService _channelSettingsSyncService;
        private List<Language> _selectedLanguages;
        private Dictionary<int, Disclaimer> _disclaimers = new Dictionary<int, Disclaimer>();
        private bool _isInitializing = true; // Flag per tracciare l'inizializzazione

        public DisclaimerPage()
        {
            InitializeComponent();
            _context = App.DatabaseService.CreateContext();
            _channelSettingsSyncService = App.ChannelSettingsSyncService;
            LoadData();
            SaveButton.IsEnabled = _channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId());
            StatusTextBlock.Text = _channelSettingsSyncService.CurrentStatus.Message;
        }

        private void LoadData()
        {
            // Carica solo le lingue selezionate (predefinita + secondarie selezionate)
            _selectedLanguages = _context.Languages
                .Where(l => l.IsDefault || l.IsSelected)
                .ToList();

            // Carica i disclaimer esistenti e li inserisce nel dictionary per l'accesso rapido
            var existingDisclaimers = _context.Disclaimers
                .Where(d => _selectedLanguages.Select(l => l.Id).Contains(d.LanguageId))
                .ToList();

            foreach (var disclaimer in existingDisclaimers)
            {
                _disclaimers[disclaimer.LanguageId] = disclaimer;
            }

            // Imposta la fonte dati per il selettore di lingua
            LanguageSelector.ItemsSource = _selectedLanguages;

            // Seleziona la lingua predefinita
            var defaultLanguage = _selectedLanguages.FirstOrDefault(l => l.IsDefault);
            if (defaultLanguage != null)
            {
                LanguageSelector.SelectedItem = defaultLanguage;
            }
            
            // Carica il testo del disclaimer per la lingua predefinita
            if (defaultLanguage != null)
            {
                LoadDisclaimerText(defaultLanguage.Id);
            }
            
            // Imposta il flag a false dopo l'inizializzazione completa
            _isInitializing = false;
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ignora l'evento se siamo nella fase di inizializzazione
            if (_isInitializing)
                return;
                
            if (LanguageSelector.SelectedItem != null)
            {
                // Carica il testo per la lingua selezionata
                var selectedLanguage = (Language)LanguageSelector.SelectedItem;
                LoadDisclaimerText(selectedLanguage.Id);
            }
        }

        private void LoadDisclaimerText(int languageId)
        {
            // Verifica se esiste già un disclaimer per questa lingua
            if (_disclaimers.TryGetValue(languageId, out var disclaimer))
            {
                DisclaimerTextBox.Text = disclaimer.Text;
            }
            else
            {
                DisclaimerTextBox.Text = string.Empty;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (LanguageSelector.SelectedItem is not Language currentLanguage ||
                !_channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId()))
                return;

            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            StatusTextBlock.Text = "Salvataggio del disclaimer...";
            try
            {
                var channelId = App.GetCurrentChannelId();
                var snapshot = await _channelSettingsSyncService.GetLocalSnapshotAsync(App.ApplicationStoppingToken);
                var disclaimerText = DisclaimerTextBox.Text?.Trim() ?? string.Empty;
                var languages = snapshot.Languages.Select(language =>
                    string.Equals(language.Code, currentLanguage.Code, StringComparison.OrdinalIgnoreCase)
                        ? language with { DisclaimerText = string.IsNullOrEmpty(disclaimerText) ? null : disclaimerText }
                        : language).ToArray();
                var result = await _channelSettingsSyncService.SaveAsync(
                    channelId,
                    snapshot with { Languages = languages },
                    App.ApplicationStoppingToken);
                StatusTextBlock.Text = result.Message;
                if (!result.LocalCacheSynchronized)
                {
                    System.Windows.MessageBox.Show(result.Message, "Cache locale non sincronizzata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(disclaimerText))
                    _disclaimers.Remove(currentLanguage.Id);
                else
                    _disclaimers[currentLanguage.Id] = new Disclaimer
                    {
                        LanguageId = currentLanguage.Id,
                        Text = disclaimerText
                    };
                System.Windows.MessageBox.Show("Disclaimer salvato con successo!", "Salvataggio Completato", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException) when (App.ApplicationStoppingToken.IsCancellationRequested)
            {
                StatusTextBlock.Text = "Salvataggio annullato durante la chiusura dell'applicazione.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Salvataggio non riuscito. Il testo resta disponibile per un nuovo tentativo.";
                System.Diagnostics.Trace.TraceError("Disclaimer settings UI save failed: {0}", ex);
                System.Windows.MessageBox.Show(
                    "Il server non ha confermato il salvataggio. Il testo resta disponibile per un nuovo tentativo.",
                    "Salvataggio non riuscito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                DisclaimerTextBox.Focus();
            }
            finally
            {
                SaveButton.IsEnabled = _channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId());
                CancelButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context.Dispose();
            base.OnClosed(e);
        }
    }
}