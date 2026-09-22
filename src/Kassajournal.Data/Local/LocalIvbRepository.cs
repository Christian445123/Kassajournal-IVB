using Kassajournal.Core.Models;
using Kassajournal.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Kassajournal.Data.Local;

public class LocalIvbRepository(LocalDbContext db) : IIvbRepository
{
    public async Task<IReadOnlyList<IvbEntry>> GetEntriesForMonthAsync(int year, int month, CancellationToken ct = default)
    {
        return await db.IvbEntries
            .Where(e => !e.IsDeleted && e.Date.Year == year && e.Date.Month == month)
            .OrderBy(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<IvbEntry>> GetEntriesForYearAsync(int year, CancellationToken ct = default)
    {
        return await db.IvbEntries
            .Where(e => !e.IsDeleted && e.Date.Year == year)
            .OrderBy(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task UpsertEntryAsync(IvbEntry entry, CancellationToken ct = default)
    {
        entry.UpdatedAtUtc = DateTimeOffset.UtcNow;
        entry.IsSynced = false;

        var existing = await db.IvbEntries.FindAsync([entry.Id], ct);
        if (existing is null)
        {
            db.IvbEntries.Add(entry);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(entry);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteEntryAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await db.IvbEntries.FindAsync([id], ct);
        if (existing is null)
        {
            return;
        }

        existing.IsDeleted = true;
        existing.IsSynced = false;
        existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IvbEntry>> GetUnsyncedEntriesAsync(CancellationToken ct = default)
    {
        return await db.IvbEntries.Where(e => !e.IsSynced).ToListAsync(ct);
    }

    public async Task MarkSyncedAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idSet = ids.ToHashSet();
        var toUpdate = await db.IvbEntries.Where(e => idSet.Contains(e.Id)).ToListAsync(ct);
        foreach (var entry in toUpdate)
        {
            entry.IsSynced = true;
        }

        await db.SaveChangesAsync(ct);
    }
}
