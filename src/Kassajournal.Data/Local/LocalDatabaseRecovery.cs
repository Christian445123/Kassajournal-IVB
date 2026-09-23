using Kassajournal.Core;
using Kassajournal.Data.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kassajournal.Data.Local;

/// <summary>
/// Sucht nach lokalen Datenbank-Dateien, die durch die Selbstheilung in
/// <see cref="LocalDbContextFactory"/> beiseitegeschoben wurden (Dateiname endet auf
/// ".beschaedigt-JJJJMMTT-HHmmss"), und versucht, darin enthaltene Buchungen in die aktuell
/// aktive lokale Datenbank zu übernehmen. Wird beim App-Start aufgerufen, damit nach einem
/// früheren Zwischenfall keine Daten dauerhaft verloren bleiben.
/// </summary>
public static class LocalDatabaseRecovery
{
    public sealed record Fund(string DbPath, string Password, DateTime Zeitpunkt);

    /// <summary>Findet alle beiseitegeschobenen Datenbank-Dateien, neueste zuerst.</summary>
    public static IReadOnlyList<Fund> FindeSicherungen()
    {
        var ordner = AppPaths.AppDataFolder;
        if (!Directory.Exists(ordner))
        {
            return [];
        }

        var aktuellerSchluessel = LocalDatabaseKeyProvider.GetOrCreateKey();
        var ergebnisse = new List<Fund>();

        // Die Wildcard matcht auch die mitverschobenen "-wal"/"-shm"-Begleitdateien (siehe
        // LocalDbContextFactory.QuarantineDatabaseFile) - die sind keine eigenständig öffenbaren
        // Datenbanken und werden hier ausdrücklich ausgeschlossen.
        var dbDateien = Directory.GetFiles(ordner, "app.local.db.beschaedigt-*")
            .Where(p => !p.EndsWith("-wal", StringComparison.OrdinalIgnoreCase) && !p.EndsWith("-shm", StringComparison.OrdinalIgnoreCase));

        foreach (var dbPath in dbDateien)
        {
            var dateiname = Path.GetFileName(dbPath);
            var suffix = dateiname.Replace("app.local.db.beschaedigt-", string.Empty);

            var zeitpunkt = DateTime.TryParseExact(suffix, "yyyyMMdd-HHmmss", null, System.Globalization.DateTimeStyles.None, out var geparst)
                ? geparst
                : File.GetLastWriteTime(dbPath);

            // Ein zeitgleich beiseitegeschobener Schlüssel gehört zu genau dieser Datenbank -
            // falls vorhanden, muss DER (nicht der aktuelle) zum Entschlüsseln verwendet werden.
            var passenderSchluesselPfad = Path.Combine(ordner, $"local.key.protected.beschaedigt-{suffix}");
            var passwort = File.Exists(passenderSchluesselPfad)
                ? TryLesen(passenderSchluesselPfad) ?? aktuellerSchluessel
                : aktuellerSchluessel;

            ergebnisse.Add(new Fund(dbPath, passwort, zeitpunkt));
        }

        return ergebnisse.OrderByDescending(f => f.Zeitpunkt).ToList();
    }

    private static string? TryLesen(string pfad)
    {
        try
        {
            return ProtectedFileStore.ReadProtected(pfad);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Versucht, die Buchungen aus einer gefundenen Sicherung in die aktuell aktive lokale
    /// Datenbank zu übernehmen (nur Einträge, die dort noch nicht existieren - nichts wird
    /// überschrieben). Gibt zurück, wie viele Einträge je Bereich übernommen wurden.
    /// </summary>
    public static async Task<(int Kassajournal, int Ivb)> WiederherstellenAsync(Fund fund, LocalDbContext zielDb, CancellationToken ct = default)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fund.DbPath,
            Password = fund.Password,
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<LocalDbContext>().UseSqlite(connectionString).Options;

        await using var sicherungsDb = new LocalDbContext(options);

        List<Core.Models.KassaEntry> kassaEintraege;
        List<Core.Models.IvbEntry> ivbEintraege;
        try
        {
            kassaEintraege = await sicherungsDb.Entries.AsNoTracking().Where(e => !e.IsDeleted).ToListAsync(ct);
            ivbEintraege = await sicherungsDb.IvbEntries.AsNoTracking().Where(e => !e.IsDeleted).ToListAsync(ct);
        }
        catch (SqliteException)
        {
            // Passwort passt nicht zu dieser Sicherung (z. B. bei einer sehr alten,
            // unverschlüsselten Datei) - es gibt nichts, das sich daraus lesen lässt.
            return (0, 0);
        }

        var vorhandeneKassaIds = (await zielDb.Entries.Select(e => e.Id).ToListAsync(ct)).ToHashSet();
        var vorhandeneIvbIds = (await zielDb.IvbEntries.Select(e => e.Id).ToListAsync(ct)).ToHashSet();

        var neueKassa = kassaEintraege.Where(e => !vorhandeneKassaIds.Contains(e.Id)).ToList();
        var neueIvb = ivbEintraege.Where(e => !vorhandeneIvbIds.Contains(e.Id)).ToList();

        foreach (var entry in neueKassa)
        {
            entry.IsSynced = false; // erneuter Sync-Versuch zur Zentraldatenbank erzwingen
            zielDb.Entries.Add(entry);
        }

        foreach (var entry in neueIvb)
        {
            entry.IsSynced = false;
            zielDb.IvbEntries.Add(entry);
        }

        if (neueKassa.Count > 0 || neueIvb.Count > 0)
        {
            await zielDb.SaveChangesAsync(ct);
        }

        return (neueKassa.Count, neueIvb.Count);
    }
}
