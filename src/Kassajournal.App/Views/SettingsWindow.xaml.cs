using System.Windows;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(UpdateManagerViewModel updateManager)
    {
        InitializeComponent();

        _viewModel = new SettingsViewModel(updateManager);
        _viewModel.Load();
        DataContext = _viewModel;

        // Zugangsdaten werden zusätzlich zum "Speichern"-Button beim Schließen des Fensters
        // automatisch gesichert - so geht nichts verloren, falls jemand nach dem Eintragen
        // (oder nach "Verbindung testen") direkt schließt, ohne extra auf "Speichern" zu klicken.
        Closing += (_, _) => _viewModel.SaveAll();
    }

    private void Schliessen_Click(object sender, RoutedEventArgs e) => Close();
}
