using Kassajournal.Core.Models;

namespace Kassajournal.Core.Services;

public interface IIvbRepository
{
    Task<IReadOnlyList<IvbEntry>> GetEntriesForMonthAsync(int year, int month, CancellationToken ct = default);

    Task<IReadOnlyList<IvbEntry>> GetEntriesForYearAsync(int year, CancellationToken ct = default);

    Task UpsertEntryAsync(IvbEntry entry, CancellationToken ct = default);

    Task DeleteEntryAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<IvbEntry>> GetUnsyncedEntriesAsync(CancellationToken ct = default);

    Task MarkSyncedAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
