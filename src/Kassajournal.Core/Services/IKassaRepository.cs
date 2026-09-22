using Kassajournal.Core.Models;

namespace Kassajournal.Core.Services;

/// <summary>
/// Abstraktion über den Datenzugriff, damit ViewModels/UI nicht direkt von EF Core oder SQLite
/// abhängen. Die App verwendet immer die lokale (verschlüsselte) Implementierung als Quelle der
/// Wahrheit; die Synchronisation mit der Zentraldatenbank läuft im Hintergrund darüber.
/// </summary>
public interface IKassaRepository
{
    Task<IReadOnlyList<KassaEntry>> GetEntriesForMonthAsync(int year, int month, CancellationToken ct = default);

    Task<IReadOnlyList<KassaEntry>> GetEntriesForDayAsync(DateOnly date, CancellationToken ct = default);

    Task UpsertEntryAsync(KassaEntry entry, CancellationToken ct = default);

    Task DeleteEntryAsync(Guid id, CancellationToken ct = default);

    Task<MonthSettings> GetOrCreateMonthSettingsAsync(int year, int month, CancellationToken ct = default);

    Task SaveMonthSettingsAsync(MonthSettings settings, CancellationToken ct = default);

    /// <summary>Alle Einträge, die noch nicht erfolgreich mit der Zentraldatenbank abgeglichen wurden.</summary>
    Task<IReadOnlyList<KassaEntry>> GetUnsyncedEntriesAsync(CancellationToken ct = default);

    Task MarkSyncedAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    /// <summary>Das jüngste Datum, zu dem bereits Einträge existieren – dient dazu, beim Start zu erkennen, ob ein neuer Tag angelegt werden muss.</summary>
    Task<DateOnly?> GetMostRecentEntryDateAsync(CancellationToken ct = default);
}
