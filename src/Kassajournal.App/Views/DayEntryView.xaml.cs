using System.Windows;
using System.Windows.Controls;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class DayEntryView : UserControl
{
    public DayEntryView()
    {
        InitializeComponent();
    }

    private async void AmountTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: CategoryLineViewModel line } && DataContext is DayEntryViewModel dayViewModel)
        {
            if (line.TryCommitAmountText())
            {
                await dayViewModel.CommitLineAsync(line);
            }
        }
    }
}
