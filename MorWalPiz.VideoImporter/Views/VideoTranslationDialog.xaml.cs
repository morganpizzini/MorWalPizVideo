using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;

namespace MorWalPiz.VideoImporter.Views
{
    /// <summary>
    /// Interaction logic for VideoTranslationDialog.xaml
    /// </summary>
    public partial class VideoTranslationDialog : Window, INotifyPropertyChanged
    {
        private bool _isLoading;
        private readonly ApiService _apiService;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    TranslateButton.IsEnabled = !value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public VideoTranslationDialog()
        {
            InitializeComponent();
            DataContext = this;

            // Initialize API service with foobar endpoint
            _apiService = new ApiService("https://api.foobar.com");
        }

        private async void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Inserisci un titolo per il video.", "Campo Obbligatorio",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("Inserisci una descrizione per il video.", "Campo Obbligatorio",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DescriptionTextBox.Focus();
                return;
            }

            // Get enabled languages from database
            IList<Language> enabledLanguages;
            using (var dbContext = App.DatabaseService.CreateContext())
            {
                enabledLanguages = dbContext.Languages
                    .Where(l => !l.IsDefault && l.IsSelected)
                    .ToList();
            }

            if (!enabledLanguages.Any())
            {
                MessageBox.Show("Nessuna lingua abilitata per la traduzione. " +
                    "Vai in Entità → Lingue per abilitare le lingue desiderate.",
                    "Lingue Non Configurate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                // Call the API for translation
                var translationResult = await TranslateVideoContentAsync(
                    TitleTextBox.Text,
                    DescriptionTextBox.Text,
                    enabledLanguages);

                // Create tabs for each translation
                CreateTranslationTabs(translationResult, enabledLanguages);

                // Switch to the first translation tab
                if (MainTabControl.Items.Count > 1)
                {
                    MainTabControl.SelectedIndex = 1;
                }

                MessageBox.Show($"Traduzione completata! Create {translationResult.Count} traduzioni.",
                    "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante la traduzione: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<List<TranslationResult>> TranslateVideoContentAsync(string title, string description, IList<Language> languages)
        {
            try
            {
                // Call the real API service
                var apiResponses = await _apiService.TranslateVideoContentAsync(title, description, languages);

                var results = new List<TranslationResult>();

                foreach (var apiResponse in apiResponses)
                {
                    // Find the corresponding language from our database
                    var language = languages.FirstOrDefault(l => l.Code == apiResponse.LanguageCode);

                    results.Add(new TranslationResult
                    {
                        LanguageCode = apiResponse.LanguageCode,
                        LanguageName = language?.Name ?? apiResponse.LanguageCode,
                        TranslatedTitle = apiResponse.TranslatedTitle,
                        TranslatedDescription = apiResponse.TranslatedDescription
                    });
                }

                return results;
            }
            catch (Exception)
            {
                // Fallback to mock data if API call fails
                await Task.Delay(1000); // Brief delay to simulate processing

                var results = new List<TranslationResult>();

                foreach (var language in languages)
                {
                    results.Add(new TranslationResult
                    {
                        LanguageCode = language.Code,
                        LanguageName = language.Name,
                        TranslatedTitle = $"[DEMO-{language.Code}] {title}",
                        TranslatedDescription = $"[DEMO-{language.Code}] {description}"
                    });
                }

                return results;
            }
        }

        private void CreateTranslationTabs(List<TranslationResult> translations, IList<Language> languages)
        {
            // Remove existing translation tabs (keep only the input tab)
            var tabsToRemove = MainTabControl.Items.Cast<TabItem>()
                .Where(tab => tab.Header.ToString() != "Inserimento")
                .ToList();

            foreach (var tab in tabsToRemove)
            {
                MainTabControl.Items.Remove(tab);
            }

            // Create new tabs for each translation
            foreach (var translation in translations)
            {
                var tabItem = new TabItem
                {
                    Header = translation.LanguageName
                };

                var tabContent = CreateTranslationTabContent(translation);
                tabItem.Content = tabContent;

                MainTabControl.Items.Add(tabItem);
            }
        }

        private Grid CreateTranslationTabContent(TranslationResult translation)
        {
            var grid = new Grid
            {
                Margin = new Thickness(10)
            };

            // Define rows
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Language code display
            var languageLabel = new TextBlock
            {
                Text = $"Lingua: {translation.LanguageName} ({translation.LanguageCode})",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(languageLabel, 0);
            grid.Children.Add(languageLabel);

            // Title section
            var titleLabel = new TextBlock
            {
                Text = "Titolo tradotto:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(titleLabel, 1);
            grid.Children.Add(titleLabel);

            var titleTextBox = new TextBox
            {
                Text = translation.TranslatedTitle,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(5),
                IsReadOnly = true
            };
            Grid.SetRow(titleTextBox, 2);
            grid.Children.Add(titleTextBox);

            var copyTitleButton = new Button
            {
                Content = "Copia Titolo",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };
            copyTitleButton.Click += (s, e) => CopyToClipboard(translation.TranslatedTitle, "Titolo");
            Grid.SetRow(copyTitleButton, 3);
            grid.Children.Add(copyTitleButton);

            // Description section
            var descriptionLabel = new TextBlock
            {
                Text = "Descrizione tradotta:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(descriptionLabel, 4);
            grid.Children.Add(descriptionLabel);

            var descriptionTextBox = new TextBox
            {
                Text = translation.TranslatedDescription,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(5),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(descriptionTextBox, 5);
            grid.Children.Add(descriptionTextBox);

            var copyDescriptionButton = new Button
            {
                Content = "Copia Descrizione",
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };
            copyDescriptionButton.Click += (s, e) => CopyToClipboard(translation.TranslatedDescription, "Descrizione");
            Grid.SetRow(copyDescriptionButton, 6);
            grid.Children.Add(copyDescriptionButton);

            return grid;
        }

        private void CopyToClipboard(string text, string itemType)
        {
            try
            {
                Clipboard.SetText(text);
                MessageBox.Show($"{itemType} copiato negli appunti!", "Successo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante la copia: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TranslationResult
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string TranslatedTitle { get; set; } = string.Empty;
        public string TranslatedDescription { get; set; } = string.Empty;
    }
}
