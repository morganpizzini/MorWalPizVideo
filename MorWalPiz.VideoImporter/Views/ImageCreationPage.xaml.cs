using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace MorWalPiz.VideoImporter.Views;

public partial class ImageCreationPage : Window
{
    private readonly ImageGenerationOptions _options = App.ImageGenerationOptions;
    private readonly ObservableCollection<PromptTemplate> _templates = [];
    private readonly ObservableCollection<OutputFile> _outputs = [];
    private CancellationTokenSource? _cancellationTokenSource;

    public ImageCreationPage()
    {
        InitializeComponent();
        SizeComboBox.ItemsSource = _options.SupportedSizes.Keys.OrderBy(value => value).ToList();
        SizeComboBox.SelectedIndex = 0;
        QualityTextBox.Text = _options.Quality;
        BackgroundTextBox.Text = _options.Background;
        OutputDirectoryTextBlock.Text = string.IsNullOrWhiteSpace(_options.OutputDirectory) ? "Non configurata" : _options.OutputDirectory;
        TemplateComboBox.ItemsSource = _templates;
        OutputListBox.ItemsSource = _outputs;
        LoadTemplates();
    }

    private void LoadTemplates()
    {
        _templates.Clear();
        foreach (var template in App.PromptTemplateStore.GetAll()) _templates.Add(template);
    }

    private void TemplateComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TemplateComboBox.SelectedItem is PromptTemplate template)
        {
            NameTextBox.Text = template.Name;
            PromptTextBox.Text = template.Prompt;
        }
    }

    private void SaveTemplate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ImageGenerationValidation.ValidatePrompt(NameTextBox.Text, PromptTextBox.Text);
            App.PromptTemplateStore.Save(new PromptTemplate(NameTextBox.Text.Trim(), PromptTextBox.Text.Trim()));
            LoadTemplates();
            StatusTextBlock.Text = "Template salvato localmente.";
        }
        catch (Exception exception) { ShowError(exception.Message); }
    }

    private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (TemplateComboBox.SelectedItem is PromptTemplate template)
        {
            App.PromptTemplateStore.Delete(template.Name);
            LoadTemplates();
            StatusTextBlock.Text = "Template eliminato.";
        }
    }

    private void ChooseSource_Click(object sender, RoutedEventArgs e) => ChooseFile(SourceTextBox, "Immagini|*.png;*.jpg;*.jpeg;*.webp");
    private void ChooseMask_Click(object sender, RoutedEventArgs e) => ChooseFile(MaskTextBox, "Immagini|*.png;*.jpg;*.jpeg;*.webp");

    private static void ChooseFile(System.Windows.Controls.TextBox target, string filter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = filter, Multiselect = true };
        if (dialog.ShowDialog() != true) return;
        if (dialog.FileNames.Length > 1)
        {
            WpfMessageBox.Show("L'editing supporta una sola immagine sorgente più una maschera. Seleziona un solo file.", "Riferimenti non supportati", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        target.Text = dialog.FileName;
    }

    private Task RunAsync(bool edit)
    {
        ImageGenerationValidation.ValidatePrompt(NameTextBox.Text, PromptTextBox.Text);
        var configuredSize = SizeComboBox.SelectedItem?.ToString() ?? string.Empty;
        var providerSize = ImageGenerationValidation.ResolveProviderSize(_options, configuredSize);
        var outputDirectory = ImageGenerationValidation.ValidateOutputDirectory(_options);
        if (edit)
        {
            ImageGenerationValidation.ValidateReferenceImages(string.IsNullOrWhiteSpace(SourceTextBox.Text) ? [] : [SourceTextBox.Text]);
            if (string.IsNullOrWhiteSpace(SourceTextBox.Text)) throw new ImageProviderException("Seleziona un'immagine sorgente per l'editing.");
        }
        _cancellationTokenSource = new CancellationTokenSource();
        GenerateButton.IsEnabled = EditButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        return RunRequestAsync(edit, providerSize, outputDirectory, _cancellationTokenSource.Token);
    }

    private async Task RunRequestAsync(bool edit, string providerSize, string outputDirectory, CancellationToken cancellationToken)
    {
        try
        {
            StatusTextBlock.Text = "Richiesta in corso...";
            var images = edit
                ? await App.ImageGenerationService.EditAsync(new ImageEditRequest(PromptTextBox.Text.Trim(), SourceTextBox.Text, string.IsNullOrWhiteSpace(MaskTextBox.Text) ? null : MaskTextBox.Text), cancellationToken)
                : await App.ImageGenerationService.GenerateAsync(new ImageGenerationRequest(PromptTextBox.Text.Trim(), providerSize, QualityTextBox.Text.Trim(), BackgroundTextBox.Text.Trim(), _options.OutputCompression, _options.OutputFormat, _options.Count), cancellationToken);
            var paths = await App.ImageOutputService.SaveAsync(images, outputDirectory, NameTextBox.Text, cancellationToken);
            _outputs.Clear();
            foreach (var path in paths) _outputs.Add(new OutputFile(Path.GetFileName(path), path));
            PreviewImage.Source = new BitmapImage(new Uri(paths[0]));
            StatusTextBlock.Text = $"Completato: {paths.Count} immagine/i salvata/e.";
        }
        catch (OperationCanceledException) { StatusTextBlock.Text = "Operazione annullata."; }
        catch (Exception exception) { ShowError(exception.Message); }
        finally { GenerateButton.IsEnabled = EditButton.IsEnabled = true; CancelButton.IsEnabled = false; _cancellationTokenSource?.Dispose(); _cancellationTokenSource = null; }
    }

    private async void Generate_Click(object sender, RoutedEventArgs e) { try { await RunAsync(false); } catch (Exception exception) { ShowError(exception.Message); ResetButtons(); } }
    private async void Edit_Click(object sender, RoutedEventArgs e) { try { await RunAsync(true); } catch (Exception exception) { ShowError(exception.Message); ResetButtons(); } }
    private void Cancel_Click(object sender, RoutedEventArgs e) => _cancellationTokenSource?.Cancel();
    private void ShowError(string message) { StatusTextBlock.Text = message; WpfMessageBox.Show(message, "Creazione immagini", MessageBoxButton.OK, MessageBoxImage.Error); }
    private void ResetButtons() { GenerateButton.IsEnabled = EditButton.IsEnabled = true; CancelButton.IsEnabled = false; }
    private sealed record OutputFile(string FileName, string Path);
}