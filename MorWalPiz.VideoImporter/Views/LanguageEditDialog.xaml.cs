using System;
using System.Linq;
using System.Windows;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;

namespace MorWalPiz.VideoImporter.Views
{
    public partial class LanguageEditDialog : Window
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;
        private Language _language;
        private bool _isEditMode;

        public Language Language => _language;

        public LanguageEditDialog(Language language = null)
        {
            InitializeComponent();
            _context = App.DatabaseService.CreateContext();
            _tenantContext = App.TenantContext;
            
            _isEditMode = language != null;
            _language = language ?? new Language();

            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Title = _isEditMode ? "Modifica Lingua" : "Aggiungi Lingua";
            
            if (_isEditMode)
            {
                CodeTextBox.Text = _language.Code;
                NameTextBox.Text = _language.Name;
                IsSelectedCheckBox.IsChecked = _language.IsSelected;
            }
            else
            {
                // Set default tenant for new languages
                _language.TenantId = _tenantContext?.CurrentTenantId ?? 1;
            }

            CodeTextBox.Focus();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                try
                {
                    _language.Code = CodeTextBox.Text.Trim();
                    _language.Name = NameTextBox.Text.Trim();
                    _language.IsSelected = IsSelectedCheckBox.IsChecked ?? false;

                    if (!_isEditMode)
                    {
                        _language.TenantId = _tenantContext?.CurrentTenantId ?? 1;
                        _context.Languages.Add(_language);
                    }
                    else
                    {
                        _context.Languages.Update(_language);
                    }

                    // If this language is set as default, remove default from other languages
                    if (_language.IsDefault)
                    {
                        var otherLanguages = _context.Languages
                            .Where(l => l.Id != _language.Id && l.TenantId == _language.TenantId && l.IsDefault)
                            .ToList();

                        foreach (var lang in otherLanguages)
                        {
                            lang.IsDefault = false;
                        }
                    }

                    _context.SaveChanges();
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    ShowError($"Errore durante il salvataggio: {ex.Message}");
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                ShowError("Il codice lingua è obbligatorio.");
                CodeTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ShowError("Il nome lingua è obbligatorio.");
                NameTextBox.Focus();
                return false;
            }

            // Check for duplicate code
            var currentTenantId = _tenantContext?.CurrentTenantId ?? 1;
            var existingLanguage = _context.Languages
                .FirstOrDefault(l => l.Code.ToLower() == CodeTextBox.Text.Trim().ToLower() 
                                && l.TenantId == currentTenantId
                                && l.Id != _language.Id);

            if (existingLanguage != null)
            {
                ShowError("Esiste già una lingua con questo codice.");
                CodeTextBox.Focus();
                return false;
            }

            // Check for duplicate name
            var existingNameLanguage = _context.Languages
                .FirstOrDefault(l => l.Name.ToLower() == NameTextBox.Text.Trim().ToLower() 
                                && l.TenantId == currentTenantId
                                && l.Id != _language.Id);

            if (existingNameLanguage != null)
            {
                ShowError("Esiste già una lingua con questo nome.");
                NameTextBox.Focus();
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}
