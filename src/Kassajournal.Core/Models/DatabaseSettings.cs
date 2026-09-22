namespace Kassajournal.Core.Models;

/// <summary>
/// Verbindungsdaten zur zentralen Datenbank, wie sie 1x im Einstellungsdialog der App eingegeben werden.
/// Diese Klasse wird nie im Klartext auf die Festplatte geschrieben – siehe
/// Kassajournal.App/Settings/ProtectedSettingsStore (DPAPI-Verschlüsselung) in der App-Schicht.
/// </summary>
public class DatabaseSettings
{
    public bool IsConfigured { get; set; }

    public DatabaseEngine Engine { get; set; } = DatabaseEngine.MySql;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 3306;

    public string DatabaseName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Verschlüsselte Verbindung (TLS/SSL) erzwingen – Standard: immer an.</summary>
    public bool RequireEncryption { get; set; } = true;

    public static int DefaultPortFor(DatabaseEngine engine) => engine switch
    {
        DatabaseEngine.MySql => 3306,
        DatabaseEngine.PostgreSql => 5432,
        DatabaseEngine.SqlServer => 1433,
        _ => 0,
    };
}
