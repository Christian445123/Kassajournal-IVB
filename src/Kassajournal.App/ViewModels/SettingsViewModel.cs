using CommunityToolkit.Mvvm.ComponentModel;
using Kassajournal.Core.Models;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Einstellungsdialog: enthält je einen unabhängigen Zugangsdaten-Block für die Kassajournal-Datenbank
/// (kassajournal_db) und die IVB-Datenbank (ivb_db) - komplett getrennt, wie gefordert.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel()
    {
        Kassajournal = new DatabaseConnectionSettingsViewModel(AppModule.Kassajournal);
        Ivb = new DatabaseConnectionSettingsViewModel(AppModule.Ivb);
    }

    public DatabaseConnectionSettingsViewModel Kassajournal { get; }

    public DatabaseConnectionSettingsViewModel Ivb { get; }

    public void Load()
    {
        Kassajournal.Load();
        Ivb.Load();
    }
}
