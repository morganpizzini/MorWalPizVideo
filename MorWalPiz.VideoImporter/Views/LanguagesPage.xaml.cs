using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;

namespace MorWalPiz.VideoImporter.Views
{
  public partial class LanguagesPage : Window
  {
    private readonly AppDbContext _context;
    private List<Language> _languages;
    private Language _defaultLanguage;

    public LanguagesPage()
    {
      InitializeComponent();
      _context = App.DatabaseService.GetContext();
      LoadLanguages();
    }

    private void LoadLanguages()
    {
      _languages = _context.Languages.ToList();
      _defaultLanguage = _languages.FirstOrDefault(l => l.IsDefault);

      // Popola il ComboBox con tutte le lingue
      DefaultLanguageComboBox.ItemsSource = _languages;
      DefaultLanguageComboBox.SelectedItem = _defaultLanguage;

      // Popola la ListView con le lingue che possono essere selezionate come secondarie
      SecondaryLanguagesListView.ItemsSource = _languages.Where(l => !l.IsDefault).ToList();
    }

    private void DefaultLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (DefaultLanguageComboBox.SelectedItem != null)
      {
        var selectedLanguage = (Language)DefaultLanguageComboBox.SelectedItem;

        // Aggiorna la ListView rimuovendo la lingua selezionata come default
        SecondaryLanguagesListView.ItemsSource = _languages.Where(l => l.Id != selectedLanguage.Id).ToList();
      }
    }

    private void SecondaryLanguage_Click(object sender, RoutedEventArgs e)
    {
      // Gestisce la selezione/deselezione di una lingua secondaria
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        // Ottieni la lingua predefinita selezionata
        var newDefaultLanguage = (Language)DefaultLanguageComboBox.SelectedItem;

        // Imposta la nuova lingua predefinita
        foreach (var language in _languages)
        {
          language.IsDefault = (language.Id == newDefaultLanguage.Id);
        }

        // Aggiorna le lingue selezionate
        var secondaryLanguages = SecondaryLanguagesListView.ItemsSource as IEnumerable<Language>;
        if (secondaryLanguages != null)
        {
          foreach (var language in secondaryLanguages)
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
  }
}