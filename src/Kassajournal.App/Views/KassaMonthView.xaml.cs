using System.Windows.Input;
using System.Windows.Controls;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class KassaMonthView : UserControl
{
    public KassaMonthView()
    {
        InitializeComponent();
    }

    private void Anfangssaldo_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is KassaMonthViewModel vm)
        {
            vm.AnfangssaldoBearbeitenCommand.Execute(null);
        }
    }
}
