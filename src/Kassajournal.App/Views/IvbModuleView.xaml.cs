using System.Windows.Controls;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class IvbModuleView : UserControl
{
    public IvbModuleView()
    {
        InitializeComponent();
    }

    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is IvbModuleViewModel vm && Tabs.SelectedIndex >= 0)
        {
            await vm.OnTabSelectedAsync(Tabs.SelectedIndex);
        }
    }
}
