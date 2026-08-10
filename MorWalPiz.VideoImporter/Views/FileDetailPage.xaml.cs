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
    public FileDetailPage(VideoFile file)
    {
      InitializeComponent();
      _currentFile = file;
      Loaded += (_, _) => UpdateTranslationFields();

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
      TagsTextBox.Text = file.Tags;
      // Imposta il CheckBox ContainsWeapon (Assicurati che esista un CheckBox con x:Name="ContainsWeaponCheckBox" nel XAML)
      ContainsWeaponCheckBox.IsChecked = file.containsWeapon;


      // Imposta data e ora di pubblicazione
      PublishDatePicker.SelectedDate = file.PublishDate;
      HourTextBox.Text = file.PublishTime.Hours.ToString("00");
      MinuteTextBox.Text = file.PublishTime.Minutes.ToString("00");

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
      if (_secondaryLanguages != null && SecondaryLanguagesItemsControl.ItemsSource != null)
      {
        foreach (var language in _secondaryLanguages)
        {
          var titleTranslation = string.Empty;
          var descriptionTranslation = string.Empty;

          if (_currentFile.Translations.TryGetValue(language.Id, out TranslationItem translationItem))
          {
            titleTranslation = translationItem.Title;
            descriptionTranslation = translationItem.Description;
          }
          else if (_currentFile.TranslatedTitles.TryGetValue(language.Id, out string legacyTranslation))
          {
            titleTranslation = legacyTranslation;
          }

          var container = SecondaryLanguagesItemsControl.ItemContainerGenerator.ContainerFromItem(language);
          if (container == null)
          {
            continue;
          }

          var titleTextBox = FindTextBox(container, "TitleTranslationTextBox");
          if (titleTextBox != null)
          {
            titleTextBox.Text = titleTranslation;
          }

          var descriptionTextBox = FindTextBox(container, "DescriptionTranslationTextBox");
          if (descriptionTextBox != null)
          {
            descriptionTextBox.Text = descriptionTranslation;
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

    private TextBox FindTextBox(DependencyObject container, string name)
    {
      return FindVisualChild<TextBox>(container) is TextBox textBox && textBox.Name == name
        ? textBox
        : FindVisualChildren<TextBox>(container).FirstOrDefault(item => item.Name == name);
    }

    private IEnumerable<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
    {
      for (int index = 0; index < VisualTreeHelper.GetChildrenCount(obj); index++)
      {
        var child = VisualTreeHelper.GetChild(obj, index);
        if (child is T typedChild)
        {
          yield return typedChild;
        }

        foreach (var descendant in FindVisualChildren<T>(child))
        {
          yield return descendant;
        }
      }
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

            var titleTextBox = FindTextBox(container, "TitleTranslationTextBox");
            if (titleTextBox?.Tag?.ToString() == languageId.ToString())
            {
              _currentFile.Translations[languageId].Title = titleTextBox.Text;
            }

            var descriptionTextBox = FindTextBox(container, "DescriptionTranslationTextBox");
            if (descriptionTextBox?.Tag?.ToString() == languageId.ToString())
            {
              _currentFile.Translations[languageId].Description = descriptionTextBox.Text;
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