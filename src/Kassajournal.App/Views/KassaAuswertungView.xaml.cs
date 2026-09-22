using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class KassaAuswertungView : UserControl
{
    public KassaAuswertungView()
    {
        InitializeComponent();
    }

    private void MonatZeile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KassaMonatsZeileViewModel zeile } && DataContext is KassaAuswertungViewModel vm)
        {
            vm.MonatOeffnenCommand.Execute(zeile);
        }
    }
}
