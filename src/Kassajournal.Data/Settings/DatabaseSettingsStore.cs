using System.Text.Json;
using Kassajournal.Core;
using Kassajournal.Core.Models;

namespace Kassajournal.Data.Settings;

/// <summary>
/// Lädt/speichert die Datenbank-Zugangsdaten je Bereich (Kassajournal / IVB) aus den Programm-Einstellungen.
/// Werden 1x vom Benutzer im Einstellungsdialog eingegeben (siehe Anforderung) und danach getrennt je
/// Bereich DPAPI-verschlüsselt lokal abgelegt – nie im Klartext, nie in der Zentraldatenbank selbst.
/// Kassajournal- und IVB-Daten landen dadurch garantiert nie in derselben Datenbank.
/// </summary>
public class DatabaseSettingsStore(AppModule module)
{
    public DatabaseSettings Load()
    {
        var json = ProtectedFileStore.ReadProtected(AppPaths.DatabaseSettingsFile(module));
        if (string.IsNullOrEmpty(json))
        {
            return new DatabaseSettings { DatabaseName = module.DefaultDatabaseName() };
        }

        try
        {
            return JsonSerializer.Deserialize<DatabaseSettings>(json) ?? new DatabaseSettings { DatabaseName = module.DefaultDatabaseName() };
        }
        catch (JsonException)
        {
            // Beschädigte/nicht mehr entschlüsselbare Einstellungsdatei -> von vorne beginnen,
            // statt die App zum Absturz zu bringen.
            return new DatabaseSettings { DatabaseName = module.DefaultDatabaseName() };
        }
    }

    public void Save(DatabaseSettings settings)
    {
        settings.IsConfigured = true;
        var json = JsonSerializer.Serialize(settings);
        ProtectedFileStore.WriteProtected(AppPaths.DatabaseSettingsFile(module), json);
    }

    public void Clear()
    {
        var path = AppPaths.DatabaseSettingsFile(module);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
