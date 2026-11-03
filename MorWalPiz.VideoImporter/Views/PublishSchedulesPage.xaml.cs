using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using MessageBox = System.Windows.MessageBox;

namespace MorWalPiz.VideoImporter.Views
{
    /// <summary>
    /// Interaction logic for PublishSchedulesPage.xaml
    /// </summary>
    public partial class PublishSchedulesPage : Window
    {
        private readonly PublishScheduleService _scheduleService;
        private readonly DatabaseService _databaseService;
        private readonly IChannelConfigurationSyncService _channelSettingsSyncService;
        private bool _isSaving;
        public ObservableCollection<PublishScheduleViewModel> Schedules { get; set; }

        public PublishSchedulesPage()
        {
            InitializeComponent();
            _scheduleService = new PublishScheduleService(App.DatabaseService);
            _databaseService = App.DatabaseService;
            _channelSettingsSyncService = App.ChannelSettingsSyncService;
            Schedules = new ObservableCollection<PublishScheduleViewModel>();
            SchedulesListView.ItemsSource = Schedules;
            LoadSchedules();
            SetMutationControlsEnabled(_channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId()));
            StatusTextBlock.Text = _channelSettingsSyncService.CurrentStatus.Message;
        }

        private void LoadSchedules()
        {
            try
            {
                Schedules.Clear();
                var schedules = _scheduleService.GetAllSchedules();
                
                foreach (var schedule in schedules)
                {
                    Schedules.Add(new PublishScheduleViewModel(schedule));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Publish schedule cache load failed: {0}", ex);
                MessageBox.Show("Impossibile caricare le pianificazioni locali.",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            var editDialog = new ScheduleEditDialog();
            editDialog.Owner = this;
            
            if (editDialog.ShowDialog() == true)
            {
                var candidate = editDialog.Schedule;
                while (true)
                {
                    if (await SaveScheduleCandidateAsync(candidate))
                        break;
                    if (!_channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId()))
                        break;

                    var retryDialog = new ScheduleEditDialog(candidate) { Owner = this };
                    if (retryDialog.ShowDialog() != true)
                        break;
                    candidate = retryDialog.Schedule;
                }
            }
        }

        private async void EditScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PublishScheduleViewModel viewModel)
            {
                var editDialog = new ScheduleEditDialog(viewModel.Schedule);
                editDialog.Owner = this;
                
                if (editDialog.ShowDialog() == true)
                {
                    var candidate = editDialog.Schedule;
                    while (true)
                    {
                        if (await SaveScheduleCandidateAsync(candidate, viewModel.Schedule.Name))
                            break;
                        if (!_channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId()))
                            break;

                        var retryDialog = new ScheduleEditDialog(candidate) { Owner = this };
                        if (retryDialog.ShowDialog() != true)
                            break;
                        candidate = retryDialog.Schedule;
                    }
                }
            }
        }

        private async void DeleteScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PublishScheduleViewModel viewModel)
            {
                var result = MessageBox.Show(
                    $"Sei sicuro di voler eliminare la pianificazione '{viewModel.Name}'?",
                    "Conferma eliminazione",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var snapshot = await _channelSettingsSyncService.GetLocalSnapshotAsync(App.ApplicationStoppingToken);
                        var schedules = snapshot.PublishSchedules
                            .Where(schedule => !string.Equals(
                                schedule.Name,
                                viewModel.Schedule.Name,
                                StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                        await SaveSnapshotAsync(snapshot with { PublishSchedules = schedules });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("Publish schedule delete save failed: {0}", ex);
                        MessageBox.Show("Il server non ha confermato l'eliminazione. La pianificazione locale è invariata.",
                            "Eliminazione non riuscita", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void ActiveCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PublishScheduleViewModel viewModel)
            {
                try
                {
                    await SaveScheduleCandidateAsync(viewModel.Schedule, viewModel.Schedule.Name);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Publish schedule active-state save failed: {0}", ex);
                    MessageBox.Show("Il server non ha confermato la modifica. La pianificazione locale è invariata.",
                        "Aggiornamento non riuscito", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadSchedules(); // Reload to reset the checkbox
                }
            }
        }

        private async Task<bool> SaveScheduleCandidateAsync(PublishSchedule schedule, string? previousName = null)
        {
            if (!CanMutateSettings())
                return false;

            try
            {
                var snapshot = await _channelSettingsSyncService.GetLocalSnapshotAsync(App.ApplicationStoppingToken);
                var schedules = snapshot.PublishSchedules
                    .Where(existing => !string.Equals(
                        existing.Name,
                        previousName ?? schedule.Name,
                        StringComparison.OrdinalIgnoreCase))
                    .Append(new ChannelPublishScheduleSnapshot(
                        schedule.Name.Trim(),
                        schedule.DaysOfWeek,
                        schedule.PublishTime,
                        schedule.IsActive))
                    .ToArray();
                return await SaveSnapshotAsync(snapshot with { PublishSchedules = schedules });
            }
            catch (OperationCanceledException) when (App.ApplicationStoppingToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError("Could not build a publish schedule snapshot: {0}", exception);
                StatusTextBlock.Text = "Impossibile preparare la pianificazione. Riprova.";
                MessageBox.Show("Impossibile preparare la pianificazione. La cache locale non è stata modificata.",
                    "Salvataggio non riuscito", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async Task<bool> SaveSnapshotAsync(ChannelConfigurationSnapshot snapshot)
        {
            if (_isSaving)
                return false;

            _isSaving = true;
            SetMutationControlsEnabled(false);
            RefreshButton.IsEnabled = false;
            CloseButton.IsEnabled = false;
            StatusTextBlock.Text = "Salvataggio delle pianificazioni...";
            try
            {
                var result = await _channelSettingsSyncService.SaveAsync(
                    App.GetCurrentChannelId(),
                    snapshot,
                    App.ApplicationStoppingToken);
                StatusTextBlock.Text = result.Message;
                if (!result.LocalCacheSynchronized)
                {
                    MessageBox.Show(result.Message, "Cache locale non sincronizzata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                LoadSchedules();
                return true;
            }
            catch (OperationCanceledException) when (App.ApplicationStoppingToken.IsCancellationRequested)
            {
                StatusTextBlock.Text = "Salvataggio annullato durante la chiusura dell'applicazione.";
                return false;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Salvataggio non riuscito. Le modifiche restano disponibili per un nuovo tentativo.";
                System.Diagnostics.Trace.TraceError("Publish schedule UI save failed: {0}", ex);
                MessageBox.Show("Il server non ha confermato il salvataggio. La cache locale è invariata.",
                    "Salvataggio non riuscito", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadSchedules();
                return false;
            }
            finally
            {
                _isSaving = false;
                SetMutationControlsEnabled(_channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId()));
                RefreshButton.IsEnabled = true;
                CloseButton.IsEnabled = true;
            }
        }

        private bool CanMutateSettings() =>
            !_isSaving && _channelSettingsSyncService.CanAttemptSaveFor(App.GetCurrentChannelId());

        private void SetMutationControlsEnabled(bool enabled)
        {
            AddScheduleButton.IsEnabled = enabled;
            SchedulesListView.IsEnabled = enabled;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSchedules();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// ViewModel for displaying PublishSchedule in the UI
    /// </summary>
    public class PublishScheduleViewModel : INotifyPropertyChanged
    {
        public PublishSchedule Schedule { get; }

        public PublishScheduleViewModel(PublishSchedule schedule)
        {
            Schedule = schedule;
        }

        public string Name => Schedule.Name;
        public bool IsActive
        {
            get => Schedule.IsActive;
            set
            {
                if (Schedule.IsActive != value)
                {
                    Schedule.IsActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DaysDisplay => WeekdayHelper.GetDisplayString(Schedule.DaysOfWeek);
        public string PublishTimeDisplay => Schedule.PublishTime.ToString(@"hh\:mm");

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
