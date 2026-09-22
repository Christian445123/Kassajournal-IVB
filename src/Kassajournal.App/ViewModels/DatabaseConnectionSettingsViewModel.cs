using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kassajournal.Core.Models;
using Kassajournal.Data.Remote;
using Kassajournal.Data.Settings;

namespace Kassajournal.App.ViewModels;

/// <summary>
/// Zugangsdaten-Einstellungen für EINE Zentraldatenbank (Kassajournal ODER IVB - nie beides gemischt).
/// Wird 1x ausgefüllt; danach DPAPI-verschlüsselt lokal gespeichert. Verbindung läuft immer über TLS/SSL.
/// </summary>
public partial class DatabaseConnectionSettingsViewModel : ObservableObject
{
    private readonly DatabaseSettingsStore _store;

    public DatabaseConnectionSettingsViewModel(AppModule module)
    {
        Module = module;
        Titel = module.DisplayName();
        _store = new DatabaseSettingsStore(module);
        DatabaseName = module.DefaultDatabaseName();
    }

    public AppModule Module { get; }

    public string Titel { get; }

    [ObservableProperty]
    private DatabaseEngine _engine = DatabaseEngine.MySql;

    [ObservableProperty]
    private string _host = string.Empty;

    [ObservableProperty]
    private int _port = DatabaseSettings.DefaultPortFor(DatabaseEngine.MySql);

    [ObservableProperty]
    private string _databaseName;

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

    [ObservableProperty]
    private bool _isConfigured;

    public IReadOnlyList<DatabaseEngine> VerfuegbareEngines { get; } = Enum.GetValues<DatabaseEngine>();

    public void Load()
    {
        var settings = _store.Load();
        Engine = settings.Engine;
        Host = settings.Host;
        Port = settings.Port;
        DatabaseName = string.IsNullOrWhiteSpace(settings.DatabaseName) ? Module.DefaultDatabaseName() : settings.DatabaseName;
        Username = settings.Username;
        Password = settings.Password;
        RequireEncryption = settings.RequireEncryption;
        IsConfigured = settings.IsConfigured;
    }

    partial void OnEngineChanged(DatabaseEngine value) => Port = DatabaseSettings.DefaultPortFor(value);

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
        IsConfigured = true;
        TestStatusMessage = "Zugangsdaten gespeichert.";
    }
}
