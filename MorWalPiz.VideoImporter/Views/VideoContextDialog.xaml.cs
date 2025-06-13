using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using MorWalPizVideo.BackOffice.DTOs;

namespace MorWalPiz.VideoImporter.Views
{
    /// <summary>
    /// Logica di interazione per VideoContextDialog.xaml
    /// </summary>
    public partial class VideoContextDialog : Window, INotifyPropertyChanged
    {
        private bool _isLoading;
        private readonly ApiService _apiService;

        public ObservableCollection<string> SelectedFiles { get; private set; } = new ObservableCollection<string>();
        public Review ProcessingResult { get; private set; } // Add this property

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    ConfirmButton.IsEnabled = !value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public VideoContextDialog(IEnumerable<string> selectedFileNames,  string apiEndpoint)
        {
            InitializeComponent();
            DataContext = this;
            foreach (var fileName in selectedFileNames)
            {
                SelectedFiles.Add(fileName);
            }

            _apiService = new ApiService(apiEndpoint);
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string context = VideoContextTextBox.Text;
            IList<Language> languagues;
            using (var dbContext = App.DatabaseService.CreateContext())
            {
                languagues = dbContext.Languages
                                            .Where(l => !l.IsDefault && l.IsSelected)
                                            .ToList();
            }

            IsLoading = true;

            try
            {
                var translations = await _apiService.SendVideosContextAsync(SelectedFiles, context, languagues);
                ProcessingResult = translations; // Assign the result here

                System.Windows.MessageBox.Show("Dati ricevuti! Controllare traduzioni", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Si è verificato un errore: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
