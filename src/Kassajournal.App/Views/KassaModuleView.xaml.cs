using System.Windows.Controls;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class KassaModuleView : UserControl
{
    public KassaModuleView()
    {
        InitializeComponent();
    }

    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is KassaModuleViewModel vm && Tabs.SelectedIndex >= 0)
        {
            await vm.OnTabSelectedAsync(Tabs.SelectedIndex);
        }
    }
}
