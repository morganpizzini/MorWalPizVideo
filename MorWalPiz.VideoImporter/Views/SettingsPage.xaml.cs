using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Window
    {
        private readonly DatabaseService _databaseService;
        private Settings _currentSettings;

        public SettingsPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadSettings();
        }

        private void LoadSettings()
        {
            using (var context = _databaseService.GetContext())
            {
                _currentSettings = context.Settings.FirstOrDefault() ?? new Settings { Id = 1 };

                // Popola la TextBox degli hashtag
                HashtagsTextBox.Text = _currentSettings.DefaultHashtags;

                // Popola i campi per l'ora
                HourTextBox.Text = _currentSettings.DefaultPublishTime.Hours.ToString("00");
                MinuteTextBox.Text = _currentSettings.DefaultPublishTime.Minutes.ToString("00");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validazione dell'input dell'ora
                if (!int.TryParse(HourTextBox.Text, out int hour) || hour < 0 || hour > 23)
                {
                    System.Windows.MessageBox.Show("L'ora deve essere un numero tra 0 e 23.", "Errore di validazione", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!int.TryParse(MinuteTextBox.Text, out int minute) || minute < 0 || minute > 59)
                {
                    System.Windows.MessageBox.Show("I minuti devono essere un numero tra 0 e 59.", "Errore di validazione", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Salvataggio delle impostazioni
                using (var context = _databaseService.GetContext())
                {
                    var settings = context.Settings.FirstOrDefault() ?? new Settings { Id = 1 };

                    settings.DefaultHashtags = HashtagsTextBox.Text.Trim();
                    settings.DefaultPublishTime = new TimeSpan(hour, minute, 0);

                    if (settings.Id == 0)
                    {
                        context.Settings.Add(settings);
                    }
                    else
                    {
                        context.Settings.Update(settings);
                    }

                    context.SaveChanges();
                }

                System.Windows.MessageBox.Show("Impostazioni salvate con successo!", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Errore durante il salvataggio: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Input validation per i campi numerici
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text);
        }

        private bool IsNumeric(string text)
        {
            return int.TryParse(text, out _);
        }
    }
}