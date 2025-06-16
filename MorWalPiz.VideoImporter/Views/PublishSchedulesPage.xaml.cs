using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
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
        public ObservableCollection<PublishScheduleViewModel> Schedules { get; set; }

        public PublishSchedulesPage()
        {
            InitializeComponent();
            _scheduleService = new PublishScheduleService(App.DatabaseService);
            Schedules = new ObservableCollection<PublishScheduleViewModel>();
            SchedulesListView.ItemsSource = Schedules;
            LoadSchedules();
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
                MessageBox.Show($"Errore durante il caricamento delle pianificazioni: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            var editDialog = new ScheduleEditDialog();
            editDialog.Owner = this;
            
            if (editDialog.ShowDialog() == true)
            {
                try
                {
                    _scheduleService.SaveSchedule(editDialog.Schedule);
                    LoadSchedules();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore durante il salvataggio: {ex.Message}",
                        "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PublishScheduleViewModel viewModel)
            {
                var editDialog = new ScheduleEditDialog(viewModel.Schedule);
                editDialog.Owner = this;
                
                if (editDialog.ShowDialog() == true)
                {
                    try
                    {
                        _scheduleService.SaveSchedule(editDialog.Schedule);
                        LoadSchedules();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Errore durante il salvataggio: {ex.Message}",
                            "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteScheduleButton_Click(object sender, RoutedEventArgs e)
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
                        _scheduleService.DeleteSchedule(viewModel.Schedule.Id);
                        LoadSchedules();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Errore durante l'eliminazione: {ex.Message}",
                            "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ActiveCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PublishScheduleViewModel viewModel)
            {
                try
                {
                    _scheduleService.SaveSchedule(viewModel.Schedule);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore durante l'aggiornamento: {ex.Message}",
                        "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadSchedules(); // Reload to reset the checkbox
                }
            }
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
