using Kassajournal.Core.Models;
using Kassajournal.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Kassajournal.Data.Local;

/// <summary>Implementierung von <see cref="IKassaRepository"/> gegen die lokale, verschlüsselte SQLite-Datenbank.</summary>
public class LocalKassaRepository(LocalDbContext db) : IKassaRepository
{
    public async Task<IReadOnlyList<KassaEntry>> GetEntriesForMonthAsync(int year, int month, CancellationToken ct = default)
    {
        return await db.Entries
            .Where(e => !e.IsDeleted && e.Date.Year == year && e.Date.Month == month)
            .OrderBy(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<KassaEntry>> GetEntriesForDayAsync(DateOnly date, CancellationToken ct = default)
    {
        return await db.Entries
            .Where(e => !e.IsDeleted && e.Date == date)
            .ToListAsync(ct);
    }

    public async Task UpsertEntryAsync(KassaEntry entry, CancellationToken ct = default)
    {
        entry.UpdatedAtUtc = DateTimeOffset.UtcNow;
        entry.IsSynced = false;

        var existing = await db.Entries.FindAsync([entry.Id], ct);
        if (existing is null)
        {
            db.Entries.Add(entry);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(entry);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteEntryAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await db.Entries.FindAsync([id], ct);
        if (existing is null)
        {
            return;
        }

        existing.IsDeleted = true;
        existing.IsSynced = false;
        existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<MonthSettings> GetOrCreateMonthSettingsAsync(int year, int month, CancellationToken ct = default)
    {
        var existing = await db.MonthSettings.FindAsync([year, month], ct);
        if (existing is not null)
        {
            return existing;
        }

        var created = new MonthSettings { Year = year, Month = month };
        db.MonthSettings.Add(created);
        await db.SaveChangesAsync(ct);
        return created;
    }

    public async Task SaveMonthSettingsAsync(MonthSettings settings, CancellationToken ct = default)
    {
        var existing = await db.MonthSettings.FindAsync([settings.Year, settings.Month], ct);
        if (existing is null)
        {
            db.MonthSettings.Add(settings);
        }
        else
        {
            existing.Anfangssaldo = settings.Anfangssaldo;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<KassaEntry>> GetUnsyncedEntriesAsync(CancellationToken ct = default)
    {
        return await db.Entries.Where(e => !e.IsSynced).ToListAsync(ct);
    }

    public async Task MarkSyncedAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idSet = ids.ToHashSet();
        var toUpdate = await db.Entries.Where(e => idSet.Contains(e.Id)).ToListAsync(ct);
        foreach (var entry in toUpdate)
        {
            entry.IsSynced = true;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<DateOnly?> GetMostRecentEntryDateAsync(CancellationToken ct = default)
    {
        return await db.Entries
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.Date)
            .Select(e => (DateOnly?)e.Date)
            .FirstOrDefaultAsync(ct);
    }
}
