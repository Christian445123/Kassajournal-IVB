using System.ComponentModel;
using System.Windows.Controls;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class DatabaseConnectionSettingsView : UserControl
{
    private bool _isSyncingFromViewModel;

    public DatabaseConnectionSettingsView()
    {
        InitializeComponent();

        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is DatabaseConnectionSettingsViewModel oldVm)
            {
                oldVm.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is DatabaseConnectionSettingsViewModel vm)
            {
                SetPasswordBoxFromViewModel(vm.Password);
                vm.PropertyChanged += ViewModel_PropertyChanged;
            }
        };
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Wichtig für "Zugangsdaten übernehmen": das Passwort kann sich ändern, ohne dass sich
        // die DataContext-Instanz selbst ändert (DataContextChanged würde dann nicht feuern) -
        // die PasswordBox muss das trotzdem nachziehen.
        if (e.PropertyName == nameof(DatabaseConnectionSettingsViewModel.Password) &&
            sender is DatabaseConnectionSettingsViewModel vm &&
            PasswordBox.Password != vm.Password)
        {
            SetPasswordBoxFromViewModel(vm.Password);
        }
    }

    private void SetPasswordBoxFromViewModel(string password)
    {
        _isSyncingFromViewModel = true;
        try
        {
            PasswordBox.Password = password;
        }
        finally
        {
            _isSyncingFromViewModel = false;
        }
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_isSyncingFromViewModel)
        {
            return; // Verhindert eine Rückkopplungsschleife beim programmatischen Setzen oben.
        }

        if (DataContext is DatabaseConnectionSettingsViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }
}
