using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter.Views
{
  public partial class LanguagesPage : Window
  {
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private List<Language> _languages;
    private Language _defaultLanguage;

    public LanguagesPage()
    {
      InitializeComponent();
      _context = App.DatabaseService.CreateContext();
      _tenantContext = App.TenantContext;
      LoadLanguages();
    }

    private void LoadLanguages()
    {
      var currentTenantId = _tenantContext?.CurrentTenantId ?? 1;
      _languages = _context.Languages.Where(l => l.TenantId == currentTenantId).ToList();
      _defaultLanguage = _languages.FirstOrDefault(l => l.IsDefault);

      // Popola il ComboBox con tutte le lingue
      DefaultLanguageComboBox.ItemsSource = _languages;
      DefaultLanguageComboBox.SelectedItem = _defaultLanguage;

      // Popola la ListView con tutte le lingue
      LanguagesListView.ItemsSource = _languages.ToList();
    }

    private void DefaultLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      // This event is handled when the default language selection changes
      // The main saving logic is in SaveButton_Click
    }

    private void SecondaryLanguage_Click(object sender, RoutedEventArgs e)
    {
      // Gestisce la selezione/deselezione di una lingua secondaria
    }

    private void AddLanguageButton_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new LanguageEditDialog();
      dialog.Owner = this;
      if (dialog.ShowDialog() == true)
      {
        LoadLanguages(); // Ricarica la lista dopo l'aggiunta
      }
    }

    private void EditLanguageButton_Click(object sender, RoutedEventArgs e)
    {
      if (LanguagesListView.SelectedItem is Language selectedLanguage)
      {
        var dialog = new LanguageEditDialog(selectedLanguage);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
          LoadLanguages(); // Ricarica la lista dopo la modifica
        }
      }
    }

    private void DeleteLanguageButton_Click(object sender, RoutedEventArgs e)
    {
      if (LanguagesListView.SelectedItem is Language selectedLanguage)
      {
        // Verifica se la lingua è quella predefinita
        if (selectedLanguage.IsDefault)
        {
          System.Windows.MessageBox.Show("Non è possibile eliminare la lingua predefinita.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        // Verifica se ci sono disclaimer associati a questa lingua
        var disclaimersCount = _context.Disclaimers.Count(d => d.LanguageId == selectedLanguage.Id);
        if (disclaimersCount > 0)
        {
          System.Windows.MessageBox.Show($"Non è possibile eliminare questa lingua perché è utilizzata da {disclaimersCount} disclaimer.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        var result = System.Windows.MessageBox.Show($"Sei sicuro di voler eliminare la lingua '{selectedLanguage.Name}'?", 
                                    "Conferma Eliminazione", 
                                    MessageBoxButton.YesNo, 
                                    MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
          try
          {
            _context.Languages.Remove(selectedLanguage);
            _context.SaveChanges();
            LoadLanguages(); // Ricarica la lista dopo l'eliminazione
            System.Windows.MessageBox.Show("Lingua eliminata con successo!", "Eliminazione Completata", MessageBoxButton.OK, MessageBoxImage.Information);
          }
          catch (Exception ex)
          {
            System.Windows.MessageBox.Show($"Errore durante l'eliminazione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
          }
        }
      }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        // Ottieni la lingua predefinita selezionata
        var newDefaultLanguage = (Language)DefaultLanguageComboBox.SelectedItem;

        if (newDefaultLanguage == null)
        {
          System.Windows.MessageBox.Show("Seleziona una lingua predefinita.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        // Imposta la nuova lingua predefinita
        foreach (var language in _languages)
        {
          language.IsDefault = (language.Id == newDefaultLanguage.Id);
        }

        // Aggiorna le lingue selezionate dalla ListView
        var languagesFromListView = LanguagesListView.ItemsSource as IEnumerable<Language>;
        if (languagesFromListView != null)
        {
          foreach (var language in languagesFromListView)
          {
            var originalLanguage = _languages.FirstOrDefault(l => l.Id == language.Id);
            if (originalLanguage != null)
            {
              originalLanguage.IsSelected = language.IsSelected;
            }
          }
        }

        // Salva le modifiche nel database
        _context.SaveChanges();

        System.Windows.MessageBox.Show("Impostazioni lingua salvate con successo!", "Salvataggio Completato", MessageBoxButton.OK, MessageBoxImage.Information);
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

    protected override void OnClosed(EventArgs e)
    {
      _context?.Dispose();
      base.OnClosed(e);
    }
  }
}
