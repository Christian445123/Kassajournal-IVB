using System.Windows;
using Kassajournal.App.ViewModels;

namespace Kassajournal.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        var viewModel = new SettingsViewModel();
        viewModel.Load();
        DataContext = viewModel;
    }

    private void Schliessen_Click(object sender, RoutedEventArgs e) => Close();
}
