using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Models;
using Kassajournal.Data.Remote;
using Kassajournal.Data.Settings;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Einstellungsdialog für die Zugangsdaten der zentralen Datenbank. Wird 1x ausgefüllt;
/// die Daten werden danach DPAPI-verschlüsselt lokal gespeichert (siehe <see cref="DatabaseSettingsStore"/>)
/// und nie im Klartext irgendwo abgelegt. Die Verbindung selbst läuft immer über TLS/SSL.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly DatabaseSettingsStore _store = new();

    [ObservableProperty]
    private DatabaseEngine _engine = DatabaseEngine.MySql;

    [ObservableProperty]
    private string _host = string.Empty;

    [ObservableProperty]
    private int _port = DatabaseSettings.DefaultPortFor(DatabaseEngine.MySql);

    [ObservableProperty]
    private string _databaseName = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _requireEncryption = true;

    [ObservableProperty]
    private string _testStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _testWasSuccessful;

    [ObservableProperty]
    private bool _isTesting;

    public IReadOnlyList<DatabaseEngine> VerfuegbareEngines { get; } = Enum.GetValues<DatabaseEngine>();

    public void Load()
    {
        var settings = _store.Load();
        Engine = settings.Engine;
        Host = settings.Host;
        Port = settings.Port;
        DatabaseName = settings.DatabaseName;
        Username = settings.Username;
        Password = settings.Password;
        RequireEncryption = settings.RequireEncryption;
    }

    partial void OnEngineChanged(DatabaseEngine value)
    {
        Port = DatabaseSettings.DefaultPortFor(value);
    }

    private DatabaseSettings BuildSettings() => new()
    {
        Engine = Engine,
        Host = Host.Trim(),
        Port = Port,
        DatabaseName = DatabaseName.Trim(),
        Username = Username.Trim(),
        Password = Password,
        RequireEncryption = RequireEncryption,
    };

    [RelayCommand]
    private async Task VerbindungTestenAsync()
    {
        IsTesting = true;
        TestStatusMessage = "Verbindung wird geprüft …";
        try
        {
            var gateway = new SqlRemoteKassaGateway(BuildSettings());
            var (success, error) = await gateway.TestConnectionAsync();
            TestWasSuccessful = success;
            TestStatusMessage = success
                ? "Verbindung erfolgreich hergestellt (verschlüsselt)."
                : $"Verbindung fehlgeschlagen: {error}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private void Speichern()
    {
        _store.Save(BuildSettings());
        TestStatusMessage = "Zugangsdaten gespeichert.";
    }
}
