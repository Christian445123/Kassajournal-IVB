using Kassajournal.Core.Models;

namespace Kassajournal.Core;

/// <summary>Zentrale Pfade für alles, was die App lokal auf der Festplatte ablegt.</summary>
public static class AppPaths
{
    public static string AppDataFolder
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Kassajournal-IVB");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    /// <summary>Eine gemeinsame lokale, verschlüsselte SQLite-Datenbank für beide Bereiche (Kassajournal + IVB).</summary>
    public static string LocalDatabaseFile => Path.Combine(AppDataFolder, "app.local.db");

    public static string LocalDatabaseKeyFile => Path.Combine(AppDataFolder, "local.key.protected");

    /// <summary>Je Bereich eine eigene, verschlüsselte Datei mit den Zugangsdaten zur jeweiligen Zentraldatenbank.</summary>
    public static string DatabaseSettingsFile(AppModule module) =>
        Path.Combine(AppDataFolder, $"database.settings.{module.ToString().ToLowerInvariant()}.protected");
}
