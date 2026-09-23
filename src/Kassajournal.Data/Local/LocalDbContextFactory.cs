using Kassajournal.Core;
using Kassajournal.Data.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kassajournal.Data.Local;

/// <summary>
/// Baut den <see cref="LocalDbContext"/> mit einer verschlüsselten SQLite-Verbindung auf
/// (SQLCipher via SQLitePCLRaw.bundle_e_sqlcipher, Passwort kommt aus <see cref="LocalDatabaseKeyProvider"/>).
///
/// WICHTIG (siehe Vorfall v0.1.7-v0.1.10): Ein automatisches "Neuanlegen bei Fehler" ist hier
/// gefährlich, wenn es zu großzügig ausgelöst wird - eine völlig intakte Datenbank mit echten
/// Buchungen könnte dabei stillschweigend beiseitegeschoben und durch eine leere ersetzt werden,
/// OHNE dass eine Fehlermeldung erscheint (der zweite Create()-Versuch gelingt ja). Deshalb:
///
/// 1) Nur EIN einziger, eindeutiger SQLite-Fehlercode (26 = SQLITE_NOTADB - die Datei existiert,
///    lässt sich aber mit dem aktuellen Schlüssel nicht als Datenbank öffnen, z. B. ein
///    Überbleibsel einer sehr alten, unverschlüsselten Programmversion) löst ein Neuanlegen aus.
///    Alle anderen SQLite-Fehler (z. B. kurzzeitig gesperrt/busy) werden NICHT als "kaputt"
///    behandelt, sondern nach kurzer Wartezeit erneut versucht.
/// 2) Dabei wird NUR die Datenbankdatei beiseitegeschoben, NIEMALS der Schlüssel. Der Schlüssel
///    ist per Definition in Ordnung (sonst gäbe es eine CryptographicException, keine
///    SqliteException) - würden Datenbank UND Schlüssel als zwei getrennte Dateioperationen
///    verschoben und eine davon schlägt fehl (z. B. weil kurz gesperrt), entstünde ein dauerhaft
///    unpassendes Paar (neuer Schlüssel + alte Datenbank), das für immer mit genau demselben
///    Fehler fehlschlagen würde. Indem nur die Datenbank bewegt und mit dem UNVERÄNDERTEN,
///    bereits vorhandenen Schlüssel neu angelegt wird, kann dieser Fall gar nicht erst entstehen.
/// </summary>
public static class LocalDbContextFactory
{
    /// <summary>SQLite-Fehlercode "file is not a database" - siehe https://www.sqlite.org/rescode.html#notadb</summary>
    private const int SqliteNotADatabase = 26;

    public static LocalDbContext Create()
    {
        for (var versuch = 1; versuch <= 3; versuch++)
        {
            try
            {
                return CreateInternal();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteNotADatabase)
            {
                // Eindeutig: die Datenbankdatei ist mit dem aktuellen (unveränderten) Schlüssel
                // nicht lesbar - Datei beiseiteschieben (nicht löschen, für eine eventuelle
                // manuelle Datenrettung) und mit demselben Schlüssel frisch anlegen.
                QuarantineDatabaseFile();
                return CreateInternal();
            }
            catch (SqliteException) when (versuch < 3)
            {
                // Vermutlich nur kurzzeitig gesperrt (z. B. Virenscanner, noch laufender vorheriger
                // Prozess) - kurz warten und erneut versuchen, statt sofort aufzugeben.
                Thread.Sleep(300 * versuch);
            }
        }

        // Letzter Versuch ohne Fangnetz: falls es weiterhin fehlschlägt, wird der Fehler ganz normal
        // nach oben gereicht und über die globale Fehleranzeige sichtbar gemacht - Daten bleiben
        // dabei in jedem Fall unangetastet.
        return CreateInternal();
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

    private static void QuarantineDatabaseFile()
    {
        // Verbindungspool leeren, damit kein offener (gepoolter) Datei-Handle das Verschieben
        // blockiert oder anschließend auf die alte, verschobene Datei zugreift.
        SqliteConnection.ClearAllPools();

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var path = AppPaths.LocalDatabaseFile;

        // Die Haupt-Datenbankdatei UND ihre WAL/SHM-Begleitdateien gemeinsam verschieben - im
        // WAL-Modus (Standard bei SQLite) können noch nicht eingecheckte Änderungen ausschließlich
        // in "-wal" liegen; würde nur die Hauptdatei verschoben, gingen diese Daten verloren.
        // Wichtig: SQLite erwartet die Begleitdateien als "<Hauptdatei>-wal"/"-shm" - die neuen
        // Namen müssen also relativ zum NEUEN (verschobenen) Hauptdateinamen gebildet werden,
        // nicht einfach ".beschaedigt-…" an die alten "-wal"/"-shm"-Namen angehängt werden.
        var neuerHauptname = $"{path}.beschaedigt-{stamp}";
        MoveTo(path, neuerHauptname);
        MoveTo($"{path}-wal", $"{neuerHauptname}-wal");
        MoveTo($"{path}-shm", $"{neuerHauptname}-shm");
    }

    private static void MoveTo(string path, string zielPfad)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Move(path, zielPfad);
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
                // und die App zeigt (dank der globalen Fehleranzeige) eine verständliche Meldung,
                // statt irgendetwas in einen inkonsistenten Zustand zu bringen.
            }
        }
    }
}
