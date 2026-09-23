using System.Windows;
using System.Windows.Controls;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class IvbDayRowView : UserControl
{
    public IvbDayRowView()
    {
        InitializeComponent();
    }

    private async void AmountTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: IvbDayRowViewModel row })
        {
            if (row.TryCommitAmountText())
            {
                await row.CommitAsync();
            }
        }
    }

    /// <summary>Siehe DayEntryView.AmountTextBox_GotFocus - gleiches Verhalten für das IVB-Feld.</summary>
    private void AmountTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: IvbDayRowViewModel row } textBox)
        {
            return;
        }

        if (row.Amount == 0m)
        {
            textBox.Clear();
        }
        else
        {
            textBox.SelectAll();
        }
    }
}
