using System.Windows;
using Kassajournal.App.ViewModels;
using Kassajournal.App.Views;

namespace Kassajournal.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();

        // Erster Start (oder noch nicht eingerichtet): lokale, verschlüsselte Datenbank existiert
        // bereits automatisch - jetzt fehlen nur noch die Zugangsdaten zu den Zentraldatenbanken.
        if (!_viewModel.IstKassaDbKonfiguriert && !_viewModel.IstIvbDbKonfiguriert)
        {
            MessageBox.Show(
                this,
                "Willkommen bei Kassajournal & IVB!\n\n" +
                "Die lokale, verschlüsselte Datenbank auf diesem PC wurde bereits automatisch angelegt.\n" +
                "Bitte trage jetzt noch einmalig die Zugangsdaten zu den beiden Zentraldatenbanken " +
                "(Kassajournal und IVB) ein, damit deine Daten zusätzlich dort gesichert werden.",
                "Ersteinrichtung",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            OpenSettings();
        }
    }

    private void Einstellungen_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();

        // Nach dem Schließen der Einstellungen den DB-Status-Hinweis aktualisieren (falls neu konfiguriert).
        _viewModel.RefreshDbStatus();
    }
}
