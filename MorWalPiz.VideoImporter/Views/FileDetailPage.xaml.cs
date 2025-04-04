using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MorWalPiz.VideoImporter.Models;

namespace MorWalPiz.VideoImporter.Views
{
  public partial class FileDetailPage : Window
  {
    private VideoFile _currentFile;
    private List<Language> _secondaryLanguages;
    private static readonly HttpClient _httpClient = new HttpClient();

    public FileDetailPage(VideoFile file)
    {
      InitializeComponent();
      _currentFile = file;

      // Carica le lingue secondarie dal database
      LoadSecondaryLanguages();

      // Popola i campi
      OriginalFileNameTextBox.Text = file.FileName;

      // Mostra il nome pulito modificato se esiste, altrimenti il nome pulito originale
      CleanFileNameTextBox.Text = !string.IsNullOrEmpty(file.EditedCleanFileName)
                                ? file.EditedCleanFileName
                                : file.CleanFileName;

      NeedTranslationCheckBox.IsChecked = file.NeedTranslation;

      // Imposta data e ora di pubblicazione
      PublishDatePicker.SelectedDate = file.PublishDate;
      HourTextBox.Text = file.PublishTime.Hours.ToString("00");
      MinuteTextBox.Text = file.PublishTime.Minutes.ToString("00");

      // Aggiorna le traduzioni esistenti
      UpdateTranslationFields();
    }

    private void LoadSecondaryLanguages()
    {
      using (var context = App.DatabaseService.GetContext())
      {
        // Carica solo le lingue secondarie selezionate
        _secondaryLanguages = context.Languages
            .Where(l => !l.IsDefault && l.IsSelected)
            .ToList();

        // Nasconde il gruppo delle traduzioni se non ci sono lingue secondarie
        TranslationsGroupBox.Visibility = _secondaryLanguages.Any() ? Visibility.Visible : Visibility.Collapsed;

        // Imposta il source per l'ItemsControl delle lingue secondarie
        SecondaryLanguagesItemsControl.ItemsSource = _secondaryLanguages;
      }
    }

    private void UpdateTranslationFields()
    {
      // Questo metodo deve essere chiamato dopo che l'ItemsControl è stato completamente caricato
      if (_secondaryLanguages != null && SecondaryLanguagesItemsControl.ItemsSource != null)
      {
        // Aggiorniamo le TextBox con le traduzioni esistenti
        foreach (var language in _secondaryLanguages)
        {
          if (_currentFile.TranslatedTitles.TryGetValue(language.Id, out string translation))
          {
            // Trova la TextBox corrispondente a questa lingua
            // Non possiamo farlo direttamente, quindi cerchiamo dopo il rendering completo
            Dispatcher.InvokeAsync(() =>
            {
              foreach (var item in SecondaryLanguagesItemsControl.Items)
              {
                var container = SecondaryLanguagesItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null)
                {
                  var textBox = FindVisualChild<System.Windows.Controls.TextBox>(container);
                  if (textBox != null && textBox.Tag != null && textBox.Tag.ToString() == language.Id.ToString())
                  {
                    textBox.Text = translation;
                  }
                }
              }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
          }
        }
      }
    }

    private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
      {
        DependencyObject child = VisualTreeHelper.GetChild(obj, i);
        if (child != null && child is T)
          return (T)child;

        T childOfChild = FindVisualChild<T>(child);
        if (childOfChild != null)
          return childOfChild;
      }
      return null;
    }

    private void CleanFileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      // Si potrebbe aggiungere qui la validazione del testo inserito se necessario
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      // Accetta solo caratteri numerici
      e.Handled = !IsNumeric(e.Text);
    }

    private bool IsNumeric(string text)
    {
      return int.TryParse(text, out _);
    }

    private async void TranslateButton_Click(object sender, RoutedEventArgs e)
    {
      // Otteniamo l'ID della lingua dal Tag del bottone
      if (sender is System.Windows.Controls.Button button && button.Tag != null && int.TryParse(button.Tag.ToString(), out int languageId))
      {
        try
        {
          // Trova la TextBox corrispondente a questa lingua
          var grid = button.Parent as Grid;
          if (grid != null)
          {
            var textBox = grid.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
            if (textBox != null)
            {
              button.IsEnabled = false;
              button.Content = "Traduco...";

              // Utilizziamo il nome pulito per la traduzione
              string titleToTranslate = !string.IsNullOrEmpty(_currentFile.EditedCleanFileName)
                                      ? _currentFile.EditedCleanFileName
                                      : _currentFile.CleanFileName;

              // Prepara il corpo della chiamata API
              var requestBody = new { title = titleToTranslate };

              // Effettua la chiamata API
              try
              {
                var response = await _httpClient.PostAsJsonAsync("https://localhost:5000/api/pippo", requestBody);

                if (response.IsSuccessStatusCode)
                {
                  var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
                  if (result != null && !string.IsNullOrEmpty(result.TranslatedTitle))
                  {
                    textBox.Text = result.TranslatedTitle;

                    // Memorizza la traduzione nell'oggetto VideoFile
                    _currentFile.TranslatedTitles[languageId] = result.TranslatedTitle;
                  }
                  else
                  {
                    System.Windows.MessageBox.Show("Risposta API non valida.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                  }
                }
                else
                {
                  System.Windows.MessageBox.Show($"Errore API: {response.StatusCode}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
              }
              catch (Exception ex)
              {
                System.Windows.MessageBox.Show($"Errore di connessione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
              }
              finally
              {
                button.IsEnabled = true;
                button.Content = "Traduci";
              }
            }
          }
        }
        catch (Exception ex)
        {
          System.Windows.MessageBox.Show($"Errore durante la traduzione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
      // Ripristina il nome pulito all'originale
      CleanFileNameTextBox.Text = _currentFile.CleanFileName;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        // Validazione dell'ora
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

        // Salva le proprietà
        _currentFile.NeedTranslation = NeedTranslationCheckBox.IsChecked ?? true;
        _currentFile.EditedCleanFileName = CleanFileNameTextBox.Text;

        // Salva data e ora di pubblicazione
        _currentFile.PublishDate = PublishDatePicker.SelectedDate ?? DateTime.Today;
        _currentFile.PublishTime = new TimeSpan(hour, minute, 0);

        // Salva le traduzioni delle lingue secondarie
        foreach (var item in SecondaryLanguagesItemsControl.Items)
        {
          var container = SecondaryLanguagesItemsControl.ItemContainerGenerator.ContainerFromItem(item);
          if (container != null)
          {
            var grid = VisualTreeHelper.GetChild(container, 0) as Grid;
            if (grid != null)
            {
              var textBox = grid.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
              if (textBox != null && textBox.Tag != null && int.TryParse(textBox.Tag.ToString(), out int languageId))
              {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                  _currentFile.TranslatedTitles[languageId] = textBox.Text;
                }
              }
            }
          }
        }

        // Chiudi la finestra di dialogo con successo
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
      // Chiudi la finestra di dialogo senza salvare
      DialogResult = false;
      Close();
    }
  }

  // Classe per la deserializzazione della risposta API
  class TranslationResponse
  {
    public string TranslatedTitle { get; set; }
  }
}