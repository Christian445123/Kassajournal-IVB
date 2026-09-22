using Kassajournal.Core;
using Kassajournal.Data.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kassajournal.Data.Local;

/// <summary>
/// Baut den <see cref="LocalDbContext"/> mit einer verschlüsselten SQLite-Verbindung auf
/// (SQLCipher via SQLitePCLRaw.bundle_e_sqlcipher, Passwort kommt aus <see cref="LocalDatabaseKeyProvider"/>).
/// Es wird immer sichergestellt, dass am Ende eine funktionierende lokale Datenbank existiert -
/// notfalls durch Neuanlegen, falls die vorhandene Datei beschädigt oder nicht mehr lesbar ist.
/// </summary>
public static class LocalDbContextFactory
{
    public static LocalDbContext Create()
    {
        try
        {
            return CreateInternal();
        }
        catch (SqliteException)
        {
            // Die vorhandene Datenbankdatei lässt sich nicht öffnen - z. B. ein Überbleibsel einer
            // alten Programmversion (vor der SQLCipher-Verschlüsselung) oder eine beschädigte Datei.
            // Statt die App dauerhaft abstürzen zu lassen: defekte Datei(en) beiseiteschieben
            // (nicht löschen - für eine eventuelle manuelle Datenrettung) und frisch anlegen.
            QuarantineBrokenFiles();
            return CreateInternal();
        }
    }

    private static LocalDbContext CreateInternal()
    {
        var password = LocalDatabaseKeyProvider.GetOrCreateKey();

        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = AppPaths.LocalDatabaseFile,
            Password = password,
        };

        var options = new DbContextOptionsBuilder<LocalDbContext>()
            .UseSqlite(connectionStringBuilder.ConnectionString)
            .Options;

        var context = new LocalDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void QuarantineBrokenFiles()
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        MoveAside(AppPaths.LocalDatabaseFile, stamp);
        MoveAside(AppPaths.LocalDatabaseKeyFile, stamp);
    }

    private static void MoveAside(string path, string stamp)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Move(path, $"{path}.beschaedigt-{stamp}");
        }
        catch
        {
            // Wenn selbst das Verschieben scheitert (z. B. Datei gesperrt), als letzten Ausweg
            // versuchen zu löschen, damit die App zumindest wieder starten kann.
            try
            {
                File.Delete(path);
            }
            catch
            {
                // Beide Versuche gescheitert - der erneute Create()-Aufruf schlägt dann wieder fehl
                // und die App zeigt (dank der globalen Fehleranzeige) eine verständliche Meldung.
            }
        }
    }
}
