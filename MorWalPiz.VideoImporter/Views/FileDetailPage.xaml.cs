using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MorWalPiz.VideoImporter.Models;
using TextBox = System.Windows.Controls.TextBox;

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

      // Imposta la descrizione
      TitleTextBox.Text = file.Title;
      DescriptionTextBox.Text = file.Description;

      // Imposta il CheckBox ContainsWeapon (Assicurati che esista un CheckBox con x:Name="ContainsWeaponCheckBox" nel XAML)
      ContainsWeaponCheckBox.IsChecked = file.containsWeapon;


      // Imposta data e ora di pubblicazione
      PublishDatePicker.SelectedDate = file.PublishDate;
      HourTextBox.Text = file.PublishTime.Hours.ToString("00");
      MinuteTextBox.Text = file.PublishTime.Minutes.ToString("00");

      // Aggiorna le traduzioni esistenti
      UpdateTranslationFields();
    }

    private void LoadSecondaryLanguages()
    {
      // Utilizza il metodo CreateContext per ottenere un nuovo contesto isolato
      using (var context = App.DatabaseService.CreateContext())
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
          // Verifica se esistono traduzioni sia nel vecchio formato che nel nuovo
          string titleTranslation = null;
          string descriptionTranslation = null;

          // Controlla prima il nuovo formato (Translations dictionary)
          if (_currentFile.Translations.TryGetValue(language.Id, out TranslationItem translationItem))
          {
            titleTranslation = translationItem.Title;
            descriptionTranslation = translationItem.Description;
          }
          // Controlla anche il vecchio formato per retrocompatibilità
          else if (_currentFile.TranslatedTitles.TryGetValue(language.Id, out string legacyTranslation))
          {
            titleTranslation = legacyTranslation;
          }

          // Aggiorna l'UI con le traduzioni trovate
          if (titleTranslation != null || descriptionTranslation != null)
          {
            // Non possiamo aggiornare direttamente le TextBox, quindi usiamo il dispatcher
            Dispatcher.InvokeAsync(() =>
            {
              // Itera sugli elementi per trovare i controlli corrispondenti alla lingua corrente
              foreach (var item in SecondaryLanguagesItemsControl.Items)
              {
                var container = SecondaryLanguagesItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null)
                {
                  var grid = VisualTreeHelper.GetChild(container, 0) as Grid;
                  if (grid != null)
                  {
                    // Cerca e aggiorna la TextBox del titolo
                    if (titleTranslation != null)
                    {
                      var titleTextBox = FindTitleTextBox(grid);
                      if (titleTextBox != null && titleTextBox.Tag.ToString() == language.Id.ToString())
                      {
                        titleTextBox.Text = titleTranslation;
                      }
                    }

                    // Cerca e aggiorna la TextBox della descrizione
                    if (descriptionTranslation != null)
                    {
                      var descriptionTextBox = FindDescriptionTextBox(grid);
                      if (descriptionTextBox != null && descriptionTextBox.Tag.ToString() == language.Id.ToString())
                      {
                        descriptionTextBox.Text = descriptionTranslation;
                      }
                    }
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

    // Metodo di utilità per trovare il grid padre di un elemento
    private Grid FindParentGrid(FrameworkElement element)
    {
      DependencyObject parent = VisualTreeHelper.GetParent(element);
      while (parent != null && !(parent is Grid))
      {
        parent = VisualTreeHelper.GetParent(parent);
      }
      return parent as Grid;
    }

    // Trova la TextBox per il titolo nel Grid
    private TextBox FindTitleTextBox(Grid grid)
    {
      foreach (var child in grid.Children)
      {
        if (child is TextBox textBox && textBox.Name == "TitleTranslationTextBox")
        {
          return textBox;
        }
      }
      return grid.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "TitleTranslationTextBox");
    }

    // Trova la TextBox per la descrizione nel Grid
    private TextBox FindDescriptionTextBox(Grid grid)
    {
      foreach (var child in grid.Children)
      {
        if (child is TextBox textBox && textBox.Name == "DescriptionTranslationTextBox")
        {
          return textBox;
        }
      }
      return grid.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "DescriptionTranslationTextBox");
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
        _currentFile.EditedCleanFileName = CleanFileNameTextBox.Text;
        _currentFile.Title = TitleTextBox.Text;
        _currentFile.Description = DescriptionTextBox.Text;
        // Salva lo stato del CheckBox ContainsWeapon
        _currentFile.containsWeapon = ContainsWeaponCheckBox.IsChecked ?? false;

        // Salva data e ora di pubblicazione
        _currentFile.PublishDate = PublishDatePicker.SelectedDate ?? DateTime.Today;
        _currentFile.PublishTime = new TimeSpan(hour, minute, 0);

        // Salva le traduzioni delle lingue secondarie
        foreach (var language in _secondaryLanguages)
        {
          int languageId = language.Id;

          // Inizializza o recupera l'oggetto TranslationItem per questa lingua
          if (!_currentFile.Translations.ContainsKey(languageId))
          {
            _currentFile.Translations[languageId] = new TranslationItem();
          }

          // Cerca gli elementi nell'interfaccia utente per ogni lingua
          foreach (var item in SecondaryLanguagesItemsControl.Items)
          {
            var container = SecondaryLanguagesItemsControl.ItemContainerGenerator.ContainerFromItem(item);
            if (container == null) continue;

            var grid = VisualTreeHelper.GetChild(container, 0) as Grid;
            if (grid == null) continue;

            // Trova e salva il titolo tradotto
            var titleTextBoxes = grid.Children.OfType<TextBox>().Where(tb => tb.Name == "TitleTranslationTextBox" &&
                                                                      tb.Tag != null &&
                                                                      tb.Tag.ToString() == languageId.ToString());
            foreach (var textBox in titleTextBoxes)
            {
              if (!string.IsNullOrWhiteSpace(textBox.Text))
              {
                _currentFile.Translations[languageId].Title = textBox.Text;
              }
            }

            // Trova e salva la descrizione tradotta
            var descTextBoxes = grid.Children.OfType<TextBox>().Where(tb => tb.Name == "DescriptionTranslationTextBox" &&
                                                                     tb.Tag != null &&
                                                                     tb.Tag.ToString() == languageId.ToString());
            foreach (var textBox in descTextBoxes)
            {
              if (!string.IsNullOrWhiteSpace(textBox.Text))
              {
                _currentFile.Translations[languageId].Description = textBox.Text;
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
    public string TranslatedDescription { get; set; }
  }
}