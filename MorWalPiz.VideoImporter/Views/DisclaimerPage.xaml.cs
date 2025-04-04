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
    public partial class DisclaimerPage : Window
    {
        private readonly AppDbContext _context;
        private List<Language> _selectedLanguages;
        private Dictionary<int, Disclaimer> _disclaimers = new Dictionary<int, Disclaimer>();
        private bool _isInitializing = true; // Flag per tracciare l'inizializzazione

        public DisclaimerPage()
        {
            InitializeComponent();
            _context = App.DatabaseService.GetContext();
            LoadData();
        }

        private void LoadData()
        {
            // Carica solo le lingue selezionate (predefinita + secondarie selezionate)
            _selectedLanguages = _context.Languages
                .Where(l => l.IsDefault || l.IsSelected)
                .ToList();

            // Carica i disclaimer esistenti e li inserisce nel dictionary per l'accesso rapido
            var existingDisclaimers = _context.Disclaimers
                .Where(d => _selectedLanguages.Select(l => l.Id).Contains(d.LanguageId))
                .ToList();

            foreach (var disclaimer in existingDisclaimers)
            {
                _disclaimers[disclaimer.LanguageId] = disclaimer;
            }

            // Imposta la fonte dati per il selettore di lingua
            LanguageSelector.ItemsSource = _selectedLanguages;

            // Seleziona la lingua predefinita
            var defaultLanguage = _selectedLanguages.FirstOrDefault(l => l.IsDefault);
            if (defaultLanguage != null)
            {
                LanguageSelector.SelectedItem = defaultLanguage;
            }
            
            // Carica il testo del disclaimer per la lingua predefinita
            if (defaultLanguage != null)
            {
                LoadDisclaimerText(defaultLanguage.Id);
            }
            
            // Imposta il flag a false dopo l'inizializzazione completa
            _isInitializing = false;
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ignora l'evento se siamo nella fase di inizializzazione
            if (_isInitializing)
                return;
                
            if (LanguageSelector.SelectedItem != null)
            {
                // Salva il testo corrente se è stato modificato
                //SaveCurrentText();

                // Carica il testo per la lingua selezionata
                var selectedLanguage = (Language)LanguageSelector.SelectedItem;
                LoadDisclaimerText(selectedLanguage.Id);
            }
        }

        private void LoadDisclaimerText(int languageId)
        {
            // Verifica se esiste già un disclaimer per questa lingua
            if (_disclaimers.TryGetValue(languageId, out var disclaimer))
            {
                DisclaimerTextBox.Text = disclaimer.Text;
            }
            else
            {
                DisclaimerTextBox.Text = string.Empty;
            }
        }

        private void SaveCurrentText()
        {
            if (LanguageSelector.SelectedItem != null)
            {
                var currentLanguage = (Language)LanguageSelector.SelectedItem;
                var currentText = DisclaimerTextBox.Text?.Trim() ?? string.Empty;

                // Se esiste già un disclaimer, aggiorna il testo
                if (_disclaimers.TryGetValue(currentLanguage.Id, out var disclaimer))
                {
                    disclaimer.Text = currentText;
                }
                // Altrimenti crea un nuovo disclaimer se il testo non è vuoto
                else if (!string.IsNullOrEmpty(currentText))
                {
                    var newDisclaimer = new Disclaimer
                    {
                        LanguageId = currentLanguage.Id,
                        Text = currentText
                    };
                    _disclaimers[currentLanguage.Id] = newDisclaimer;
                    _context.Disclaimers.Add(newDisclaimer);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Salva il testo corrente
                SaveCurrentText();

                // Salva tutte le modifiche nel database
                _context.SaveChanges();

                System.Windows.MessageBox.Show("Disclaimer salvati con successo!", "Salvataggio Completato", MessageBoxButton.OK, MessageBoxImage.Information);
                //DialogResult = true;
                //Close();
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