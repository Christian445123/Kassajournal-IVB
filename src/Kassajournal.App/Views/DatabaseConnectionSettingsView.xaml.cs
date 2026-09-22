using System.Windows.Controls;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class DatabaseConnectionSettingsView : UserControl
{
    public DatabaseConnectionSettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is DatabaseConnectionSettingsViewModel vm)
            {
                PasswordBox.Password = vm.Password;
            }
        };
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is DatabaseConnectionSettingsViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }
}
