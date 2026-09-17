using System.Windows;
using System.Windows.Controls;
using MorWalPiz.Contracts.Contracts;

namespace MorWalPiz.VideoImporter.Views;

public sealed class ChannelSelectionDialog : Window
{
    public ChannelContract? SelectedChannel => ChannelBox.SelectedItem as ChannelContract;
    private readonly System.Windows.Controls.ComboBox ChannelBox = new() { Margin = new Thickness(0, 0, 0, 12), DisplayMemberPath = "ChannelName" };

    public ChannelSelectionDialog(IReadOnlyList<ChannelContract> channels)
    {
        Title = "Seleziona canale";
        Width = 420;
        Height = 170;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ChannelBox.ItemsSource = channels;
        ChannelBox.SelectedIndex = 0;

        var confirm = new System.Windows.Controls.Button { Content = "Seleziona", IsDefault = true, Padding = new Thickness(12, 5, 12, 5) };
        confirm.Click += (_, _) => { DialogResult = true; Close(); };
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "Scegli il canale BackOffice per questo tenant.", Margin = new Thickness(0, 0, 0, 8) });
        panel.Children.Add(ChannelBox);
        panel.Children.Add(confirm);
        Content = panel;
    }
}