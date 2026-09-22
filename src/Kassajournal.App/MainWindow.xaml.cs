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
    }

    private async void Einstellungen_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();

        // Nach dem Schließen der Einstellungen ggf. neu laden (z. B. DB-Status-Hinweis aktualisieren).
        await _viewModel.InitializeAsync();
    }
}
