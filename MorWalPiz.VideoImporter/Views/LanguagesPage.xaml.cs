using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter.Views;

public partial class LanguagesPage : Window
{
    private readonly DatabaseService _databaseService;
    private readonly IChannelConfigurationSyncService _channelSettingsSyncService;
    private List<Language> _languages = [];
    private Dictionary<int, string> _originalCodesById = [];
    private bool _isSaving;

    public LanguagesPage()
    {
        InitializeComponent();
        _databaseService = App.DatabaseService;
        _channelSettingsSyncService = App.ChannelSettingsSyncService;
        LoadLanguages();
        SetMutationControlsEnabled(_channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId()));
        StatusTextBlock.Text = _channelSettingsSyncService.CurrentStatus.Message;
    }

    private void LoadLanguages()
    {
        using var context = _databaseService.CreateContext();
        _languages = context.Languages.AsNoTracking().OrderBy(language => language.Id).ToList();
        _originalCodesById = _languages.ToDictionary(language => language.Id, language => language.Code);
        DefaultLanguageComboBox.ItemsSource = _languages;
        DefaultLanguageComboBox.SelectedItem = _languages.FirstOrDefault(language => language.IsDefault);
        LanguagesListView.ItemsSource = _languages.ToList();
    }

    private void DefaultLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void SecondaryLanguage_Click(object sender, RoutedEventArgs e)
    {
    }

    private async void AddLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateSettings())
            return;

        var dialog = new LanguageEditDialog { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        _languages.Add(dialog.Language);
        LanguagesListView.ItemsSource = _languages.ToList();
        await SaveLanguageSettingsAsync();
    }

    private async void EditLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateSettings() || LanguagesListView.SelectedItem is not Language selectedLanguage)
            return;

        var originalCode = selectedLanguage.Code;
        var dialog = new LanguageEditDialog(selectedLanguage) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        var index = _languages.FindIndex(language => language.Id == selectedLanguage.Id);
        if (index < 0)
            return;

        var updatedLanguage = dialog.Language;
        updatedLanguage.Id = selectedLanguage.Id;
        updatedLanguage.IsDefault = selectedLanguage.IsDefault;
        updatedLanguage.TenantId = selectedLanguage.TenantId;
        _languages[index] = updatedLanguage;
        _originalCodesById[updatedLanguage.Id] = originalCode;
        DefaultLanguageComboBox.ItemsSource = _languages;
        DefaultLanguageComboBox.SelectedItem = _languages.FirstOrDefault(language => language.IsDefault);
        LanguagesListView.ItemsSource = _languages.ToList();
        await SaveLanguageSettingsAsync();
    }

    private async void DeleteLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateSettings() || LanguagesListView.SelectedItem is not Language selectedLanguage)
            return;
        if (selectedLanguage.IsDefault)
        {
            MessageBox.Show("Non è possibile eliminare la lingua predefinita.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int disclaimersCount;
        try
        {
            using var context = _databaseService.CreateContext();
            disclaimersCount = await context.Disclaimers.CountAsync(disclaimer => disclaimer.LanguageId == selectedLanguage.Id);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Could not check language disclaimer usage: {0}", exception);
            MessageBox.Show("Impossibile verificare l'utilizzo della lingua. Riprova.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (disclaimersCount > 0)
        {
            MessageBox.Show($"Non è possibile eliminare questa lingua perché è utilizzata da {disclaimersCount} disclaimer.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Sei sicuro di voler eliminare la lingua '{selectedLanguage.Name}'?",
            "Conferma Eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        _languages.Remove(selectedLanguage);
        _originalCodesById.Remove(selectedLanguage.Id);
        DefaultLanguageComboBox.ItemsSource = _languages;
        LanguagesListView.ItemsSource = _languages.ToList();
        await SaveLanguageSettingsAsync();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanMutateSettings())
            return;

        if (DefaultLanguageComboBox.SelectedItem is not Language selectedDefault)
        {
            MessageBox.Show("Seleziona una lingua predefinita.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedLanguages = LanguagesListView.ItemsSource as IEnumerable<Language> ?? _languages;
        foreach (var language in _languages)
        {
            language.IsDefault = language.Id == selectedDefault.Id;
            var displayedLanguage = selectedLanguages.FirstOrDefault(item => item.Id == language.Id);
            if (displayedLanguage is not null)
                language.IsSelected = displayedLanguage.IsSelected;
        }

        await SaveLanguageSettingsAsync(closeOnSuccess: true);
    }

    private async Task SaveLanguageSettingsAsync(bool closeOnSuccess = false)
    {
        if (_isSaving)
            return;

        _isSaving = true;
        SetMutationControlsEnabled(false);
        CancelButton.IsEnabled = false;
        StatusTextBlock.Text = "Salvataggio delle impostazioni lingua...";
        try
        {
            var channelId = App.GetCurrentChannelId();
            var localSnapshot = await _channelSettingsSyncService.GetLocalSnapshotAsync(App.ApplicationStoppingToken);
            var candidateLanguages = _languages.Select(language =>
            {
                var previousCode = _originalCodesById.GetValueOrDefault(language.Id, language.Code);
                var previous = localSnapshot.Languages.FirstOrDefault(item =>
                    string.Equals(item.Code, previousCode, StringComparison.OrdinalIgnoreCase));
                return new ChannelLanguageSnapshot(
                    language.Code.Trim().ToLowerInvariant(),
                    language.Name.Trim(),
                    language.IsDefault,
                    language.IsSelected,
                    previous?.DisclaimerText);
            }).ToArray();

            var result = await _channelSettingsSyncService.SaveAsync(
                channelId,
                localSnapshot with { Languages = candidateLanguages },
                App.ApplicationStoppingToken);
            StatusTextBlock.Text = result.Message;
            if (!result.LocalCacheSynchronized)
            {
                MessageBox.Show(result.Message, "Cache locale non sincronizzata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadLanguages();
            if (closeOnSuccess)
            {
                MessageBox.Show("Impostazioni lingua salvate con successo!", "Salvataggio Completato", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
        }
        catch (OperationCanceledException) when (App.ApplicationStoppingToken.IsCancellationRequested)
        {
            StatusTextBlock.Text = "Salvataggio annullato durante la chiusura dell'applicazione.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Salvataggio non riuscito. Le modifiche restano disponibili per un nuovo tentativo.";
            System.Diagnostics.Trace.TraceError("Language settings UI save failed: {0}", ex);
            MessageBox.Show(
                "Il server non ha confermato il salvataggio. Le modifiche restano disponibili per un nuovo tentativo.",
                "Salvataggio non riuscito",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            SaveButton.Focus();
        }
        finally
        {
            _isSaving = false;
            SetMutationControlsEnabled(_channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId()));
            CancelButton.IsEnabled = true;
        }
    }

    private bool CanMutateSettings() =>
        !_isSaving && _channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId());

    private void SetMutationControlsEnabled(bool enabled)
    {
        DefaultLanguageComboBox.IsEnabled = enabled;
        LanguagesListView.IsEnabled = enabled;
        AddLanguageButton.IsEnabled = enabled;
        EditLanguageButton.IsEnabled = enabled && LanguagesListView.SelectedItem is not null;
        DeleteLanguageButton.IsEnabled = enabled && LanguagesListView.SelectedItem is not null;
        SaveButton.IsEnabled = enabled;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
