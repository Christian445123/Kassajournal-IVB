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
}
