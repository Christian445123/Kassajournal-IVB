using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class IvbAuswertungView : UserControl
{
    public IvbAuswertungView()
    {
        InitializeComponent();
    }

    private void MonatZeile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KassaMonatsZeileViewModel zeile } && DataContext is IvbAuswertungViewModel vm)
        {
            vm.MonatOeffnenCommand.Execute(zeile);
        }
    }
}
