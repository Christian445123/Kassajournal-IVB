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

    /// <summary>
    /// Beim Klicken in ein leeres Feld (noch "0,00") verschwindet die 0,00 sofort, damit man direkt
    /// lostippen kann. Steht bereits ein echter Betrag drin, wird er markiert, damit ein Tastendruck
    /// ihn komplett ersetzt.
    /// </summary>
    private void AmountTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: CategoryLineViewModel line } textBox)
        {
            return;
        }

        if (line.Amount == 0m)
        {
            textBox.Clear();
        }
        else
        {
            textBox.SelectAll();
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
