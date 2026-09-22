using System.Windows;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(UpdateManagerViewModel updateManager)
    {
        InitializeComponent();

        var viewModel = new SettingsViewModel(updateManager);
        viewModel.Load();
        DataContext = viewModel;
    }

    private void Schliessen_Click(object sender, RoutedEventArgs e) => Close();
}
