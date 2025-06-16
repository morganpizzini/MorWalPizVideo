using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
// Aggiungi using per AppDbContext e Disclaimer
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Views;
using MorWalPiz.VideoImporter.Services;
using System.Windows.Input;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.MessageBox;
using Cursors = System.Windows.Input.Cursors;


namespace MorWalPiz.VideoImporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>    
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<VideoFile> VideoFiles { get; set; } = new ObservableCollection<VideoFile>();
        private List<string> selectedFolders = new List<string>();
        private List<string> selectedFiles = new List<string>();

        private DateTime _selectedPublishDate = DateTime.Today;

        public DateTime SelectedPublishDate
        {
            get => _selectedPublishDate;
            set
            {
                if (_selectedPublishDate != value)
                {
                    _selectedPublishDate = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _hasSelectedItems;
        public bool HasSelectedItems
        {
            get => _hasSelectedItems;
            set
            {
                if (_hasSelectedItems != value)
                {
                    _hasSelectedItems = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            FileListView.ItemsSource = VideoFiles;
            DataContext = this;
            
            // Subscribe to collection changes
            VideoFiles.CollectionChanged += VideoFiles_CollectionChanged;
            
            // Initialize button states
            UpdateButtonStates();
            
            // Initialize tenant dropdown
            LoadTenants();
        }

        private void VideoFiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (VideoFile item in e.OldItems)
                {
                    item.PropertyChanged -= VideoFile_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (VideoFile item in e.NewItems)
                {
                    item.PropertyChanged += VideoFile_PropertyChanged;
                }
            }

            UpdateButtonStates();
        }

        private void VideoFile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VideoFile.IsSelected))
            {
                UpdateButtonStates();
            }
        }

        private void UpdateButtonStates()
        {
            HasSelectedItems = VideoFiles.Any(f => f.IsSelected);
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Seleziona una cartella contenente file MP4";

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    selectedFolders.Clear();
                    selectedFolders.Add(folderDialog.SelectedPath);
                }
                ProcessFiles();
            }
        }

        private void BrowseFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "File MP4 (*.mp4)|*.mp4",
                Title = "Seleziona file MP4"
            };

            if (fileDialog.ShowDialog() == true)
            {
                selectedFiles.Clear();
                foreach (var file in fileDialog.FileNames)
                {
                    selectedFiles.Add(file);
                }
                ProcessFiles();
            }
        }


        private void ProcessFiles()
        {
            VideoFiles.Clear();

            // Ottieni la lingua predefinita dai settings
            string defaultLanguage;
            using (var context = App.DatabaseService.CreateContext())
            {
                defaultLanguage = context.Languages.FirstOrDefault(l => l.IsDefault)?.Code ?? "it";
            }

            // Conta il numero totale di file per calcolare le date di pubblicazione
            var allFiles = new List<string>();
            allFiles.AddRange(selectedFiles.Where(f => File.Exists(f) && Path.GetExtension(f).ToLower() == ".mp4"));
            
            foreach (var folderPath in selectedFolders)
            {
                if (Directory.Exists(folderPath))
                {
                    var mp4Files = Directory.GetFiles(folderPath, "*.mp4", SearchOption.AllDirectories);
                    allFiles.AddRange(mp4Files);
                }
            }

            // Usa il servizio di pianificazione per ottenere le date e orari di pubblicazione
            var publishScheduleService = new Services.PublishScheduleService(App.DatabaseService);
            var publishDateTimes = publishScheduleService.GetPublishDateTimesForVideos(SelectedPublishDate, allFiles.Count);

            var orderIndex = 1;
            var dateTimeIndex = 0;

            // Process selected individual files
            foreach (var filePath in selectedFiles)
            {
                if (File.Exists(filePath) && Path.GetExtension(filePath).ToLower() == ".mp4")
                {
                    var (publishDate, publishTime) = dateTimeIndex < publishDateTimes.Count 
                        ? publishDateTimes[dateTimeIndex] 
                        : (SelectedPublishDate.AddDays(dateTimeIndex), new TimeSpan(12, 0, 0));

                    VideoFiles.Add(new VideoFile
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        IsSelected = false,
                        PublishDate = publishDate,
                        PublishTime = publishTime,
                        OrderIndex = orderIndex,
                        DefaultLanguage = defaultLanguage,
                    });
                    dateTimeIndex++;
                    orderIndex++;
                }
            }

            // Process files from selected folders
            foreach (var folderPath in selectedFolders)
            {
                if (Directory.Exists(folderPath))
                {
                    var mp4Files = Directory.GetFiles(folderPath, "*.mp4", SearchOption.AllDirectories);
                    foreach (var filePath in mp4Files)
                    {
                        var (publishDate, publishTime) = dateTimeIndex < publishDateTimes.Count 
                            ? publishDateTimes[dateTimeIndex] 
                            : (SelectedPublishDate.AddDays(dateTimeIndex), new TimeSpan(12, 0, 0));

                        VideoFiles.Add(new VideoFile
                        {
                            FileName = Path.GetFileName(filePath),
                            FilePath = filePath,
                            IsSelected = false,
                            PublishDate = publishDate,
                            PublishTime = publishTime,
                            OrderIndex = orderIndex,
                            DefaultLanguage = defaultLanguage
                        });
                        dateTimeIndex++;
                        orderIndex++;
                    }
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = VideoFiles.Where(f => f.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("Nessun file selezionato!", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Sei sicuro di voler rimuovere {selectedItems.Count} file dalla lista?",
                "Conferma eliminazione",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var item in selectedItems.ToList())
                {
                    VideoFiles.Remove(item);
                }
            }
        }

        // Gestione del menu
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LanguagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var languagesPage = new LanguagesPage();
            languagesPage.Owner = this;
            languagesPage.ShowDialog();
        }

        private void DisclaimerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var disclaimerPage = new DisclaimerPage();
            disclaimerPage.Owner = this;
            disclaimerPage.ShowDialog();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsPage = new SettingsPage();
            settingsPage.Owner = this;
            settingsPage.ShowDialog();
        }

        private void PublishSchedulesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var publishSchedulesPage = new Views.PublishSchedulesPage();
            publishSchedulesPage.Owner = this;
            publishSchedulesPage.ShowDialog();
        }

        private void FileDetailButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the video file associated with the clicked button
            if (sender is FrameworkElement element && element.DataContext is VideoFile file)
            {
                ShowFileDetailDialog(file);
            }
        }

        private void FileListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Get the clicked item from the ListView
            if (FileListView.SelectedItem is VideoFile file)
            {
                ShowFileDetailDialog(file);
            }
        }

        private void ShowFileDetailDialog(VideoFile file)
        {
            // Open the file detail dialog
            var detailPage = new Views.FileDetailPage(file);
            detailPage.Owner = this;

            // Show the dialog and process the result
            var result = detailPage.ShowDialog();

            if (result == true)
            {
                // Refresh the ListView to show any changes
                FileListView.Items.Refresh();
            }
        }

        private void ContextButton_Click(object sender, RoutedEventArgs e)
        {
            // Verifica se ci sono file selezionati
            var selectedItems = VideoFiles.Where(f => f.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("Nessun file selezionato!", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Estrai i nomi puliti dei file selezionati
            var selectedFileNames = selectedItems.Select(f => !string.IsNullOrEmpty(f.EditedCleanFileName)
                                                ? f.EditedCleanFileName
                                                : f.CleanFileName).ToList();

            // Apri la finestra di dialogo per il contesto video
            var contextDialog = new Views.VideoContextDialog(selectedFileNames, App.ApiSettings.ApiEndpoint);
            contextDialog.Owner = this;

            // Mostra la finestra di dialogo
            var result = contextDialog.ShowDialog();
            if (result == true)
            {
                // Ottieni i risultati della traduzione
                var translations = contextDialog.ProcessingResult;

                // Aggiorna i file video con le traduzioni ricevute
                foreach (var item in selectedItems)
                {
                    var cleanFileName = !string.IsNullOrEmpty(item.EditedCleanFileName)
                        ? item.EditedCleanFileName
                        : item.CleanFileName;

                    var current = translations.Videos.FirstOrDefault(t => t.Name == cleanFileName);
                    if (current == null)
                        continue;

                    // Ottieni la lingua predefinita dal database
                    using var dbContext = App.DatabaseService.CreateContext();
                    var defaultLanguage = dbContext.Languages.FirstOrDefault(l => l.IsDefault);
                    var allLanguages = dbContext.Languages.ToList();

                    // Trova la traduzione per la lingua predefinita (italiano)
                    var defaultTranslation = current.Translations.FirstOrDefault(t =>
                        string.Equals(t.Language, defaultLanguage?.Name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(t.Language, "Italian", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(t.Language, "Italiano", StringComparison.OrdinalIgnoreCase));

                    if (defaultTranslation != null)
                    {
                        item.Title = defaultTranslation.Title;
                        item.Description = defaultTranslation.Description;
                    }

                    // Aggiungi le altre traduzioni
                    foreach (var translation in current.Translations)
                    {
                        // Trova la lingua corrispondente nel database
                        var language = allLanguages.FirstOrDefault(l =>
                            string.Equals(l.Name, translation.Language, StringComparison.OrdinalIgnoreCase));

                        if (language != null && !language.IsDefault)
                        {
                            if (!item.Translations.TryGetValue(language.Id, out var translationItem))
                            {
                                translationItem = new TranslationItem();
                                item.Translations.Add(language.Id, translationItem);
                            }

                            translationItem.Title = translation.Title;
                            translationItem.Description = translation.Description;
                        }
                    }
                }

                // Aggiorna la visualizzazione
                FileListView.Items.Refresh();
            }
        }

        private async void UploadToYouTubeButton_Click(object sender, RoutedEventArgs e)
        {
            // Verifica se ci sono file selezionati
            var selectedVideos = VideoFiles.Where(f => f.IsSelected).ToList();
            if (selectedVideos.Count == 0)
            {
                System.Windows.MessageBox.Show("Nessun file selezionato!", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Carica i disclaimer dal database
            Dictionary<int, string> disclaimers;
            int defaultLanguageId;
            var context = App.DatabaseService.CreateContext();

            disclaimers = context.Disclaimers.ToDictionary(d => d.LanguageId, d => d.Text);
            defaultLanguageId = context.Languages.FirstOrDefault(l => l.IsDefault)?.Id ?? 0; // Assumi 0 se non trovata, anche se dovrebbe esserci


            // Prepara i video per l'upload, aggiungendo i disclaimer se necessario
            foreach (var video in selectedVideos)
            {
                if (video.containsWeapon)
                {
                    // Aggiungi disclaimer alla descrizione principale (lingua predefinita)
                    if (defaultLanguageId != 0 && disclaimers.TryGetValue(defaultLanguageId, out var defaultDisclaimer) && !string.IsNullOrEmpty(defaultDisclaimer))
                    {
                        video.Description = $"{video.Description}\n\n{defaultDisclaimer}".Trim();
                    }

                    // Aggiungi disclaimer alle traduzioni
                    foreach (var langId in video.Translations.Keys.ToList()) // Usa ToList per evitare problemi di modifica durante l'iterazione
                    {
                        if (disclaimers.TryGetValue(langId, out var translatedDisclaimer) && !string.IsNullOrEmpty(translatedDisclaimer))
                        {
                            var translationItem = video.Translations[langId];
                            translationItem.Description = $"{translationItem.Description}\n\n{translatedDisclaimer}".Trim();
                        }
                    }
                }
            }


            var result = System.Windows.MessageBox.Show(
                $"Sei sicuro di voler caricare {selectedVideos.Count} video su YouTube?\n\n" +
                "Assicurati che tutti i video abbiano titolo e descrizione corretti.",
                "Conferma Caricamento",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Mostra indicatore di caricamento in corso
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                    var settings = context.Settings.FirstOrDefault() ?? new Settings { Id = 1 };

                    // Show progress panel
                    ProgressPanel.Visibility = Visibility.Visible;
                    UploadToYouTubeButton.IsEnabled = false;

                    // Esegui il caricamento in modo asincrono con progress callback
                    var uploadResults = await App.YouTubeUploadService.UploadVideosAsync(selectedVideos, settings.DefaultHashtags.Split(",",StringSplitOptions.TrimEntries), OnUploadProgress);

                    // Mostra un riepilogo dei risultati
                    int successCount = uploadResults.Count(r => r.Success);
                    int failCount = uploadResults.Count(r => !r.Success);

                    var resultMessage = $"Caricamento completato.\n\n" +
                        $"- Video caricati con successo: {successCount}\n" +
                        $"- Video falliti: {failCount}";

                    if (failCount > 0)
                    {
                        resultMessage += "\n\nDettagli errori:";
                        foreach (var failedUpload in uploadResults.Where(r => !r.Success))
                        {
                            resultMessage += $"\n- {failedUpload.FileName}: {failedUpload.ErrorMessage}";
                        }
                    }

                    System.Windows.MessageBox.Show(resultMessage, "Risultato Caricamento",
                        MessageBoxButton.OK,
                        failCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Errore durante il caricamento: {ex.Message}",
                        "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // Hide progress panel and re-enable button
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    UploadToYouTubeButton.IsEnabled = true;
                    // Ripristina il cursore
                    Mouse.OverrideCursor = null;
                }
            }
            else
            {
                // Se l'utente annulla, ricarica i dati originali per rimuovere i disclaimer aggiunti temporaneamente
                // (Questo potrebbe richiedere di ricaricare i dati o clonare gli oggetti prima della modifica)
                // Per semplicità, qui potremmo semplicemente informare l'utente che le modifiche ai disclaimer non sono state salvate permanentemente.
                // Oppure, si potrebbe clonare 'selectedVideos' prima di aggiungere i disclaimer e passare il clone al servizio di upload.
                // ProcessFiles(); // Ricarica i file per resettare le descrizioni modificate
                MessageBox.Show("Caricamento annullato. Le descrizioni sono state ripristinate.", "Annullato", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles upload progress updates and updates the UI accordingly
        /// </summary>
        private void OnUploadProgress(UploadProgressInfo progressInfo)
        {
            // Ensure UI updates happen on the UI thread
            Dispatcher.Invoke(() =>
            {
                ProgressStatusText.Text = $"Video {progressInfo.CurrentVideoNumber} di {progressInfo.TotalVideos}: {progressInfo.Status}";
                CurrentFileText.Text = progressInfo.CurrentFileName;
                UploadProgressBar.Value = progressInfo.OverallProgress;
            });
        }

        /// <summary>
        /// Gestisce la pulizia delle credenziali YouTube
        /// </summary>
        private async void ClearYouTubeCredentials_Click(object sender, RoutedEventArgs e)
        {
            // Mostra un messaggio di conferma prima di procedere
            var result = MessageBox.Show(
                "Questa operazione eliminerà le credenziali YouTube memorizzate.\n" +
                "Al prossimo utilizzo sarà necessario autenticarsi nuovamente.\n\n" +
                "Vuoi continuare?",
                "Conferma pulizia credenziali",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    bool success = App.YouTubeUploadService.ClearStoredCredentials();

                    if (success)
                    {
                        Mouse.OverrideCursor = null;
                        
                        // Chiedi all'utente se vuole effettuare subito il login con le nuove credenziali
                        var loginResult = MessageBox.Show(
                            "Credenziali YouTube eliminate con successo.\n\n" +
                            "Vuoi effettuare subito il login per ottenere un nuovo token di accesso?",
                            "Login YouTube",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (loginResult == MessageBoxResult.Yes)
                        {
                            Mouse.OverrideCursor = Cursors.Wait;
                            
                            // Forza una nuova autenticazione
                            bool loginSuccess = await App.YouTubeUploadService.ForceReauthenticationAsync();
                            
                            if (loginSuccess)
                            {
                                MessageBox.Show(
                                    "Login YouTube completato con successo.\n" +
                                    "Il servizio è ora pronto per l'utilizzo.",
                                    "Login completato",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Errore durante il login YouTube.\n" +
                                    "Riprova più tardi o verifica le credenziali.",
                                    "Errore login",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            MessageBox.Show(
                                "Credenziali eliminate. Il login sarà richiesto al primo utilizzo del servizio YouTube.",
                                "Operazione completata",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Si è verificato un errore durante la pulizia delle credenziali:\n{ex.Message}",
                        "Errore",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private void UpdateOrderIndexes()
        {
            // Aggiorna gli indici di ordinamento per tutti gli elementi nella lista
            for (int i = 0; i < VideoFiles.Count; i++)
            {
                VideoFiles[i].OrderIndex = i + 1; // Indice 1-based per migliore leggibilità
            }
            // Aggiorna la visualizzazione
            FileListView.Items.Refresh();
        }

        private void ApplyDateButton_Click(object sender, RoutedEventArgs e)
        {
            // Ottieni la data selezionata nel DatePicker
            if (PublishDatePicker.SelectedDate != null)
            {
                DateTime baseDate = PublishDatePicker.SelectedDate.Value;

                // Applica la data a tutti gli elementi selezionati, incrementando di un giorno ogni volta
                for (int i = 0; i < VideoFiles.Count; i++)
                {
                    VideoFiles[i].PublishDate = baseDate.AddDays(i);
                }

                // Aggiorna la visualizzazione
                FileListView.Items.Refresh();

                MessageBox.Show($"Data di pubblicazione aggiornata per {VideoFiles.Count} elementi.", "Operazione completata", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MoveItemUpButton_Click(object sender, RoutedEventArgs e)
        {
            // Ottiene l'elemento direttamente dal DataContext del bottone cliccato
            if (sender is FrameworkElement element && element.DataContext is VideoFile itemToMove)
            {
                int currentIndex = VideoFiles.IndexOf(itemToMove);

                // Verifica che non sia già il primo elemento
                if (currentIndex > 0)
                {
                    // Sposta l'elemento in alto
                    VideoFiles.Move(currentIndex, currentIndex - 1);

                    // Aggiorna gli indici di ordinamento
                    UpdateOrderIndexes();

                    // Imposta la selezione sull'elemento spostato per facilitare operazioni multiple
                    FileListView.SelectedItem = itemToMove;
                    FileListView.ScrollIntoView(itemToMove);
                }
            }
        }

        private void MoveItemDownButton_Click(object sender, RoutedEventArgs e)
        {
            // Ottiene l'elemento direttamente dal DataContext del bottone cliccato
            if (sender is FrameworkElement element && element.DataContext is VideoFile itemToMove)
            {
                int currentIndex = VideoFiles.IndexOf(itemToMove);

                // Verifica che non sia già l'ultimo elemento
                if (currentIndex < VideoFiles.Count - 1)
                {
                    // Sposta l'elemento in basso
                    VideoFiles.Move(currentIndex, currentIndex + 1);

                    // Aggiorna gli indici di ordinamento
                    UpdateOrderIndexes();

                    // Imposta la selezione sull'elemento spostato per facilitare operazioni multiple
                    FileListView.SelectedItem = itemToMove;
                    FileListView.ScrollIntoView(itemToMove);
                }
            }
        }

        /// <summary>
        /// Gestisce la validazione dell'input per garantire che siano inseriti solo numeri
        /// </summary>
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Verifica che l'input sia numerico
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
                return;
            }

            // Controlla se l'input è per ora o minuti e valida il valore risultante
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                string currentText = textBox.Text;
                string newText = currentText.Substring(0, textBox.SelectionStart) + e.Text + currentText.Substring(textBox.SelectionStart + textBox.SelectionLength);

                if (int.TryParse(newText, out int value))
                {
                    // Validazione basata sul tipo di campo (ora o minuti)
                    if (textBox.Tag?.ToString() == "Hour" && (value < 0 || value > 23))
                    {
                        e.Handled = true;
                    }
                    else if (textBox.Tag?.ToString() == "Minute" && (value < 0 || value > 59))
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var isChecked = (sender as CheckBox)?.IsChecked ?? false;

            foreach (var item in VideoFiles)
            {
                item.IsSelected = isChecked;
            }
            FileListView.Items.Refresh();
        }

        // Tenant-related methods
        private async void LoadTenants()
        {
            try
            {
                var tenants = await App.TenantService.GetActiveTenantsAsync();
                TenantComboBox.ItemsSource = tenants;
                
                // Set the current tenant
                var currentTenant = tenants.FirstOrDefault(t => t.Id == App.TenantContext.CurrentTenantId);
                if (currentTenant != null)
                {
                    TenantComboBox.SelectedItem = currentTenant;
                }
                else if (tenants.Any())
                {
                    // Select the first tenant if current is not found
                    TenantComboBox.SelectedItem = tenants.First();
                    App.TenantContext.SetCurrentTenant(tenants.First().Id, tenants.First().Name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel caricamento dei tenant: {ex.Message}", "Errore", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TenantComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TenantComboBox.SelectedItem is Tenant selectedTenant)
            {
                App.TenantContext.SetCurrentTenant(selectedTenant.Id, selectedTenant.Name);
                
                // Clear current data and reload for the new tenant
                VideoFiles.Clear();
                selectedFolders.Clear();
                selectedFiles.Clear();
                
                // Update title to show current tenant
                Title = $"Video Importer - {selectedTenant.Name}";
            }
        }

        private void TenantManagementMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tenantManagementPage = new TenantManagementPage(App.TenantService, App.TenantContext);
                var dialog = new Window
                {
                    Content = tenantManagementPage,
                    Title = "Gestione Tenant",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };
                
                dialog.ShowDialog();
                
                // Reload tenants after the dialog closes
                LoadTenants();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'apertura della gestione tenant: {ex.Message}", "Errore", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Classe per contenere i dati di traduzione con titolo e descrizione
    public class TranslationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    public class VideoFile : INotifyPropertyChanged
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        
        private bool _isSelected;
        public bool IsSelected 
        { 
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string EditedCleanFileName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DefaultLanguage { get; set; } = string.Empty;

        private bool _containsWeapon;
        public bool containsWeapon
        {
            get => _containsWeapon;
            set
            {
                if (_containsWeapon != value)
                {
                    _containsWeapon = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _publishDate = DateTime.Today;
        public DateTime PublishDate
        {
            get => _publishDate;
            set
            {
                if (_publishDate != value)
                {
                    _publishDate = value;
                    OnPropertyChanged();
                }
            }
        }

        private TimeSpan _publishTime = TimeSpan.FromHours(0);
        public TimeSpan PublishTime
        {
            get => _publishTime;
            set
            {
                if (_publishTime != value)
                {
                    _publishTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PublishTimeHour));
                    OnPropertyChanged(nameof(PublishTimeMinute));
                }
            }
        }

        public string PublishTimeHour
        {
            get => PublishTime.Hours.ToString("00");
            set
            {
                if (int.TryParse(value, out int hours) && hours >= 0 && hours <= 23)
                {
                    PublishTime = new TimeSpan(hours, PublishTime.Minutes, 0);
                }
            }
        }

        public string PublishTimeMinute
        {
            get => PublishTime.Minutes.ToString("00");
            set
            {
                if (int.TryParse(value, out int minutes) && minutes >= 0 && minutes <= 59)
                {
                    PublishTime = new TimeSpan(PublishTime.Hours, minutes, 0);
                }
            }
        }

        public int OrderIndex { get; set; }
        public Dictionary<int, TranslationItem> Translations { get; set; } = new Dictionary<int, TranslationItem>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Mantenuto per retrocompatibilità
        public Dictionary<int, string> TranslatedTitles
        {
            get
            {
                var result = new Dictionary<int, string>();
                foreach (var kvp in Translations)
                {
                    result[kvp.Key] = kvp.Value.Title;
                }
                return result;
            }
        }

        // Clean filename property - replaces non-alphanumeric chars with spaces and removes extension
        public string CleanFileName
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                    return string.Empty;

                if (!string.IsNullOrEmpty(EditedCleanFileName))
                    return EditedCleanFileName;

                // Rimuove l'estensione
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);

                // Sostituisce caratteri non alfanumerici con spazi
                return System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9]", " ").Trim();
            }
        }
    }
}
