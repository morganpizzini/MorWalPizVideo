using System.Windows;
using MorWalPiz.Contracts.DTOs;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter.Views
{
    public partial class VideoTranscriptAnalysisDialog : Window
    {
        private readonly ApiService _apiService;

        public VideoTranscriptAnalysisDialog(string apiEndpoint, string? apiKey = null)
        {
            InitializeComponent();
            _apiService = new ApiService(apiEndpoint,apiKey);
        }

        private async void TrimTextBUtton_Click(object sender, RoutedEventArgs e)
        {
            TranscriptTextBox.Text = TranscriptTextBox.Text?.Trim();
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            var transcript = TranscriptTextBox.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(transcript))
            {
                System.Windows.MessageBox.Show("Inserisci una trascrizione prima di analizzare.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Disable button and show status
                AnalyzeButton.IsEnabled = false;
                StatusTextBlock.Text = "Analisi in corso...";
                ResultsPanel.Visibility = Visibility.Collapsed;

                var request = new TranscriptAnalysisRequest
                {
                    Transcript = transcript,
                    Context = ContextTextBox.Text?.Trim()
                };

                var result = await _apiService.AnalyzeTranscriptAsync(request);

                // Display results
                SeoDescriptionTextBox.Text = result.SeoDescription;
                
                TitlesListBox.Items.Clear();
                foreach (var title in result.Titles)
                {
                    TitlesListBox.Items.Add(title);
                }

                DescriptionsListBox.Items.Clear();
                foreach (var description in result.Descriptions)
                {
                    DescriptionsListBox.Items.Add(description);
                }

                HashtagsTextBox.Text = string.Join(", ", result.Hashtags);

                ResultsPanel.Visibility = Visibility.Visible;
                StatusTextBlock.Text = "Analisi completata con successo!";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Errore durante l'analisi: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Errore durante l'analisi.";
            }
            finally
            {
                AnalyzeButton.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
