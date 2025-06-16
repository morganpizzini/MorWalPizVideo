using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using CheckBox = System.Windows.Controls.CheckBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Orientation = System.Windows.Controls.Orientation;

namespace MorWalPiz.VideoImporter.Views
{
    public partial class TenantManagementPage : Page
    {
        private readonly ITenantService _tenantService;
        private readonly ITenantContext _tenantContext;

        public TenantManagementPage(ITenantService tenantService, ITenantContext tenantContext)
        {
            InitializeComponent();
            _tenantService = tenantService;
            _tenantContext = tenantContext;
            LoadTenants();
        }

        private async void LoadTenants()
        {
            try
            {
                var tenants = await _tenantService.GetAllTenantsAsync();
                TenantsDataGrid.ItemsSource = tenants;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel caricamento dei tenant: {ex.Message}", "Errore", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddTenantButton_Click(object sender, RoutedEventArgs e)
        {
            await AddTenant();
        }

        private async void NewTenantNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await AddTenant();
            }
        }

        private async System.Threading.Tasks.Task AddTenant()
        {
            var tenantName = NewTenantNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(tenantName))
            {
                MessageBox.Show("Inserire un nome per il tenant.", "Attenzione", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _tenantService.CreateTenantAsync(tenantName);
                NewTenantNameTextBox.Clear();
                LoadTenants();
                MessageBox.Show($"Tenant '{tenantName}' creato con successo.", "Successo", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nella creazione del tenant: {ex.Message}", "Errore", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditTenantButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Tenant tenant)
            {
                var dialog = new TenantEditDialog(tenant);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        await _tenantService.UpdateTenantAsync(dialog.EditedTenant);
                        LoadTenants();
                        MessageBox.Show("Tenant aggiornato con successo.", "Successo", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Errore nell'aggiornamento del tenant: {ex.Message}", "Errore", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void DeleteTenantButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Tenant tenant)
            {
                var result = MessageBox.Show(
                    $"Sei sicuro di voler eliminare il tenant '{tenant.Name}'?\n" +
                    "Questa operazione eliminerà anche tutti i dati associati al tenant.",
                    "Conferma Eliminazione",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _tenantService.DeleteTenantAsync(tenant.Id);
                        LoadTenants();
                        MessageBox.Show("Tenant eliminato con successo.", "Successo", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Errore nell'eliminazione del tenant: {ex.Message}", "Errore", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTenants();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }

    // Dialog for editing tenant details
    public partial class TenantEditDialog : Window
    {
        public Tenant EditedTenant { get; private set; }

        public TenantEditDialog(Tenant tenant)
        {
            InitializeComponent();
            EditedTenant = new Tenant
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedDate = tenant.CreatedDate,
                IsActive = tenant.IsActive
            };
            DataContext = EditedTenant;
        }

        private void InitializeComponent()
        {
            Title = "Modifica Tenant";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Nome
            var nameLabel = new TextBlock { Text = "Nome:", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(nameLabel, 0);
            var nameTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 15) };
            nameTextBox.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("Name"));
            Grid.SetRow(nameTextBox, 1);

            // Attivo
            var activeCheckBox = new CheckBox { Content = "Attivo", Margin = new Thickness(0, 0, 0, 15) };
            activeCheckBox.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("IsActive"));
            Grid.SetRow(activeCheckBox, 2);

            // Bottoni
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            var okButton = new Button 
            { 
                Content = "OK", 
                Padding = new Thickness(15, 5, 15, 5), 
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };
            
            var cancelButton = new Button 
            { 
                Content = "Annulla", 
                Padding = new Thickness(15, 5, 15, 5),
                IsCancel = true
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 4);

            grid.Children.Add(nameLabel);
            grid.Children.Add(nameTextBox);
            grid.Children.Add(activeCheckBox);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}
