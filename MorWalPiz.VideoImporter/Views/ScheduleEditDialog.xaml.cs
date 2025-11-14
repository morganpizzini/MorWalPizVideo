using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter.Views
{
    /// <summary>
    /// Interaction logic for ScheduleEditDialog.xaml
    /// </summary>
    public partial class ScheduleEditDialog : Window
    {
        private readonly PublishScheduleService _scheduleService;
        public PublishSchedule Schedule { get; private set; }

        public ScheduleEditDialog() : this(new PublishSchedule())
        {
        }

        public ScheduleEditDialog(PublishSchedule schedule)
        {
            InitializeComponent();
            _scheduleService = new PublishScheduleService(App.DatabaseService);
            Schedule = new PublishSchedule
            {
                Id = schedule.Id,
                Name = schedule.Name,
                DaysOfWeek = schedule.DaysOfWeek,
                PublishTime = schedule.PublishTime,
                IsActive = schedule.IsActive,
                CreatedDate = schedule.CreatedDate == default ? DateTime.Now : schedule.CreatedDate
            };

            LoadScheduleData();
        }

        private void LoadScheduleData()
        {
            NameTextBox.Text = Schedule.Name;
            HourTextBox.Text = Schedule.PublishTime.Hours.ToString("00");
            MinuteTextBox.Text = Schedule.PublishTime.Minutes.ToString("00");

            // Set checkboxes based on the bitmask
            MondayCheckBox.IsChecked = WeekdayHelper.HasDay(Schedule.DaysOfWeek, DayOfWeek.Monday);
            TuesdayCheckBox.IsChecked = WeekdayHelper.HasDay(Schedule.DaysOfWeek, DayOfWeek.Tuesday);
            WednesdayCheckBox.IsChecked = WeekdayHelper.HasDay(Schedule.DaysOfWeek, DayOfWeek.Wednesday);
            ThursdayCheckBox.IsChecked = WeekdayHelper.HasDay(Schedule.DaysOfWeek, DayOfWeek.Thursday);
            FridayCheckBox.IsChecked = WeekdayHelper.HasDay(Schedule.DaysOfWeek, DayOfWeek.Friday);
            SaturdayCheckBox.IsChecked = WeekdayHelper.HasDay(Schedule.DaysOfWeek, DayOfWeek.Saturday);
            SundayCheckBox.IsChecked = WeekdayHelper.HasDay(Schedule.DaysOfWeek, DayOfWeek.Sunday);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate name
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    System.Windows.MessageBox.Show("Il nome della pianificazione è obbligatorio.", 
                        "Errore di validazione", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Validate time
                if (!int.TryParse(HourTextBox.Text, out int hour) || hour < 0 || hour > 23)
                {
                    System.Windows.MessageBox.Show("L'ora deve essere un numero tra 0 e 23.", 
                        "Errore di validazione", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!int.TryParse(MinuteTextBox.Text, out int minute) || minute < 0 || minute > 59)
                {
                    System.Windows.MessageBox.Show("I minuti devono essere un numero tra 0 e 59.", 
                        "Errore di validazione", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Validate days selection
                var selectedDays = GetSelectedDays();
                if (!selectedDays.Any())
                {
                    System.Windows.MessageBox.Show("Deve essere selezionato almeno un giorno della settimana.", 
                        "Errore di validazione", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Update schedule
                Schedule.Name = NameTextBox.Text.Trim();
                Schedule.PublishTime = new TimeSpan(hour, minute, 0);
                Schedule.DaysOfWeek = WeekdayHelper.CreateBitmask(selectedDays);

                // Validate using service
                var (isValid, errorMessage) = _scheduleService.ValidateSchedule(Schedule);
                if (!isValid)
                {
                    System.Windows.MessageBox.Show(errorMessage, 
                        "Errore di validazione", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Errore durante il salvataggio: {ex.Message}", 
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DayOfWeek[] GetSelectedDays()
        {
            var days = new System.Collections.Generic.List<DayOfWeek>();

            if (MondayCheckBox.IsChecked == true) days.Add(DayOfWeek.Monday);
            if (TuesdayCheckBox.IsChecked == true) days.Add(DayOfWeek.Tuesday);
            if (WednesdayCheckBox.IsChecked == true) days.Add(DayOfWeek.Wednesday);
            if (ThursdayCheckBox.IsChecked == true) days.Add(DayOfWeek.Thursday);
            if (FridayCheckBox.IsChecked == true) days.Add(DayOfWeek.Friday);
            if (SaturdayCheckBox.IsChecked == true) days.Add(DayOfWeek.Saturday);
            if (SundayCheckBox.IsChecked == true) days.Add(DayOfWeek.Sunday);

            return days.ToArray();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numeric input
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
                return;
            }

            // Additional validation for hour and minute fields
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                string currentText = textBox.Text;
                string newText = currentText.Substring(0, textBox.SelectionStart) + e.Text + 
                    currentText.Substring(textBox.SelectionStart + textBox.SelectionLength);

                if (int.TryParse(newText, out int value))
                {
                    if (textBox == HourTextBox && (value < 0 || value > 23))
                    {
                        e.Handled = true;
                    }
                    else if (textBox == MinuteTextBox && (value < 0 || value > 59))
                    {
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
