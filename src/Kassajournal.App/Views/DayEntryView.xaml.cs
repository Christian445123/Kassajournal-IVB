using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private async void Feiertag_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { IsChecked: not null } checkBox && DataContext is DayEntryViewModel dayViewModel)
        {
            await dayViewModel.ToggleFeiertagAsync(checkBox.IsChecked.Value);
        }
    }

    private void Datum_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DayEntryViewModel dayViewModel)
        {
            dayViewModel.DatumBearbeitenCommand.Execute(null);
        }
    }
}
