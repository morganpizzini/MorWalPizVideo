using Microsoft.Win32;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace MorWalPiz.VideoImporter.Views;

public partial class SocialPublishingPage : Window
{
    private readonly ObservableCollection<SocialMediaDescriptor> _media = [];
    private IReadOnlyList<SocialProviderCapability> _capabilities = [];

    public SocialPublishingPage()
    {
        InitializeComponent();
        MediaList.ItemsSource = _media;
        ProviderBox.ItemsSource = Enum.GetValues<SocialProviderKind>();
        ProviderBox.SelectedIndex = 0;
        ScheduleDate.SelectedDate = DateTime.Today.AddDays(1);
        LoadCapabilities();
    }

    private void LoadCapabilities()
    {
        _capabilities = App.SocialPublishingService.GetCapabilities(App.GetCurrentChannelId(), App.TenantContext.CurrentTenantId);
        ProviderBox.ItemsSource = _capabilities;
        ProviderBox.SelectedIndex = 0;
    }

    private SocialProviderCapability? SelectedCapability => ProviderBox.SelectedItem as SocialProviderCapability;

    private void AddMedia_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Multiselect = true, Filter = "Media|*.jpg;*.jpeg;*.png;*.mp4;*.mov;*.webm" };
        if (dialog.ShowDialog() != true) return;
        foreach (var path in dialog.FileNames)
        {
            _media.Add(new SocialMediaDescriptor
            {
                FilePath = path,
                MediaType = IsVideo(path) ? SocialMediaType.Video : SocialMediaType.Image
            });
        }
    }

    private void SelectThumbnail_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Immagine|*.jpg;*.jpeg;*.png" };
        if (dialog.ShowDialog() == true && _media.Count > 0) _media[0].ThumbnailPath = dialog.FileName;
    }

    private async void ExtractThumbnail_Click(object sender, RoutedEventArgs e)
    {
        var video = _media.FirstOrDefault(item => item.MediaType == SocialMediaType.Video);
        if (video is null)
        {
            MessageBox.Show("Seleziona prima un video.", "Thumbnail", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var thumbnailPath = await App.VideoThumbnailService.ExtractAsync(video.FilePath);
        if (thumbnailPath is null)
        {
            MessageBox.Show("Impossibile estrarre la thumbnail dal video selezionato.", "Thumbnail", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        video.ThumbnailPath = thumbnailPath;
        MessageBox.Show("Thumbnail estratta.", "Thumbnail", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CaptionBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var token = CaptionBox.Text.Split(' ').LastOrDefault() ?? string.Empty;
        HashtagSuggestions.ItemsSource = token.StartsWith('#')
            ? App.HashtagHistoryService.Suggest(token, App.TenantContext.CurrentTenantId, App.GetCurrentChannelId())
            : [];
    }

    private void HashtagSuggestion_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (HashtagSuggestions.SelectedItem is string hashtag)
            CaptionBox.Text += $"{(CaptionBox.Text.EndsWith(' ') ? string.Empty : " ")}{hashtag} ";
    }

    private void ProviderBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SelectedCapability is null) return;
        AspectRatioBox.ItemsSource = SelectedCapability.SupportedAspectRatios;
        AspectRatioBox.SelectedIndex = 0;
        CapabilityStatus.Text = SelectedCapability.StatusMessage;
        ScheduleBox.IsEnabled = SelectedCapability.SupportsScheduling;
        if (!SelectedCapability.SupportsScheduling) ScheduleBox.IsChecked = false;
    }

    private void ScheduleBox_Changed(object sender, RoutedEventArgs e)
    {
        var enabled = ScheduleBox.IsChecked == true && ScheduleBox.IsEnabled;
        ScheduleDate.IsEnabled = enabled;
        ScheduleTime.IsEnabled = enabled;
    }

    private async void Publish_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedCapability is null || AspectRatioBox.SelectedItem is not string aspectRatio)
        {
            MessageBox.Show("Seleziona un provider e un formato supportati.", "Validazione", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DateTimeOffset? scheduledAt = null;
        if (ScheduleBox.IsChecked == true && ScheduleDate.SelectedDate.HasValue && TimeSpan.TryParse(ScheduleTime.Text, out var time))
            scheduledAt = new DateTimeOffset(ScheduleDate.SelectedDate.Value.Date.Add(time));
        var draft = new SocialPostDraft
        {
            TenantId = App.TenantContext.CurrentTenantId,
            ChannelId = App.GetCurrentChannelId(),
            Caption = CaptionBox.Text,
            Media = _media.ToList(),
            Providers = [SelectedCapability.Provider],
            AspectRatio = aspectRatio,
            ScheduledAt = scheduledAt
        };
        App.HashtagHistoryService.Record(HashtagNormalizer.Extract(draft.Caption), draft.TenantId, draft.ChannelId);
        var results = await App.SocialPublishingService.PublishAsync(draft);
        MessageBox.Show(string.Join(Environment.NewLine, results.Select(result => result.Message)), "Risultato", MessageBoxButton.OK, results.All(result => result.Succeeded) ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private static bool IsVideo(string path) => new[] { ".mp4", ".mov", ".webm" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}