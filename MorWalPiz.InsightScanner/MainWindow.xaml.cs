using System.Windows;
using MorWalPiz.Contracts.DTOs;
using MorWalPiz.InsightScanner.Models;

namespace MorWalPiz.InsightScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml. The user selects a topic, optionally auto-collects
    /// posts from generic public sources, and drives the embedded browser interactively (manual
    /// login/navigation) to collect posts from sources like Instagram that require an authenticated,
    /// JS-rendered session. Collected batches are then submitted together as one manual scan run.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, List<RawSocialPostDto>> _collectedPostsBySource = new();
        private List<InsightTopicSummary> _topics = [];

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Browser.EnsureCoreWebView2Async();
            MaxPostsTextBox.Text = App.Settings.DefaultMaxPostsPerSource.ToString();

            await LoadTopicsAsync();
        }

        private async Task LoadTopicsAsync()
        {
            try
            {
                _topics = await App.BackOfficeClient.GetTopicsAsync();
                TopicComboBox.ItemsSource = _topics;
                if (_topics.Count > 0)
                    TopicComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppendResult($"Errore nel caricamento dei topic: {ex.Message}");
            }
        }

        private void TopicComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var topic = TopicComboBox.SelectedItem as InsightTopicSummary;
            SourceComboBox.ItemsSource = topic?.PreferredSources ?? [];
            if (topic?.PreferredSources.Length > 0)
                SourceComboBox.SelectedIndex = 0;
        }

        private int GetMaxPosts() => int.TryParse(MaxPostsTextBox.Text, out var value) && value > 0
            ? value
            : App.Settings.DefaultMaxPostsPerSource;

        private string? GetSelectedSourceUrl() => SourceComboBox.SelectedItem as string;

        private async void NavigateButton_Click(object sender, RoutedEventArgs e)
        {
            var sourceUrl = GetSelectedSourceUrl();
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return;

            await Browser.EnsureCoreWebView2Async();
            Browser.CoreWebView2.Navigate(sourceUrl);
        }

        private async void AutoCollectButton_Click(object sender, RoutedEventArgs e)
        {
            var sourceUrl = GetSelectedSourceUrl();
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return;

            if (App.Scanner.RequiresInteractiveBrowser(sourceUrl))
            {
                AppendResult($"'{sourceUrl}' richiede la raccolta interattiva dal browser (login manuale necessario).");
                return;
            }

            try
            {
                var posts = await App.Scanner.CollectAutomaticallyAsync(sourceUrl, GetMaxPosts(), CancellationToken.None);
                AddCollectedPosts(sourceUrl, posts);
            }
            catch (Exception ex)
            {
                AppendResult($"Errore nel recupero automatico di '{sourceUrl}': {ex.Message}");
            }
        }

        private async void BrowserCollectButton_Click(object sender, RoutedEventArgs e)
        {
            var sourceUrl = GetSelectedSourceUrl();
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return;

            try
            {
                await Browser.EnsureCoreWebView2Async();
                var script = Services.InstagramDomExtractor.GetExtractionScript(GetMaxPosts());
                var scriptResult = await Browser.CoreWebView2.ExecuteScriptAsync(script);
                var posts = Services.InstagramDomExtractor.ParseExtractionResult(scriptResult, sourceUrl);

                if (posts.Count == 0)
                {
                    AppendResult("Nessun post individuato nella pagina corrente. Assicurati di aver effettuato l'accesso e di essere sulla pagina del profilo.");
                    return;
                }

                AddCollectedPosts(sourceUrl, posts);
            }
            catch (Exception ex)
            {
                AppendResult($"Errore nella raccolta dal browser: {ex.Message}");
            }
        }

        private void AddCollectedPosts(string sourceUrl, List<RawSocialPostDto> posts)
        {
            if (!_collectedPostsBySource.TryGetValue(sourceUrl, out var existing))
            {
                existing = [];
                _collectedPostsBySource[sourceUrl] = existing;
            }

            var existingUrls = existing.Select(p => p.PostUrl).ToHashSet(StringComparer.OrdinalIgnoreCase);
            existing.AddRange(posts.Where(p => !existingUrls.Contains(p.PostUrl)));

            RefreshBatchesList();
        }

        private void RefreshBatchesList()
        {
            CollectedBatchesListBox.ItemsSource = _collectedPostsBySource
                .Select(kv => $"{kv.Key} — {kv.Value.Count} post")
                .ToList();
        }

        private async void RunScanButton_Click(object sender, RoutedEventArgs e)
        {
            var topic = TopicComboBox.SelectedItem as InsightTopicSummary;
            if (topic == null)
            {
                AppendResult("Seleziona un topic prima di avviare la scansione.");
                return;
            }

            if (_collectedPostsBySource.Count == 0)
            {
                AppendResult("Nessun post raccolto: usa il recupero automatico o la raccolta dal browser prima di eseguire la scansione.");
                return;
            }

            var request = new ManualScanRequest
            {
                MaxPostsPerSource = GetMaxPosts(),
                Sources = _collectedPostsBySource
                    .Select(kv => new SourceScanBatchDto { SourceUrl = kv.Key, Posts = kv.Value })
                    .ToList()
            };

            RunScanButton.IsEnabled = false;
            try
            {
                var result = await App.BackOfficeClient.SubmitManualScanAsync(topic.Id, request);
                AppendResult(FormatScanResult(result));

                _collectedPostsBySource.Clear();
                RefreshBatchesList();
            }
            catch (Exception ex)
            {
                AppendResult($"Errore durante l'invio della scansione: {ex.Message}");
            }
            finally
            {
                RunScanButton.IsEnabled = true;
            }
        }

        private static string FormatScanResult(ManualScanResponseDto result)
        {
            var lines = result.SourceSummaries.Select(s =>
                $"{s.SourceUrl}: elaborati {s.ProcessedCount}, creati {s.CreatedCount}, duplicati {s.SkippedDuplicateCount}, non rilevanti {s.SkippedNotNewsCount}" +
                (string.IsNullOrEmpty(s.Error) ? string.Empty : $", errore: {s.Error}"));

            return $"Scansione completata. Nuove news create: {result.CreatedNewsItemIds.Count}\n" + string.Join("\n", lines);
        }

        private void AppendResult(string message)
        {
            ResultTextBox.Text = $"{message}\n{ResultTextBox.Text}";
        }
    }
}
