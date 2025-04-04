using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using MorWalPiz.VideoImporter.Views;

namespace MorWalPiz.VideoImporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<VideoFile> VideoFiles { get; set; } = new ObservableCollection<VideoFile>();
        private List<string> selectedFolders = new List<string>();
        private List<string> selectedFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            FileListView.ItemsSource = VideoFiles;
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Seleziona una cartella contenente file MP4";

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    selectedFolders.Add(folderDialog.SelectedPath);
                    UpdateFolderPathTextBox();
                }
            }
        }

        private void UpdateFolderPathTextBox()
        {
            FolderPathTextBox.Text = string.Join("; ", selectedFolders);
        }

        private void BrowseFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "File MP4 (*.mp4)|*.mp4",
                Title = "Seleziona file MP4"
            };

            if (fileDialog.ShowDialog() == true)
            {
                foreach (var file in fileDialog.FileNames)
                {
                    selectedFiles.Add(file);
                }
                UpdateFilePathTextBox();
            }
        }

        private void UpdateFilePathTextBox()
        {
            FilePathTextBox.Text = $"{selectedFiles.Count} file selezionati";
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            VideoFiles.Clear();

            // Ottieni l'ora di pubblicazione predefinita dai settings
            TimeSpan defaultPublishTime;
            using (var context = App.DatabaseService.GetContext())
            {
                var settings = context.Settings.FirstOrDefault();
                defaultPublishTime = settings?.DefaultPublishTime ?? new TimeSpan(12, 0, 0); // Default 12:00 se non ci sono settings
            }

            // Process selected individual files
            foreach (var filePath in selectedFiles)
            {
                if (File.Exists(filePath) && Path.GetExtension(filePath).ToLower() == ".mp4")
                {
                    VideoFiles.Add(new VideoFile
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        IsSelected = false,
                        NeedTranslation = true,
                        PublishDate = DateTime.Today,
                        PublishTime = defaultPublishTime
                    });
                }
            }

            // Process files from selected folders
            foreach (var folderPath in selectedFolders)
            {
                if (Directory.Exists(folderPath))
                {
                    var mp4Files = Directory.GetFiles(folderPath, "*.mp4", SearchOption.AllDirectories);
                    foreach (var filePath in mp4Files)
                    {
                        VideoFiles.Add(new VideoFile
                        {
                            FileName = Path.GetFileName(filePath),
                            FilePath = filePath,
                            IsSelected = false,
                            NeedTranslation = true,
                            PublishDate = DateTime.Today,
                            PublishTime = defaultPublishTime
                        });
                    }
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = VideoFiles.Where(f => f.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("Nessun file selezionato!", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Sei sicuro di voler rimuovere {selectedItems.Count} file dalla lista?",
                "Conferma eliminazione",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var item in selectedItems.ToList())
                {
                    VideoFiles.Remove(item);
                }
            }
        }

        // Gestione del menu
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LanguagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var languagesPage = new LanguagesPage();
            languagesPage.Owner = this;
            languagesPage.ShowDialog();
        }

        private void DisclaimerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var disclaimerPage = new DisclaimerPage();
            disclaimerPage.Owner = this;
            disclaimerPage.ShowDialog();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsPage = new SettingsPage();
            settingsPage.Owner = this;
            settingsPage.ShowDialog();
        }

        private void FileDetailButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the video file associated with the clicked button
            if (sender is FrameworkElement element && element.DataContext is VideoFile file)
            {
                ShowFileDetailDialog(file);
            }
        }

        private void FileListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Get the clicked item from the ListView
            if (FileListView.SelectedItem is VideoFile file)
            {
                ShowFileDetailDialog(file);
            }
        }

        private void ShowFileDetailDialog(VideoFile file)
        {
            // Open the file detail dialog
            var detailPage = new Views.FileDetailPage(file);
            detailPage.Owner = this;

            // Show the dialog and process the result
            var result = detailPage.ShowDialog();

            if (result == true)
            {
                // Refresh the ListView to show any changes
                FileListView.Items.Refresh();
            }
        }
    }

    public class VideoFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool NeedTranslation { get; set; } = true;
        public string EditedCleanFileName { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; } = DateTime.Today;
        public TimeSpan PublishTime { get; set; } = TimeSpan.FromHours(0);
        public Dictionary<int, string> TranslatedTitles { get; set; } = new Dictionary<int, string>();

        // Clean filename property - replaces non-alphanumeric chars with spaces
        public string CleanFileName
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                    return string.Empty;

                return System.Text.RegularExpressions.Regex.Replace(FileName, @"[^a-zA-Z0-9]", " ");
            }
        }
    }
}